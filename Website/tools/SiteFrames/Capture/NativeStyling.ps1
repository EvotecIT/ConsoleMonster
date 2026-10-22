param(
    [Parameter(Mandatory)][string] $ModulePath,
    [Parameter(Mandatory)][string] $EvidencePath,
    [Parameter(Mandatory)][ValidateSet('Dark', 'Light', 'HighContrast')][string] $Theme,
    [ValidateSet('syntax', 'selection', 'prediction', 'progress')][string] $Phase = 'syntax',
    [switch] $UseAll,
    [switch] $CapabilitiesOnly
)
$ErrorActionPreference = 'Stop'
$Host.UI.RawUI.BackgroundColor = if ($Theme -eq 'Light') { 'White' } else { 'Black' }
$Host.UI.RawUI.ForegroundColor = if ($Theme -eq 'Light') { 'Black' } else { 'Gray' }
Import-Module $ModulePath -ErrorAction Stop
Import-Module PSReadLine -ErrorAction Stop
Set-ConsoleTheme -Name $Theme
$resolvedTheme = [ConsoleMonster.Terminal]::Theme.Name
if ($resolvedTheme -cne $Theme) { throw "Native capture resolved $resolvedTheme instead of $Theme." }
Set-PSReadLineOption -HistorySaveStyle SaveNothing -HistorySavePath ($EvidencePath + '.history.txt') -EditMode Windows -BellStyle None
$options = Get-PSReadLineOption
$missingPrediction = @('InlinePredictionColor', 'PredictionSource', 'PredictionViewStyle' | Where-Object { -not $options.PSObject.Properties[$_] })
$predictionAvailable = $missingPrediction.Count -eq 0
if ($predictionAvailable) {
    Set-PSReadLineOption -PredictionSource History -PredictionViewStyle InlineView
}
$receipt = @{
    Runtime = $PSVersionTable.PSVersion.ToString(); ReadLine = (Get-Module PSReadLine).Version.ToString()
    ReadLinePath = Join-Path (Get-Module PSReadLine).ModuleBase 'PSReadLine.psd1'
    RequestedTheme = $Theme; ResolvedTheme = $resolvedTheme; PredictionAvailable = $predictionAvailable
    MissingPredictionProperties = $missingPrediction; Phase = $Phase; All = [bool] $UseAll
}
if ($CapabilitiesOnly) { $receipt | ConvertTo-Json | Set-Content -LiteralPath ($EvidencePath + '.json'); exit 0 }
if ($Phase -eq 'prediction' -and -not $predictionAvailable) { throw 'The selected PSReadLine does not expose prediction properties.' }
$baseline = @{}
foreach ($property in $options.PSObject.Properties) {
    if ($property.Name -match 'Color$') { $baseline[$property.Name] = $property.Value }
}
$progressBaseline = if ($PSVersionTable.PSVersion -ge [version] '7.2') {
    @{ Style = $PSStyle.Progress.Style; View = $PSStyle.Progress.View }
} else {
    @{ Foreground = $Host.PrivateData.ProgressForegroundColor; Background = $Host.PrivateData.ProgressBackgroundColor }
}
@{ ReadLine = $baseline; Progress = $progressBaseline } | Export-Clixml ($EvidencePath + '.baseline.clixml')
$null = Start-Transcript -LiteralPath ($EvidencePath + '.transcript.txt') -Force
try {
    $native = @(Get-Command -Name Write-* -CommandType Cmdlet -ListImported)
    if ($UseAll) { Enable-ConsoleFormatting -Feature All }
    else { Enable-ConsoleFormatting -Feature ReadLine, Streams }
    if ([ConsoleMonster.Terminal]::Theme.Name -cne $Theme) { throw 'Enabling formatting changed the requested theme.' }
    $applied = @{}
    foreach ($entry in @{ CommandColor = 'Series6'; SelectionColor = 'Focus'; InlinePredictionColor = 'Muted' }.GetEnumerator()) {
        if (-not $options.PSObject.Properties[$entry.Key]) { continue }
        $expected = [ConsoleMonster.Rendering.SgrEncoder]::Encode([ConsoleMonster.Terminal]::Theme.Resolve($entry.Value, 'TrueColor'), 'TrueColor')
        $applied[$entry.Key] = $options.PSObject.Properties[$entry.Key].Value
        if ($applied[$entry.Key] -cne $expected) { throw "Wrong native capture colour: $($entry.Key)" }
    }
    $receipt.AppliedReadLine = $applied
    foreach ($command in $native) {
        $after = Get-Command $command.Name
        if ($after.CommandType -ne 'Cmdlet' -or $after.ImplementingType -ne $command.ImplementingType) { throw "Styling replaced $($command.Name)." }
    }
    if (-not $UseAll -and (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)) { throw 'Cosmetic styling installed Out-Default.' }
    if ($PSVersionTable.PSVersion -ge [version] '7.2' -and $PSStyle.Progress.View -ne $progressBaseline.View) { throw 'Enabling changed progress View.' }
    Clear-Host
    if ($Phase -eq 'progress') {
        $ProgressPreference = 'Continue'
        Write-Host 'Native backup progress'
        Write-Progress -Id 7 -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42 -CurrentOperation 'archive.zip' -SecondsRemaining 9
        # Native hosts can throttle their progress display. Keep the real record alive for one paint interval.
        Start-Sleep -Milliseconds 500
        [Console]::Write("$([char]27)]0;CM_FRAME$([char]7)")
        Write-Progress -Id 7 -Activity 'Backup' -Completed
    } else {
        $global:NativeStylingSelection = 0
        if ($Phase -eq 'prediction') {
            Set-PSReadLineKeyHandler -Chord F2 -ScriptBlock {
                [Microsoft.PowerShell.PSConsoleReadLine]::AddToHistory('Get-Process -Name pwsh | Select-Object -First 3')
            }
        }
        Set-PSReadLineKeyHandler -Chord F12 -ScriptBlock {
            $start = 0
            $length = 0
            [Microsoft.PowerShell.PSConsoleReadLine]::GetSelectionState([ref] $start, [ref] $length)
            $global:NativeStylingSelection = $length
            [Console]::Write("$([char]27)]0;CM_FRAME$([char]7)")
            [Microsoft.PowerShell.PSConsoleReadLine]::AcceptLine()
        }
        function global:prompt { 'PS C:\Admin> ' }
        [Console]::Write('PS C:\Admin> ')
        [Console]::Write("$([char]27)]0;CM_READY$([char]7)")
        $legacyReader = [Microsoft.PowerShell.PSConsoleReadLine].GetMethods() | Where-Object { $_.Name -eq 'ReadLine' -and $_.GetParameters().Count -eq 2 }
        $line = if ($legacyReader) {
            [Microsoft.PowerShell.PSConsoleReadLine]::ReadLine([runspace]::DefaultRunspace, $ExecutionContext)
        } else {
            [Microsoft.PowerShell.PSConsoleReadLine]::ReadLine([runspace]::DefaultRunspace, $ExecutionContext, $null)
        }
        if ($Phase -eq 'selection' -and $global:NativeStylingSelection -le 0) { throw 'The native selection was not exercised.' }
        if (-not $line.StartsWith('Get-')) { throw 'The native reader did not receive the scripted command.' }
    }
    Disable-ConsoleFormatting
    $restored = @{}
    foreach ($name in $baseline.Keys) {
        $restored[$name] = $options.PSObject.Properties[$name].Value
        if ($restored[$name] -cne $baseline[$name]) { throw "Native ReadLine restore failed: $name" }
    }
    $restored | Export-Clixml ($EvidencePath + '.restored.clixml')
    if ($PSVersionTable.PSVersion -ge [version] '7.2') {
        if ($PSStyle.Progress.Style -cne $progressBaseline.Style -or $PSStyle.Progress.View -ne $progressBaseline.View) { throw 'Native progress did not restore Style and preserve View.' }
    } elseif ($Host.PrivateData.ProgressForegroundColor -ne $progressBaseline.Foreground -or $Host.PrivateData.ProgressBackgroundColor -ne $progressBaseline.Background) {
        throw 'Native host progress colours were not restored.'
    }
    $receipt.Restored = $true
    $receipt.NativeCommands = $true
    $receipt.WriteCmdlets = $native.Count
    $receipt | ConvertTo-Json | Set-Content -LiteralPath ($EvidencePath + '.json')
} finally {
    Disable-ConsoleFormatting
    $null = Stop-Transcript
}
