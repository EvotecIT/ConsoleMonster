Describe 'Native colour lease disposal contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:Runtime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:Module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
    }

    It 'checks actual native editor bytes on both PowerShell runtimes' -Skip:($env:OS -ne 'Windows_NT') {
        # Reuse the site's existing ConPTY owner instead of duplicating Windows input/capture interop in Pester.
        $project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../Website/tools/SiteFrames.Tests/SiteFrames.Tests.csproj'))
        $buildSettings = @{}
        foreach ($name in 'MSBUILDDISABLENODEREUSE', 'DOTNET_CLI_USE_MSBUILD_SERVER') { $buildSettings[$name] = [Environment]::GetEnvironmentVariable($name) }
        try {
            $env:MSBUILDDISABLENODEREUSE = '1'
            $env:DOTNET_CLI_USE_MSBUILD_SERVER = '0'
            $output = & dotnet test $project -c Debug -warnaserror --filter 'FullyQualifiedName~NativeEditorSuppressionContracts' --logger 'trx;LogFileName=native-editor.trx' --results-directory $TestDrive 2>&1
            if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
            [xml] $report = Get-Content -LiteralPath (Join-Path $TestDrive 'native-editor.trx') -Raw
            $counts = $report.SelectSingleNode('//*[local-name()="Counters"]')
            if ($counts.total -ne '2' -or $counts.passed -ne '2' -or $counts.notExecuted -ne '0') { throw 'Native-editor integration did not execute both enabled and baseline cases.' }
        } finally {
            foreach ($name in $buildSettings.Keys) { [Environment]::SetEnvironmentVariable($name, $buildSettings[$name]) }
        }
    }

    It 'honours <Suppression> without changing editor colours' -ForEach @(
        @{ Suppression = 'NO_COLOR'; Setup = "`$env:NO_COLOR = '1'; `$env:CONSOLEMONSTER_COLOR = `$null" }
        @{ Suppression = 'colour none'; Setup = "`$env:CONSOLEMONSTER_COLOR = 'none'" }
        @{ Suppression = 'plain mode'; Setup = "`$env:CONSOLEMONSTER_COLOR = 'truecolor'; `$env:CONSOLEMONSTER_MODE = 'plain'" }
    ) {
        $body = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__'
Import-Module PSReadLine
$options = Get-PSReadLineOption
$options.CommandColor = "$([char]27)[0m"
$before = $options.CommandColor
__SETUP__
Enable-ConsoleFormatting -Feature ReadLine
if ($options.CommandColor -cne $before) { throw 'No-colour setting introduced editor colour sequences.' }
Disable-ConsoleFormatting
'NO_COLOUR_OK'
'@
        $body = $body.Replace('__MODULE__', $script:Module.Replace("'", "''")).Replace('__SETUP__', $Setup)
        $body = "try {`n$body`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
        $result = Invoke-TestChild -Runtime $script:Runtime -Script $body
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'NO_COLOUR_OK'
    }

    It 'uses the supported <Depth> editor colour depth' -ForEach @(
        @{ Depth = 'Ansi16'; Override = '16' }
        @{ Depth = 'Ansi256'; Override = '256' }
    ) {
        $body = @'
$ErrorActionPreference = 'Stop'
$env:CONSOLEMONSTER_COLOR = '__OVERRIDE__'
$env:CONSOLEMONSTER_MODE = 'stream'
if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = 'Ansi' }
Import-Module '__MODULE__'
Import-Module PSReadLine
Enable-ConsoleFormatting -Feature ReadLine
$expected = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Rendering.SgrEncoder'))::Encode([ConsoleMonster.Terminal]::Theme.Resolve('Series6', '__DEPTH__'), '__DEPTH__')
if ((Get-PSReadLineOption).CommandColor -cne $expected) { throw 'Editor colours exceeded or ignored the profile depth.' }
Disable-ConsoleFormatting
'PROFILE_DEPTH_OK'
'@
        $body = $body.Replace('__MODULE__', $script:Module.Replace("'", "''")).Replace('__OVERRIDE__', $Override).Replace('__DEPTH__', $Depth)
        $body = "try {`n$body`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }"
        $result = Invoke-TestChild -Runtime $script:Runtime -Script $body
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'PROFILE_DEPTH_OK'
    }

    It 'restores shared editor and progress colours after <Order> owner disposal' -ForEach @(
        @{ Order = 'single'; TwoOwners = '$false'; LatestFirst = '$false' }
        @{ Order = 'latest first'; TwoOwners = '$true'; LatestFirst = '$true' }
        @{ Order = 'earliest first'; TwoOwners = '$true'; LatestFirst = '$false' }
    ) {
        $code = @'
$ErrorActionPreference = 'Stop'
$env:CONSOLEMONSTER_COLOR = 'truecolor'
$env:CONSOLEMONSTER_MODE = 'stream'
if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = 'Ansi' }
Import-Module PSReadLine
$baseline = (Get-PSReadLineOption).CommandColor
$progress = if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.Progress.Style } else { $null }
$first = [powershell]::Create()
$second = $null
function Invoke-Owner($shell, $code) {
    $shell.Commands.Clear()
    $result = @($shell.AddScript($code).Invoke())
    if ($shell.HadErrors) { throw ($shell.Streams.Error -join '; ') }
    $result
}
try {
    $setup = "Import-Module '__MODULE__'; Import-Module PSReadLine; Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature ReadLine, Streams"
    $null = Invoke-Owner $first $setup
    $firstColour = (Get-PSReadLineOption).CommandColor
    $firstProgress = if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.Progress.Style } else { $null }
    if ($firstColour -ceq $baseline) { throw 'Fixture did not acquire an editor colour lease.' }
    if (__TWO__) {
        $second = [powershell]::Create()
        $null = Invoke-Owner $second $setup.Replace('Theme Light', 'Theme Dark')
        if (__LATEST__) {
            $second.Dispose(); $second = $null
            if ((Get-PSReadLineOption).CommandColor -cne $firstColour) { throw 'Latest disposal did not restore the surviving editor owner.' }
            if ($PSVersionTable.PSVersion.Major -ge 7 -and $PSStyle.Progress.Style -cne $firstProgress) { throw 'Latest disposal did not restore surviving progress style.' }
            $null = Invoke-Owner $first 'Disable-ConsoleFormatting'
        } else {
            $latest = (Get-PSReadLineOption).CommandColor
            $first.Dispose(); $first = $null
            if ((Get-PSReadLineOption).CommandColor -cne $latest) { throw 'Earlier disposal replaced the latest owner.' }
            $null = Invoke-Owner $second 'Disable-ConsoleFormatting'
        }
    } else { $first.Dispose(); $first = $null }
    if ((Get-PSReadLineOption).CommandColor -cne $baseline) { throw 'Editor baseline not restored after owner disposal.' }
    if ($PSVersionTable.PSVersion.Major -ge 7 -and $PSStyle.Progress.Style -cne $progress) { throw 'Progress baseline not restored after owner disposal.' }
    'LEASE_DISPOSAL_OK'
} catch { [Console]::Out.WriteLine(($_ | Out-String)); exit 1 }
finally { if ($first) { $first.Dispose() }; if ($second) { $second.Dispose() } }
'@
        $code = $code.Replace('__MODULE__', $script:Module.Replace("'", "''")).Replace('__TWO__', $TwoOwners).Replace('__LATEST__', $LatestFirst)
        $result = Invoke-TestChild -Runtime $script:Runtime -Script $code
        if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
        $result -join "`n" | Should -Match 'LEASE_DISPOSAL_OK'
    }
}
