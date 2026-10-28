Describe 'ReadLine and native progress styling contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:NativeRuntime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:NativeModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:NativeSupport = Join-Path $PSScriptRoot 'Support/ReadLineHarness.ps1'
        $script:StreamSupport = Join-Path $PSScriptRoot 'Support/StreamsHarness.ps1'
        function Invoke-NativeStylingChild {
            param([string] $Body, [switch] $WithoutReadLine)
            $setup = @(
                '$ErrorActionPreference = ''Stop'''
                '$env:CONSOLEMONSTER_COLOR = ''truecolor'''
                '$env:CONSOLEMONSTER_MODE = ''stream'''
                'if ($PSVersionTable.PSVersion.Major -ge 7) { $PSStyle.OutputRendering = ''Ansi'' }'
                "Import-Module '$($script:NativeModule.Replace("'", "''"))' -ErrorAction Stop"
                ". '$($script:NativeSupport.Replace("'", "''"))'"
                ". '$($script:StreamSupport.Replace("'", "''"))'"
                if (-not $WithoutReadLine) { 'Import-Module PSReadLine -ErrorAction Stop' }
                "try {`n$Body`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }; exit 0"
            ) -join "`n"
            $lines = Invoke-TestChild -Runtime $script:NativeRuntime -Script $setup
            if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
            $lines -join "`n"
        }
    }

    It 'applies syntax selection prediction and native progress for <ThemeName>' -ForEach @(
        @{ ThemeName = 'Dark'; ProgressColor = 'Blue' }
        @{ ThemeName = 'Light'; ProgressColor = 'DarkBlue' }
        @{ ThemeName = 'HighContrast'; ProgressColor = 'Cyan' }
    ) {
        Invoke-NativeStylingChild (@'
Set-ReadLineBaseline
Set-StreamsBaseline
$before = Get-ReadLineState
$progress = Get-StreamsState
$unowned = Get-ReadLineUnowned
$streamUnowned = Get-StreamsUnownedState
Set-ConsoleTheme __THEME__
Enable-ConsoleFormatting -Feature ReadLine, Streams
Assert-ReadLineTheme
Assert-StreamsState (Get-StreamsExpected)
if ($PSVersionTable.PSVersion -lt [version] '7.2' -and $Host.PrivateData.ProgressForegroundColor -ne [ConsoleColor]::__PROGRESS__) { throw 'Wrong native progress palette.' }
Assert-NativeCommands
Assert-NativeState $unowned (Get-ReadLineUnowned)
Assert-StreamsUnownedState $streamUnowned
Disable-ConsoleFormatting
Assert-NativeState $before (Get-ReadLineState)
Assert-StreamsState $progress
'THEME_OK'
'@.Replace('__THEME__', $ThemeName).Replace('__PROGRESS__', $ProgressColor)) | Should -Match 'THEME_OK'
    }

    It 'does not load missing PSReadLine or install commands for <Features>' -ForEach @(
        @{ Features = 'ReadLine' }
        @{ Features = 'ReadLine, Streams' }
    ) {
        Invoke-NativeStylingChild -WithoutReadLine (@'
if (Get-Module PSReadLine) { throw 'The missing-module oracle already loaded PSReadLine.' }
Enable-ConsoleFormatting -Feature __FEATURES__
if (Get-Module PSReadLine) { throw 'Cosmetic styling imported PSReadLine.' }
Assert-NativeCommands
Disable-ConsoleFormatting
Assert-NativeCommands
'MISSING_OK'
'@.Replace('__FEATURES__', $Features)) | Should -Match 'MISSING_OK'
    }

    It 'restores the first exact baseline after theme reapply user edits and <Lifecycle>' -ForEach @(
        @{ Lifecycle = 'partial disable'; End = 'Disable-ConsoleFormatting -Feature ReadLine, Streams' }
        @{ Lifecycle = 'module removal'; End = 'Remove-Module ConsoleMonster -ErrorAction Stop' }
    ) {
        Invoke-NativeStylingChild (@'
Set-ReadLineBaseline
Set-StreamsBaseline
$first = Get-ReadLineState
$streams = Get-StreamsState
Enable-ConsoleFormatting -Feature ReadLine, Streams
Set-ReadLineBaseline -Variant 1
Set-StreamsBaseline -Variant 1
Set-ConsoleTheme HighContrast
Enable-ConsoleFormatting -Feature ReadLine, Streams
Assert-ReadLineTheme
Assert-StreamsState (Get-StreamsExpected)
Set-ReadLineBaseline -Variant 2
Set-StreamsBaseline -Variant 2
$options = Get-PSReadLineOption
$options.EmphasisColor = "$([char]27)[34m"
$unowned = Get-ReadLineUnowned
__END__
Assert-NativeState $first (Get-ReadLineState)
Assert-StreamsState $streams
Assert-NativeState $unowned (Get-ReadLineUnowned)
'RESTORE_OK'
'@.Replace('__END__', $End)) | Should -Match 'RESTORE_OK'
    }

    It 'starts a fresh baseline after disable and reenable and restores on module removal' {
        Invoke-NativeStylingChild @'
Enable-ConsoleFormatting -Feature ReadLine, Streams
Disable-ConsoleFormatting
Set-ReadLineBaseline -Variant 1
Set-StreamsBaseline -Variant 1
$second = Get-ReadLineState
$streams = Get-StreamsState
Enable-ConsoleFormatting -Feature ReadLine, Streams
Remove-Module ConsoleMonster
Assert-NativeState $second (Get-ReadLineState)
Assert-StreamsState $streams
'REENABLE_OK'
'@ | Should -Match 'REENABLE_OK'
    }

    It 'disables <First> independently from output and other cosmetic preferences' -ForEach @(
        @{ First = 'ReadLine' }
        @{ First = 'Streams' }
        @{ First = 'Output' }
    ) {
        Invoke-NativeStylingChild (@'
$baseline = Get-ReadLineState
$streams = Get-StreamsState
Enable-ConsoleFormatting -Feature Output, ReadLine, Streams
Disable-ConsoleFormatting -Feature __FIRST__
if ('__FIRST__' -eq 'ReadLine') { Assert-NativeState $baseline (Get-ReadLineState) } else { Assert-ReadLineTheme }
if ('__FIRST__' -eq 'Streams') { Assert-StreamsState $streams } else { Assert-StreamsState (Get-StreamsExpected) }
$hasShim = $null -ne (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)
if ($hasShim -ne ('__FIRST__' -ne 'Output')) { throw 'Output shim lifetime does not follow output features.' }
Disable-ConsoleFormatting
Assert-NativeState $baseline (Get-ReadLineState)
Assert-StreamsState $streams
'INDEPENDENT_OK'
'@.Replace('__FIRST__', $First)) | Should -Match 'INDEPENDENT_OK'
    }

    It 'includes ReadLine in the defaults and preserves snippet-only state for <FeatureName>' -ForEach @(
        @{ FeatureName = 'ReadLine' }
        @{ FeatureName = 'Output, FormatCommands, Errors, Streams, ReadLine' }
        @{ FeatureName = 'Streams, ReadLine' }
    ) {
        Invoke-NativeStylingChild (@'
$baseline = Get-ReadLineState
$streamBaseline = Get-StreamsState
$snippet = @(Enable-ConsoleFormatting -Feature __FEATURE__ -ProfileSnippet)
if ($snippet -cnotcontains 'Enable-ConsoleFormatting -Feature __FEATURE__') { throw 'Profile snippet lost the requested feature.' }
Assert-NativeState $baseline (Get-ReadLineState)
Assert-StreamsState $streamBaseline
Assert-NativeCommands
Enable-ConsoleFormatting
Assert-ReadLineTheme
Assert-NativeCommands -AllowOutputShim
Disable-ConsoleFormatting -Feature Output, FormatCommands, Errors, Streams, ReadLine
Assert-NativeState $baseline (Get-ReadLineState)
Assert-StreamsState $streamBaseline
'ALL_AND_SNIPPET_OK'
'@.Replace('__FEATURE__', $FeatureName)) | Should -Match 'ALL_AND_SNIPPET_OK'
    }

    It 'keeps every loaded Write cmdlet native with the defaults and restores exact progress colors without changing View' {
        Invoke-NativeStylingChild @'
$native = @(Get-Command -Name Write-* -CommandType Cmdlet -ListImported)
if ($native.Count -lt 8) { throw 'The native command oracle is incomplete.' }
$baseline = Get-ReadLineState
Set-StreamsBaseline
$streams = Get-StreamsState
if ($PSVersionTable.PSVersion -ge [version] '7.2') { $PSStyle.Progress.View = 'Classic' }
Enable-ConsoleFormatting
Assert-NativeCommands -AllowOutputShim
foreach ($command in $native) {
    $after = Get-Command $command.Name
    if ($after.CommandType -ne 'Cmdlet' -or $after.ImplementingType -ne $command.ImplementingType) { throw "All replaced $($command.Name)." }
}
Assert-StreamsState (Get-StreamsExpected)
if ($PSVersionTable.PSVersion -ge [version] '7.2') {
    if ($PSStyle.Progress.View -ne 'Classic') { throw 'All changed native progress View.' }
    $PSStyle.Progress.View = 'Minimal'
}
Set-StreamsBaseline -Variant 2
Set-ReadLineBaseline -Variant 2
Disable-ConsoleFormatting -Feature Output, FormatCommands, Errors, Streams, ReadLine
Assert-NativeState $baseline (Get-ReadLineState)
Assert-StreamsState $streams
if ($PSVersionTable.PSVersion -ge [version] '7.2' -and $PSStyle.Progress.View -ne 'Minimal') { throw 'Disable the default set restored an unowned View.' }
Assert-NativeCommands
'ALL_NATIVE_COMMANDS_AND_PROGRESS_OK'
'@ | Should -Match 'ALL_NATIVE_COMMANDS_AND_PROGRESS_OK'
    }

    It 'keeps explicit native progress rendering outside the defaults and unqualified Disable still removes it' {
        Invoke-NativeStylingChild @'
Enable-ConsoleFormatting -Feature Progress
if ((Get-Command Write-Progress).CommandType -ne 'Function') { throw 'Explicit Progress did not install its native-parameter proxy.' }
$progressProxy = (Get-Command Write-Progress).Definition
Disable-ConsoleFormatting -Feature Output, FormatCommands, Errors, Streams, ReadLine
if ((Get-Command Write-Progress).Definition -cne $progressProxy) { throw 'Disable the default set removed the separate explicit Progress feature.' }
Disable-ConsoleFormatting
Assert-NativeCommands
'EXPLICIT_PROGRESS_OUTSIDE_ALL_OK'
'@ | Should -Match 'EXPLICIT_PROGRESS_OUTSIDE_ALL_OK'
    }

    It 'keeps a custom Out-Default while both cosmetic features are enabled' {
        Invoke-NativeStylingChild @'
function global:Out-Default { 'USER_OUTPUT' }
$definition = (Get-Command Out-Default).Definition
Enable-ConsoleFormatting -Feature ReadLine, Streams -WarningVariable warnings
if (@($warnings).Count) { throw 'A custom output command blocked cosmetic styling.' }
Assert-ReadLineTheme
Disable-ConsoleFormatting
if ((Get-Command Out-Default).Definition -cne $definition) { throw 'Cosmetic styling replaced custom output.' }
'CUSTOM_OK'
'@ | Should -Match 'CUSTOM_OK'
    }

    It 'keeps native progress records preferences and success output with cosmetic features enabled' {
        Invoke-NativeStylingChild (@'
$shell = [powershell]::Create()
try {
    $setup = "Import-Module '__MODULE__' -ErrorAction Stop; "
    $body = @'
$ProgressPreference = 'Continue'
Write-Progress -Id 7 -ParentId 2 -Activity 'Native backup' -Status 'Copying' -PercentComplete 42 -CurrentOperation 'archive.zip' -SecondsRemaining 9
Write-Progress -Id 7 -ParentId 2 -Activity 'Native backup' -Completed
'native success'
__END_BODY__
    function Invoke-ProgressOracle([string] $prefix) {
        $shell.Commands.Clear()
        $shell.Streams.Progress.Clear()
        $shell.Streams.Error.Clear()
        $output = @($shell.AddScript($prefix + $body).Invoke())
        if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
        if ($output.Count -ne 1 -or $output[0] -cne 'native success') { throw 'Progress changed native success output.' }
        @($shell.Streams.Progress | Select-Object ActivityId, ParentActivityId, Activity, StatusDescription, CurrentOperation, PercentComplete, SecondsRemaining, RecordType) | ConvertTo-Json -Compress
    }
    $null = $shell.AddScript($setup).Invoke()
    if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
    $before = Invoke-ProgressOracle ''
    $records = $before | ConvertFrom-Json
    if ($records.Count -ne 2 -or $records[0].ActivityId -ne 7 -or $records[0].ParentActivityId -ne 2 -or $records[0].PercentComplete -ne 42 -or $records[1].RecordType -ne 1) { throw "The native progress oracle is incomplete: $before" }
    $after = Invoke-ProgressOracle 'Enable-ConsoleFormatting -Feature ReadLine, Streams; '
    if ($after -cne $before) { throw 'Styling changed native progress records.' }
    $shell.Commands.Clear()
    $shell.Streams.Progress.Clear()
    $null = $shell.AddScript("`$ProgressPreference = 'SilentlyContinue'; Write-Progress -Activity 'Hidden'; Disable-ConsoleFormatting").Invoke()
    if ($shell.Streams.Progress.Count -ne 0 -or $shell.HadErrors) { throw 'Styling changed ProgressPreference.' }
    'NATIVE_PROGRESS_RECORDS_OK'
} finally { $shell.Dispose() }
'@.Replace('__END_BODY__', "'@").Replace('__MODULE__', $script:NativeModule.Replace("'", "''"))) | Should -Match 'NATIVE_PROGRESS_RECORDS_OK'
    }

    It 'preserves shared native ownership when <FirstRelease> releases first via <Lifecycle> with reapply <Reapply>' -ForEach @(
        @{ FirstRelease = 'A'; Lifecycle = 'disable'; Reapply = $false }
        @{ FirstRelease = 'B'; Lifecycle = 'disable'; Reapply = $false }
        @{ FirstRelease = 'A'; Lifecycle = 'module removal'; Reapply = $false }
        @{ FirstRelease = 'B'; Lifecycle = 'module removal'; Reapply = $false }
        @{ FirstRelease = 'A'; Lifecycle = 'disable'; Reapply = $true }
        @{ FirstRelease = 'B'; Lifecycle = 'disable'; Reapply = $true }
        @{ FirstRelease = 'A'; Lifecycle = 'module removal'; Reapply = $true }
        @{ FirstRelease = 'B'; Lifecycle = 'module removal'; Reapply = $true }
    ) {
        $support = Join-Path $PSScriptRoot 'Support'
        Invoke-NativeStylingChild (@'
. '__SUPPORT__/SharedNativeOwnership.ps1'
Test-SharedNativeOwnership -ModulePath '__MODULE__' -SupportPath '__SUPPORT__' -FirstRelease '__FIRST__' -RemoveModules:$__REMOVE__ -Reapply:$__REAPPLY__
'@.Replace('__MODULE__', $script:NativeModule.Replace("'", "''")).Replace('__SUPPORT__', $support.Replace("'", "''")).Replace('__FIRST__', $FirstRelease).Replace('__REMOVE__', ($Lifecycle -eq 'module removal').ToString().ToLowerInvariant()).Replace('__REAPPLY__', $Reapply.ToString().ToLowerInvariant())) | Should -Match 'SHARED_NATIVE_OWNERSHIP_OK'
    }

    It 'preserves caller edits to the native progress View while restoring only Style' {
        Invoke-NativeStylingChild @'
if ($PSVersionTable.PSVersion -ge [version] '7.2') {
    $original = $PSStyle.Progress.Style
    $PSStyle.Progress.View = 'Classic'
    $width = $PSStyle.Progress.MaxWidth
    Enable-ConsoleFormatting -Feature ReadLine, Streams
    if ($PSStyle.Progress.View -ne 'Classic' -or $PSStyle.Progress.MaxWidth -ne $width) { throw 'Styling changed progress layout.' }
    $PSStyle.Progress.View = 'Minimal'
    $PSStyle.Progress.Style = "$([char]27)[31m"
    Disable-ConsoleFormatting
    if ($PSStyle.Progress.View -ne 'Minimal' -or $PSStyle.Progress.Style -cne $original) { throw 'Disable restored the wrong progress properties.' }
} else {
    $baseline = Get-StreamsState
    Enable-ConsoleFormatting -Feature Streams
    $Host.PrivateData.ProgressForegroundColor = 'Magenta'
    $Host.PrivateData.ProgressBackgroundColor = 'White'
    Disable-ConsoleFormatting
    Assert-StreamsState $baseline
}
'PROGRESS_OWNERSHIP_OK'
'@ | Should -Match 'PROGRESS_OWNERSHIP_OK'
    }
}
