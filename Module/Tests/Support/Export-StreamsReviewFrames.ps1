<#
.SYNOPSIS
Exports native stream text and captured stream colour preferences as review frames.
.DESCRIPTION
Run separately in Windows PowerShell 5.1 and PowerShell 7.2 or newer after building
the module. Each state uses a fresh ConsoleHost child of the current runtime.
Native stdout/stderr and JSON configuration receipts are retained beside the
Text/HTML frames. Frames reconstruct native text with captured PSStyle (7.2+)
or Host.PrivateData (5.1) colours through ConsoleMonster's parser/cell exporter.
They are not physical terminal screenshots or proof that an attached host uses
those preferences. No profile, production theme defaults or README is replaced.
.PARAMETER OutputDirectory
Existing review destination, or a directory to create for these local artifacts.
.PARAMETER Runtime
Current PowerShell executable. External orchestration must launch this script
once under each runtime; the exporting parent and capturing children must match.
.EXAMPLE
& powershell.exe -NoProfile -File ./Module/Tests/Support/Export-StreamsReviewFrames.ps1 -OutputDirectory D:/Dev/Scratch/consolemonster-everyday/frames
.EXAMPLE
& pwsh.exe -NoProfile -File ./Module/Tests/Support/Export-StreamsReviewFrames.ps1 -OutputDirectory D:/Dev/Scratch/consolemonster-everyday/frames
.OUTPUTS
One receipt object per frame. Writes local review artifacts and appends a README section.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OutputDirectory,

    [string] $Runtime = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
)

$ErrorActionPreference = 'Stop'
$currentRuntime = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
$Runtime = (Resolve-Path -LiteralPath $Runtime).ProviderPath
if (-not [string]::Equals($Runtime, $currentRuntime, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Launch this script in the requested Runtime; parent and child must use the same executable.'
}
if ($PSVersionTable.PSVersion.Major -ne 5 -and $PSVersionTable.PSVersion -lt [version]'7.2') {
    throw 'The review lanes are Windows PowerShell 5.1 and PowerShell 7.2 or newer.'
}
$modulePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../ConsoleMonster.psd1'))
Import-Module $modulePath
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$runtimeName = if ($PSVersionTable.PSVersion.Major -eq 5) { 'ps51' } else { 'ps7' }
$utf8 = [Text.UTF8Encoding]::new($false)
$escape = [string][char]27
$sgrPattern = [regex]::Escape($escape) + '\[[0-9;]*m'
$provenance = 'Native ConsoleHost text and captured native colour configuration reconstructed through Export-ConsoleFrame; not a physical terminal screenshot. Configured preferences do not establish attached-host rendering.'
$exporterAssemblies = @(
    foreach ($assembly in @((Get-Command Enable-ConsoleFormatting).ImplementingType.Assembly, [ConsoleMonster.Terminal].Assembly)) {
        [pscustomobject]@{
            Name = $assembly.FullName
            Path = $assembly.Location
            Sha256 = (Get-FileHash -LiteralPath $assembly.Location -Algorithm SHA256).Hash
        }
    }
)

function Invoke-NativeStreamCapture {
    <# Runs a bounded ConsoleHost child, preserving both native output channels without CLIXML filtering. #>
    param(
        [string] $Script,
        [string] $BasePath
    )
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Script))
    # -EncodedCommand serializes PS5.1 streams as CLIXML even with -OutputFormat Text.
    # Decode inside ordinary -Command to preserve genuine ConsoleHost prefix/text formatting.
    $command = "& ([scriptblock]::Create([Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('$encoded'))))"
    $arguments = '-NoLogo -NoProfile -NonInteractive -OutputFormat Text -Command "' + $command + '"'
    $info = [Diagnostics.ProcessStartInfo]::new($Runtime, $arguments)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $info.StandardOutputEncoding = $utf8
    $info.StandardErrorEncoding = $utf8
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) {
            throw 'Native stream capture exceeded 60 seconds.'
        }
        $process.WaitForExit()
        [IO.File]::WriteAllText($BasePath + '.native.stdout.txt', $stdout.Result, $utf8)
        [IO.File]::WriteAllText($BasePath + '.native.stderr.txt', $stderr.Result, $utf8)
        if ($process.ExitCode -ne 0) {
            throw "Native capture failed ($($process.ExitCode)); inspect $BasePath.native.stderr.txt."
        }
        if ($stderr.Result.Length -ne 0) {
            throw "Unexpected stderr; preserved separately at $BasePath.native.stderr.txt rather than guessing channel order."
        }
        if ($stdout.Result.StartsWith('#< CLIXML')) {
            throw 'ConsoleHost returned CLIXML instead of native text.'
        }
        $stdout.Result
    } finally {
        if (-not $process.HasExited) {
            $process.Kill()
            $process.WaitForExit()
        }
        $process.Dispose()
    }
}

function Convert-CapturedSgrStyle {
    <# Adapts captured ANSI foreground/bold or RGB foreground preferences to the core style parser. #>
    param([string] $Sgr)
    $match = [regex]::Match($Sgr, '\A' + [regex]::Escape($escape) + '\[(?<codes>[0-9;]+)m\z')
    if (-not $match.Success) {
        throw 'Expected one captured foreground SGR preference; no terminal-output parser is implemented here.'
    }
    $codes = @($match.Groups['codes'].Value.Split(';') | ForEach-Object { [int]$_ })
    if ($codes[0] -eq 0) {
        $codes = @($codes | Select-Object -Skip 1)
    }
    if ($codes.Count -eq 5 -and $codes[0] -eq 38 -and $codes[1] -eq 2) {
        $specification = 'rgb(' + ($codes[2..4] -join ',') + ')'
    } elseif (($codes.Count -eq 1 -or ($codes.Count -eq 2 -and $codes[1] -eq 1)) -and
        (($codes[0] -ge 30 -and $codes[0] -le 37) -or ($codes[0] -ge 90 -and $codes[0] -le 97))) {
        $index = if ($codes[0] -lt 90) { $codes[0] - 30 } else { $codes[0] - 90 + 8 }
        $specification = "color($index)"
        if ($codes.Count -eq 2) {
            $specification = 'bold ' + $specification
        }
    } else {
        throw 'Unsupported captured foreground preference; no approximate style substituted.'
    }
    $markup = '[' + $specification + ']x[/]'
    $parsed = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.Markup'))::Parse($markup, $null, [ConsoleMonster.Capabilities.ColorDepth]::TrueColor, $true)
    $parsed.Lines[0][0].Style
}
$childTemplate = @'
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
try {
    Import-Module '__MODULE__'
    if ($Host.Name -ne 'ConsoleHost') { throw 'A genuine ConsoleHost is required.' }
    Set-ConsoleTheme -Theme '__THEME__'
    # Synthetic host canvas only; never modify the parent's console or production defaults.
    $Host.UI.RawUI.BackgroundColor = '__BACKGROUND__'
    $Host.UI.RawUI.ForegroundColor = '__FOREGROUND__'
    $WarningPreference = 'Continue'
    $VerbosePreference = 'Continue'
    $DebugPreference = 'Continue'
    $ProgressPreference = 'SilentlyContinue'
    $styleVariable = Get-Variable PSStyle -ErrorAction SilentlyContinue
    $style = if ($styleVariable) { $styleVariable.Value } else { $null }
    function Read-StreamConfiguration {
        $streams = foreach ($name in 'Warning', 'Verbose', 'Debug') {
            [pscustomobject]@{
                Name = $name
                Sgr = if ($style) { [string]$style.Formatting.$name } else { $null }
                HostForeground = [string]$Host.PrivateData.($name + 'ForegroundColor')
                HostBackground = [string]$Host.PrivateData.($name + 'BackgroundColor')
            }
        }
        @($streams)
    }
    $baseline = Read-StreamConfiguration
    if ('__STATE__' -eq 'after') { Enable-ConsoleFormatting -Include Streams -Exclude Output, FormatCommands, Errors, ReadLine }
    $configuration = Read-StreamConfiguration
    $commands = foreach ($name in 'Write-Warning', 'Write-Verbose', 'Write-Debug') {
        $command = Get-Command $name
        if ($command.CommandType -ne 'Cmdlet' -or $command.ModuleName -ne 'Microsoft.PowerShell.Utility') {
            throw "Native stream command was replaced: $name."
        }
        [pscustomobject]@{ Name = $name; Type = [string]$command.CommandType; Module = $command.ModuleName }
    }
    $assemblies = foreach ($assembly in @((Get-Command Enable-ConsoleFormatting).ImplementingType.Assembly, [ConsoleMonster.Terminal].Assembly)) {
        [pscustomobject]@{
            Name = $assembly.FullName
            Path = $assembly.Location
            Sha256 = (Get-FileHash -LiteralPath $assembly.Location -Algorithm SHA256).Hash
        }
    }
    $receipt = [pscustomobject]@{
        CapturedUtc = [datetime]::UtcNow.ToString('o')
        Runtime = [Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
        Version = [string]$PSVersionTable.PSVersion
        Edition = $PSVersionTable.PSEdition
        HostName = $Host.Name
        SupportsVirtualTerminal = $Host.UI.SupportsVirtualTerminal
        OutputRedirected = [Console]::IsOutputRedirected
        Theme = '__THEME__'
        State = '__STATE__'
        CanvasForeground = [string]$Host.UI.RawUI.ForegroundColor
        CanvasBackground = [string]$Host.UI.RawUI.BackgroundColor
        ConfigurationSource = if ($style) { 'PSStyle.Formatting' } else { 'Host.PrivateData' }
        OutputRendering = if ($style) { [string]$style.OutputRendering } else { $null }
        WarningPreference = [string]$WarningPreference
        VerbosePreference = [string]$VerbosePreference
        DebugPreference = [string]$DebugPreference
        Baseline = @($baseline)
        Configuration = @($configuration)
        Commands = @($commands)
        Assemblies = @($assemblies)
    }
    [IO.File]::WriteAllText('__RECEIPT__', ($receipt | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
    Microsoft.PowerShell.Utility\Write-Warning 'Backup space is low.'
    Microsoft.PowerShell.Utility\Write-Verbose 'Checking backup inventory.'
    # -Debug can prompt even in ConsoleHost; Continue preserves native unattended behaviour.
    Microsoft.PowerShell.Utility\Write-Debug 'Resolved backup target.'
} catch {
    [Console]::Error.WriteLine($_.ToString())
    exit 1
}
'@

$results = [Collections.Generic.List[object]]::new()
foreach ($theme in 'Light', 'Dark') {
    $beforeText = $null
    foreach ($state in 'before', 'after') {
        $name = '6-streams-' + $runtimeName + '-' + $theme.ToLowerInvariant() + '-' + $state
        $basePath = Join-Path $OutputDirectory $name
        $receiptPath = $basePath + '.json'
        $background = if ($theme -eq 'Light') { 'White' } else { 'Black' }
        $foreground = if ($theme -eq 'Light') { 'Black' } else { 'Gray' }
        $script = $childTemplate.Replace('__MODULE__', $modulePath.Replace("'", "''")).
            Replace('__THEME__', $theme).Replace('__STATE__', $state).
            Replace('__BACKGROUND__', $background).Replace('__FOREGROUND__', $foreground).
            Replace('__RECEIPT__', $receiptPath.Replace("'", "''"))
        $native = Invoke-NativeStreamCapture -Script $script -BasePath $basePath
        $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
        for ($index = 0; $index -lt $exporterAssemblies.Count; $index++) {
            if ($receipt.Assemblies[$index].Sha256 -ne $exporterAssemblies[$index].Sha256) {
                throw 'Capture/export assemblies changed during the run; regenerate against one stable build.'
            }
        }
        $plain = [regex]::Replace($native, $sgrPattern, '').Replace("`r`n", "`n").TrimEnd([char[]]"`r`n")
        if ($plain.Contains($escape)) { throw 'Unparsed terminal controls in native output.' }
        $lines = $plain -split "`n"
        # Keep all native prefixes and wording. Reject missing records or import noise.
        if ($lines.Count -ne 3 -or $lines[0] -notmatch 'Backup space is low\.$' -or
            $lines[1] -notmatch 'Checking backup inventory\.$' -or $lines[2] -notmatch 'Resolved backup target\.$') {
            throw "Unexpected native stream text; inspect $basePath.native.stdout.txt."
        }
        if ($lines[0] -eq 'Backup space is low.') { throw 'Native warning prefix was lost.' }
        if ($state -eq 'before') { $beforeText = $plain } elseif ($plain -cne $beforeText) {
            throw "Streams changed native text for $runtimeName/$theme; before/after must differ only cosmetically."
        }
        $canvas = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.Markup'))::Parse("[console.$foreground on console.$background]x[/]", $null, 'TrueColor', $true).Lines[0][0].Style
        $frame = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Canvas.CellBuffer'))::new(96, 3)
        $frame.Clear($canvas)
        $styles = [Collections.Generic.List[object]]::new()
        for ($row = 0; $row -lt 3; $row++) {
            $configuration = $receipt.Configuration[$row]
            if ($receipt.ConfigurationSource -eq 'PSStyle.Formatting') {
                $streamStyle = Convert-CapturedSgrStyle $configuration.Sgr
            } else {
                $specification = '[console.' + $configuration.HostForeground + ' on console.' + $configuration.HostBackground + ']x[/]'
                $streamStyle = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.Markup'))::Parse($specification, $null, 'TrueColor', $true).Lines[0][0].Style
            }
            $resolved = $canvas.Combine($streamStyle)
            if (([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.DisplayWidth'))::Measure($lines[$row]) -gt 96) { throw 'Native output exceeds review width.' }
            $null = $frame.Write(0, $row, $lines[$row], $resolved, 96)
            $styles.Add([pscustomobject]@{ Stream = $configuration.Name; Style = $resolved })
        }
        $frame | Export-ConsoleFrame -Format Text -Path ($basePath + '.txt') -Theme $theme
        $frame | Export-ConsoleFrame -Format Html -Path ($basePath + '.html') -Theme $theme -ChromeStyle MacOS -Title ($name + ' | reconstructed native configuration')
        $exported = [IO.File]::ReadAllText($basePath + '.txt').Replace("`r`n", "`n").TrimEnd([char[]]"`r`n")
        if ($exported -cne $plain) { throw 'Frame export changed captured native text.' }
        $receipt | Add-Member -NotePropertyName Provenance -NotePropertyValue $provenance
        $receipt | Add-Member -NotePropertyName NativeSgrObserved -NotePropertyValue ([regex]::IsMatch($native, $sgrPattern))
        $receipt | Add-Member -NotePropertyName RenderingLimit -NotePropertyValue 'Redirected ConsoleHost capture. Frame colours come from configuration, not measured attached-terminal cells; PSStyle and Host.PrivateData may differ. ANSI presence alone does not prove the host used the configured stream colours.'
        $receipt | Add-Member -NotePropertyName PaletteLimit -NotePropertyValue 'ConsoleColor values use the shared exporter ANSI palette. Custom physical terminal palettes and bold-to-bright behaviour are not measured.'
        $receipt | Add-Member -NotePropertyName ExporterAssemblies -NotePropertyValue $exporterAssemblies
        $receipt | Add-Member -NotePropertyName FrameStyles -NotePropertyValue @($styles.ToArray())
        $receipt | Add-Member -NotePropertyName TextMatchesNative -NotePropertyValue $true
        $pairMatches = if ($state -eq 'after') { $true } else { $null }
        $receipt | Add-Member -NotePropertyName BeforeAfterTextMatches -NotePropertyValue $pairMatches
        $receipt | Add-Member -NotePropertyName GeneratorSha256 -NotePropertyValue (Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
        $hashes = foreach ($suffix in '.native.stdout.txt', '.native.stderr.txt', '.txt', '.html') {
            [pscustomobject]@{
                File = $name + $suffix
                Sha256 = (Get-FileHash -LiteralPath ($basePath + $suffix) -Algorithm SHA256).Hash
            }
        }
        $receipt | Add-Member -NotePropertyName ArtifactHashes -NotePropertyValue @($hashes)
        [IO.File]::WriteAllText($receiptPath, ($receipt | ConvertTo-Json -Depth 10), $utf8)
        $results.Add([pscustomobject]@{ Name = $name; Receipt = $receiptPath; Text = $basePath + '.txt'; Html = $basePath + '.html' })
    }
}
$manifest = [pscustomobject]@{
    Provenance = $provenance
    Runtime = $Runtime
    Version = [string]$PSVersionTable.PSVersion
    BeforeAfterTextMatches = $true
    Files = @($results.ToArray())
}
[IO.File]::WriteAllText((Join-Path $OutputDirectory ("6-streams-$runtimeName-manifest.json")), ($manifest | ConvertTo-Json -Depth 6), $utf8)
$readmePath = Join-Path $OutputDirectory 'README.md'
$section = "## Native streams: $runtimeName"
$existing = if (Test-Path -LiteralPath $readmePath) { [IO.File]::ReadAllText($readmePath) } else { '' }
if (-not $existing.Contains($section)) {
    $links = foreach ($result in $results) {
        '- [' + $result.Name + '](' + [IO.Path]::GetFileName($result.Html) + ') ([text](' + [IO.Path]::GetFileName($result.Text) + '), [receipt](' + [IO.Path]::GetFileName($result.Receipt) + '))'
    }
    $sectionText = "`n`n$section`n`n$provenance`n`n" +
        "Each state retains raw native stdout/stderr and actual configured PSStyle/Host.PrivateData colours. The host canvas is set only in the test child. Before/after plain text matches in each theme. Redirected capture does not prove attached-host colour rendering; see each JSON receipt for host capability and rendering limits. Actual TTY and browser inspection are separate checks.`n`n" +
        ($links -join "`n") + "`n"
    [IO.File]::AppendAllText($readmePath, $sectionText, $utf8)
}
$results.ToArray()
