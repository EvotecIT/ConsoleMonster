Describe 'Native stream formatting ownership and parity' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:StreamsRuntime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:StreamsModule = Join-Path $PSScriptRoot '../ConsoleMonster.psd1'
        $script:StreamsSupport = Join-Path $PSScriptRoot 'Support/StreamsHarness.ps1'
        $script:StreamsFixture = Join-Path $PSScriptRoot 'Support/StreamsFixtures.cs'

        function Invoke-StreamsChild {
            param([Parameter(Mandatory)][string] $Script, [switch] $Fixture)
            $setup = @(
                '$ErrorActionPreference = ''Stop'''
                "Import-Module '$($script:StreamsModule.Replace("'", "''"))' -Force -ErrorAction Stop"
                ". '$($script:StreamsSupport.Replace("'", "''"))'"
                if ($Fixture) {
                    "Add-Type -Path '$($script:StreamsFixture.Replace("'", "''"))' -ErrorAction Stop"
                }
                "try {`n$Script`n} catch { [Console]::Out.WriteLine((`$_ | Out-String)); exit 1 }; exit 0"
            ) -join "`n"
            $lines = Invoke-TestChild -Runtime $script:StreamsRuntime -Script $setup
            if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
            $lines -join "`n"
        }
    }

    It 'applies active <ThemeName> stream roles and restores native preferences' -ForEach @(
        @{ ThemeName = 'Dark'; WarningColor = 'Yellow'; VerboseColor = 'Cyan'; DebugColor = 'DarkGray' }
        @{ ThemeName = 'Light'; WarningColor = 'DarkYellow'; VerboseColor = 'DarkCyan'; DebugColor = 'DarkGray' }
        @{ ThemeName = 'HighContrast'; WarningColor = 'Yellow'; VerboseColor = 'Cyan'; DebugColor = 'Gray' }
    ) {
        $text = Invoke-StreamsChild (@'
Set-StreamsBaseline
$baseline = Get-StreamsState
$unowned = Get-StreamsUnownedState
Set-ConsoleTheme -Theme __THEME__
$expected = Get-StreamsExpected
Enable-ConsoleFormatting -Feature Streams
Assert-StreamsState $expected
if ($PSVersionTable.PSVersion -lt [version] '7.2') {
    # Fixed owner-approved colours catch role collisions in the mapper, independently of the computed oracle.
    if ($Host.PrivateData.WarningForegroundColor -ne [ConsoleColor]::__WARNING__) { throw 'The theme warning palette colour was not applied.' }
    if ($Host.PrivateData.VerboseForegroundColor -ne [ConsoleColor]::__VERBOSE__) { throw 'The theme verbose palette colour was not applied.' }
    if ($Host.PrivateData.DebugForegroundColor -ne [ConsoleColor]::__DEBUG__) { throw 'The theme debug palette colour was not applied.' }
}
Assert-StreamsUnownedState $unowned
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'Streams created an Out-Default function.' }
Disable-ConsoleFormatting -Feature Streams
Assert-StreamsState $baseline
Assert-StreamsUnownedState $unowned
'THEME_AND_RESTORE_OK'
'@.Replace('__THEME__', $ThemeName).Replace('__WARNING__', $WarningColor).Replace('__VERBOSE__', $VerboseColor).Replace('__DEBUG__', $DebugColor))
        $text | Should -Match 'THEME_AND_RESTORE_OK'
    }

    It 'includes native stream colours in All and restores them on Disable All' {
        $text = Invoke-StreamsChild @'
Set-StreamsBaseline
$baseline = Get-StreamsState
Set-ConsoleTheme -Theme Dark
Enable-ConsoleFormatting -Feature All
Assert-StreamsState (Get-StreamsExpected)
if (-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)) { throw 'All did not install output formatting.' }
Disable-ConsoleFormatting -Feature All
Assert-StreamsState $baseline
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'All left output formatting installed.' }
'ALL_STREAMS_OK'
'@
        $text | Should -Match 'ALL_STREAMS_OK'
    }

    if ($PSVersionTable.PSVersion -ge [version] '7.2') {
        It 'uses true-colour PSStyle preferences even when the terminal profile has no colour' {
            $text = Invoke-StreamsChild @'
[ConsoleMonster.Terminal]::Profile = [ConsoleMonster.Terminal]::Profile.WithColorDepth('None')
Set-ConsoleTheme -Theme HighContrast
$baseline = Get-StreamsState
Enable-ConsoleFormatting -Feature Streams
Assert-StreamsState (Get-StreamsExpected)
if (-not $PSStyle.Formatting.Warning.Contains(';38;2;')) { throw 'The native warning preference lost true colour.' }
Disable-ConsoleFormatting
Assert-StreamsState $baseline
'NATIVE_COLOUR_DEPTH_OK'
'@
            $text | Should -Match 'NATIVE_COLOUR_DEPTH_OK'
        }
    }

    It 'enables Streams beside a custom Out-Default without replacing or invoking it' {
        $text = Invoke-StreamsChild @'
$global:streamsCustomCalls = 0
function global:Out-Default { $global:streamsCustomCalls++; 'CUSTOM_OUTPUT' }
$definition = (Get-Command Out-Default -CommandType Function).Definition
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Feature Streams -WarningVariable enableWarnings
Assert-StreamsState (Get-StreamsExpected)
if (@($enableWarnings).Count) { throw 'A custom Out-Default blocked Streams.' }
if ((Get-Command Out-Default -CommandType Function).Definition -cne $definition) { throw 'Streams replaced custom output formatting.' }
Disable-ConsoleFormatting
if ((Get-Command Out-Default -CommandType Function).Definition -cne $definition) { throw 'Disable removed custom output formatting.' }
if ($global:streamsCustomCalls -ne 0) { throw 'Streams invoked custom output formatting.' }
'CUSTOM_OUTPUT_KEPT_OK'
'@
        $text | Should -Match 'CUSTOM_OUTPUT_KEPT_OK'
    }

    It 'reapplies the current theme while preserving the first baseline and later unowned edits' {
        $text = Invoke-StreamsChild @'
Set-StreamsBaseline
$baseline = Get-StreamsState
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Feature Streams
Assert-StreamsState (Get-StreamsExpected)
# A caller changes both an owned property and a neighbouring unowned property while Streams is active.
Set-StreamsBaseline -Variant 1
if ($PSVersionTable.PSVersion -ge [version] '7.2') { $PSStyle.Formatting.TableHeader = "$([char]27)[32m" }
else { $Host.PrivateData.ErrorForegroundColor = [ConsoleColor]::Blue }
$unowned = Get-StreamsUnownedState
Set-ConsoleTheme -Theme HighContrast
Enable-ConsoleFormatting -Feature Streams
Assert-StreamsState (Get-StreamsExpected)
Set-StreamsBaseline -Variant 2
Disable-ConsoleFormatting -Feature Streams
Assert-StreamsState $baseline
Assert-StreamsUnownedState $unowned
# A later enable starts a new ownership interval with the latest caller baseline.
Set-StreamsBaseline -Variant 1
$nextBaseline = Get-StreamsState
Enable-ConsoleFormatting -Feature Streams
Disable-ConsoleFormatting
Assert-StreamsState $nextBaseline
Set-StreamsBaseline -Variant 2
$afterDisable = Get-StreamsState
Disable-ConsoleFormatting -Feature Streams
Assert-StreamsState $afterDisable
'BASELINE_AND_UNOWNED_EDITS_OK'
'@
        $text | Should -Match 'BASELINE_AND_UNOWNED_EDITS_OK'
    }

    It 'disables <FirstDisabled> independently when Output and Streams are enabled together' -ForEach @(
        @{ FirstDisabled = 'Streams' }
        @{ FirstDisabled = 'Output' }
    ) {
        $text = Invoke-StreamsChild (@'
Set-StreamsBaseline
$baseline = Get-StreamsState
Set-ConsoleTheme -Theme Dark
Enable-ConsoleFormatting -Feature Output, Streams
$expected = Get-StreamsExpected
Disable-ConsoleFormatting -Feature __FIRST__
if ('__FIRST__' -eq 'Streams') {
    Assert-StreamsState $baseline
    if (-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)) { throw 'Disabling Streams removed Output.' }
    Disable-ConsoleFormatting -Feature Output
} else {
    Assert-StreamsState $expected
    if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'Streams alone retained the output shim.' }
    Disable-ConsoleFormatting -Feature Streams
}
Assert-StreamsState $baseline
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'The last output feature left a shim.' }
'PARTIAL_DISABLE_OK'
'@.Replace('__FIRST__', $FirstDisabled))
        $text | Should -Match 'PARTIAL_DISABLE_OK'
    }

    It 'restores owned stream preferences on module removal after <Lifecycle>' -ForEach @(
        @{ Lifecycle = 'enable'; Reenable = $false }
        @{ Lifecycle = 'disable and reenable'; Reenable = $true }
    ) {
        $text = Invoke-StreamsChild (@'
Set-StreamsBaseline
$baseline = Get-StreamsState
$owner = (Get-Command Enable-ConsoleFormatting).Module
$owner.OnRemove = { 'PREVIOUS_STREAMS_REMOVE_HOOK' }
Enable-ConsoleFormatting -Feature Streams
if (__REENABLE__) {
    Disable-ConsoleFormatting
    Set-StreamsBaseline -Variant 1
    $baseline = Get-StreamsState
    Enable-ConsoleFormatting -Feature Streams
}
Set-StreamsBaseline -Variant 2
Remove-Module ConsoleMonster -ErrorAction Stop
Assert-StreamsState $baseline
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'Module removal created an output shim.' }
'MODULE_REMOVAL_OK'
'@.Replace('__REENABLE__', ('$' + $Reenable.ToString().ToLowerInvariant())))
        $text | Should -Match 'MODULE_REMOVAL_OK'
        @([regex]::Matches($text, 'PREVIOUS_STREAMS_REMOVE_HOOK')).Count | Should -Be 1
    }

    It 'emits a <FeatureName> profile snippet without changing native state or installing commands' -ForEach @(
        @{ FeatureName = 'Streams' }
        @{ FeatureName = 'All' }
    ) {
        $text = Invoke-StreamsChild (@'
$baseline = Get-StreamsState
$unowned = Get-StreamsUnownedState
$owner = (Get-Command Enable-ConsoleFormatting).Module
$hook = $owner.OnRemove
$snippet = @(Enable-ConsoleFormatting -Feature __FEATURE__ -ProfileSnippet)
if ($snippet -cnotcontains 'Import-Module ConsoleMonster') { throw 'Missing module import in the profile snippet.' }
if ($snippet -cnotcontains 'Enable-ConsoleFormatting -Feature __FEATURE__') { throw 'Wrong feature in the profile snippet.' }
Assert-StreamsState $baseline
Assert-StreamsUnownedState $unowned
if (-not [object]::ReferenceEquals($hook, $owner.OnRemove)) { throw 'The profile snippet installed a removal hook.' }
if (Get-Command Out-Default -CommandType Function -ErrorAction Ignore) { throw 'The profile snippet installed output formatting.' }
if (Get-Alias Write-Progress -ErrorAction Ignore) { throw 'The profile snippet installed progress formatting.' }
'PROFILE_SNIPPET_NO_EFFECTS_OK'
'@.Replace('__FEATURE__', $FeatureName))
        $text | Should -Match 'PROFILE_SNIPPET_NO_EFFECTS_OK'
    }

    It 'preserves warning redirection and WarningVariable with <Preference> preference' -ForEach @(
        @{ Preference = 'Continue' }
        @{ Preference = 'SilentlyContinue' }
        @{ Preference = 'Stop' }
    ) {
        $path = Join-Path $TestDrive ($Preference + '-warning.txt')
        $text = Invoke-StreamsChild (@'
$path = '__PATH__'
$before = Get-StreamsWarningOracle -Preference __PREFERENCE__ -Path $path
if ('__PREFERENCE__' -eq 'Stop') {
    if ($null -eq $before.Failure -or $before.Completed) { throw 'The native warning oracle did not stop.' }
} else {
    if ($before.WarningCount -ne 1 -or -not $before.Completed -or $before.Success -cne 'native success') { throw 'The native warning oracle did not capture one warning and continue.' }
    if ($before.WarningTypes -cne 'System.Management.Automation.WarningRecord') { throw 'The native oracle did not return a warning record.' }
}
Enable-ConsoleFormatting -Feature Streams
$after = Get-StreamsWarningOracle -Preference __PREFERENCE__ -Path $path
Assert-StreamsOracle $before $after
'WARNING_ORACLE_OK'
'@.Replace('__PATH__', $path.Replace("'", "''")).Replace('__PREFERENCE__', $Preference))
        $text | Should -Match 'WARNING_ORACLE_OK'
    }

    It 'preserves verbose debug and information records information variable tags and redirected bytes' {
        $text = Invoke-StreamsChild (@'
$before = Get-StreamsCaptureOracle -Directory '__DIRECTORY__'
if ($before.VerboseType -cne 'System.Management.Automation.VerboseRecord' -or $before.VerboseCount -ne 1) { throw 'The verbose oracle is empty or not native.' }
if ($before.DebugType -cne 'System.Management.Automation.DebugRecord' -or $before.DebugCount -ne 1) { throw 'The debug oracle is empty or not native.' }
if ($before.InformationType -cne 'System.Management.Automation.InformationRecord' -or $before.InformationCount -ne 1) { throw 'The information oracle is empty or not native.' }
if ($before.InformationTags -cne 'streams|native') { throw 'The native information tags are missing.' }
Enable-ConsoleFormatting -Feature Streams
$after = Get-StreamsCaptureOracle -Directory '__DIRECTORY__'
Assert-StreamsOracle $before $after
'CAPTURE_ORACLE_OK'
'@.Replace('__DIRECTORY__', $TestDrive.Replace("'", "''")))
        $text | Should -Match 'CAPTURE_ORACLE_OK'
    }

    It 'preserves warnings emitted directly by a compiled PSCmdlet' {
        $path = Join-Path $TestDrive 'compiled-warning.txt'
        $text = Invoke-StreamsChild -Fixture (@'
Import-Module -Assembly ([ConsoleMonsterStreamsFixtures.WriteStreamsFixtureWarningCommand].Assembly) -ErrorAction Stop
$command = 'Write-StreamsFixtureWarning'
$before = Get-StreamsWarningOracle -Preference Continue -Path '__PATH__' -Command $command
if ($before.WarningCount -ne 1 -or $before.WarningMessages -cne 'streams warning oracle') { throw 'The compiled cmdlet did not write the native warning.' }
if ($before.WarningTypes -cne 'System.Management.Automation.WarningRecord') { throw 'The compiled cmdlet warning is not a WarningRecord.' }
Enable-ConsoleFormatting -Feature Streams
$after = Get-StreamsWarningOracle -Preference Continue -Path '__PATH__' -Command $command
Assert-StreamsOracle $before $after
'COMPILED_WARNING_OK'
'@.Replace('__PATH__', $path.Replace("'", "''")))
        $text | Should -Match 'COMPILED_WARNING_OK'
    }

    It 'keeps native stream transcript text free of ANSI with Streams enabled' {
        $text = Invoke-StreamsChild (@'
function Get-TranscriptStreamLines($path) {
    $null = Start-Transcript -LiteralPath $path -ErrorAction Stop
    try {
        & {
            $VerbosePreference = 'Continue'
            $DebugPreference = 'Continue'
            $InformationPreference = 'Continue'
            Write-Warning 'STREAMS_TRANSCRIPT_WARNING'
            Write-Verbose 'STREAMS_TRANSCRIPT_VERBOSE'
            Write-Debug 'STREAMS_TRANSCRIPT_DEBUG'
            Write-Information 'STREAMS_TRANSCRIPT_INFORMATION'
        } | Microsoft.PowerShell.Core\Out-Default
    } finally { $null = Stop-Transcript -ErrorAction Stop }
    @(Get-Content -LiteralPath $path | Where-Object { $_ -match 'STREAMS_TRANSCRIPT_(WARNING|VERBOSE|DEBUG|INFORMATION)' })
}
$before = @(Get-TranscriptStreamLines '__BEFORE__')
foreach ($stream in 'WARNING', 'VERBOSE', 'DEBUG') {
    if (-not ($before -match ('STREAMS_TRANSCRIPT_' + $stream))) { throw "The native transcript oracle did not capture $stream." }
}
# Windows PowerShell 5.1 does not transcribe the native information stream in this host path.
if ($before.Count -lt 3) { throw 'The native transcript oracle is empty or incomplete.' }
Enable-ConsoleFormatting -Feature Streams
$after = @(Get-TranscriptStreamLines '__AFTER__')
if (($after -join "`n") -cne ($before -join "`n")) { throw 'Streams changed native transcript text.' }
if (($after -join "`n").Contains([string] [char]27)) { throw 'Streams leaked ANSI into the transcript.' }
'TRANSCRIPT_ORACLE_OK'
'@.Replace('__BEFORE__', (Join-Path $TestDrive 'before-transcript.txt').Replace("'", "''")).Replace('__AFTER__', (Join-Path $TestDrive 'after-transcript.txt').Replace("'", "''")))
        $text | Should -Match 'TRANSCRIPT_ORACLE_OK'
    }

    It 'keeps independently captured baselines through another runspace disable and module removal' {
        $text = Invoke-StreamsChild -Fixture (@'
function New-StreamsRunspaceHost {
    $data = [pscustomobject]@{
        WarningForegroundColor = [ConsoleColor]::Magenta
        WarningBackgroundColor = [ConsoleColor]::DarkMagenta
        VerboseForegroundColor = [ConsoleColor]::Magenta
        VerboseBackgroundColor = [ConsoleColor]::DarkMagenta
        DebugForegroundColor = [ConsoleColor]::Magenta
        DebugBackgroundColor = [ConsoleColor]::DarkMagenta
    }
    [ConsoleMonsterStreamsFixtures.StreamsHost]::new([psobject]::AsPSObject($data), $false)
}
$firstRunspace = [runspacefactory]::CreateRunspace((New-StreamsRunspaceHost))
$secondRunspace = [runspacefactory]::CreateRunspace((New-StreamsRunspaceHost))
$firstRunspace.Open()
$secondRunspace.Open()
$first = [powershell]::Create()
$first.Runspace = $firstRunspace
$second = [powershell]::Create()
$second.Runspace = $secondRunspace
function Invoke-StreamsRunspace($shell, $body) {
    $shell.Commands.Clear()
    $shell.Streams.Error.Clear()
    $result = $shell.AddScript($body).Invoke()
    if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
    $result
}
$setup = "Import-Module '__MODULE__' -ErrorAction Stop`n. '__SUPPORT__'`n"
try {
    Invoke-StreamsRunspace $first ($setup + 'Set-StreamsBaseline; $global:baseline = Get-StreamsState; Enable-ConsoleFormatting -Feature Streams; Assert-StreamsState (Get-StreamsExpected)')
    Invoke-StreamsRunspace $second ($setup + 'if ($PSVersionTable.PSVersion -lt [version] "7.2") { Set-StreamsBaseline -Variant 1 }; $global:baseline = Get-StreamsState; Enable-ConsoleFormatting -Feature Streams; Assert-StreamsState (Get-StreamsExpected)')
    Invoke-StreamsRunspace $first 'Disable-ConsoleFormatting -Feature Streams; if ($PSVersionTable.PSVersion -lt [version] "7.2") { Assert-StreamsState $global:baseline }; Remove-Module ConsoleMonster'
    Invoke-StreamsRunspace $second 'Assert-StreamsState (Get-StreamsExpected); Remove-Module ConsoleMonster'
    if ($PSVersionTable.PSVersion -lt [version] '7.2') { Invoke-StreamsRunspace $second 'Assert-StreamsState $global:baseline' }
    else { Invoke-StreamsRunspace $first 'Assert-StreamsState $global:baseline' }
    'RUNSPACE_BASELINES_OK'
} finally { $first.Dispose(); $second.Dispose(); $firstRunspace.Dispose(); $secondRunspace.Dispose() }
'@.Replace('__MODULE__', $script:StreamsModule.Replace("'", "''")).Replace('__SUPPORT__', $script:StreamsSupport.Replace("'", "''")))
        $text | Should -Match 'RUNSPACE_BASELINES_OK'
    }

    if ($PSVersionTable.PSVersion -lt [version] '7.2') {
        It 'leaves unavailable PrivateData hosts usable for <HostCase>' -ForEach @(
            @{ HostCase = 'missing data'; Throws = $false }
            @{ HostCase = 'throwing getter'; Throws = $true }
        ) {
            $text = Invoke-StreamsChild -Fixture (@'
$hostFixture = [ConsoleMonsterStreamsFixtures.StreamsHost]::new($null, __THROWS__)
$runspace = [runspacefactory]::CreateRunspace($hostFixture)
$shell = [powershell]::Create()
try {
    $runspace.Open()
    $shell.Runspace = $runspace
    $null = $shell.AddScript('Import-Module ''__MODULE__'' -ErrorAction Stop; Enable-ConsoleFormatting -Feature Streams; Disable-ConsoleFormatting -Feature Streams; Enable-ConsoleFormatting -Feature Streams; Remove-Module ConsoleMonster -ErrorAction Stop').Invoke()
    if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
    'UNAVAILABLE_HOST_OK'
} finally { $shell.Dispose(); $runspace.Dispose() }
'@.Replace('__THROWS__', ('$' + $Throws.ToString().ToLowerInvariant())).Replace('__MODULE__', $script:StreamsModule.Replace("'", "''").Replace("'", "''")))
            $text | Should -Match 'UNAVAILABLE_HOST_OK'
        }

        It 'restores only successfully written host properties when setters reject writes and RawUI is absent' {
            $text = Invoke-StreamsChild -Fixture (@'
$data = [ConsoleMonsterStreamsFixtures.StreamsPrivateData]::new()
$hostFixture = [ConsoleMonsterStreamsFixtures.StreamsHost]::new([psobject]::AsPSObject($data), $false)
$runspace = [runspacefactory]::CreateRunspace($hostFixture)
$shell = [powershell]::Create()
try {
    $runspace.Open()
    $shell.Runspace = $runspace
    $null = $shell.AddScript('Import-Module ''__MODULE__'' -ErrorAction Stop; Set-ConsoleTheme Dark; Enable-ConsoleFormatting -Feature Streams').Invoke()
    if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
    if ($data.WarningForegroundColor -ne [ConsoleColor]::Yellow) { throw 'A rejected sibling setter prevented the writable warning colour.' }
    if ($data.VerboseAttempts -ne 1 -or $data.VerboseWrites -ne 0) { throw 'The rejecting property was not exercised.' }
    if ($data.DebugForegroundColor -ne [ConsoleColor]::Blue) { throw 'A read-only property changed.' }
    if ($data.ProgressForegroundColor -ne [ConsoleColor]::Blue -or $data.ProgressBackgroundColor -ne [ConsoleColor]::DarkRed) { throw 'Optional progress properties did not respect host writability.' }
    if ($data.WarningBackgroundColor -ne [ConsoleColor]::DarkMagenta -or $data.DebugBackgroundColor -ne [ConsoleColor]::DarkBlue) { throw 'A missing RawUI supplied a background colour.' }
    $data.RejectVerbose = $false
    $data.WarningForegroundColor = [ConsoleColor]::White
    $data.VerboseForegroundColor = [ConsoleColor]::Cyan
    $data.WarningBackgroundColor = [ConsoleColor]::DarkGreen
    $data.ErrorForegroundColor = [ConsoleColor]::Red
    $data.ProgressForegroundColor = [ConsoleColor]::Magenta
    $shell.Commands.Clear()
    $null = $shell.AddScript('Disable-ConsoleFormatting -Feature Streams; Remove-Module ConsoleMonster -ErrorAction Stop').Invoke()
    if ($shell.HadErrors) { throw ($shell.Streams.Error | Out-String) }
    if ($data.WarningForegroundColor -ne [ConsoleColor]::Magenta) { throw 'The successfully owned property did not restore its original baseline.' }
    if ($data.VerboseForegroundColor -ne [ConsoleColor]::Cyan -or $data.VerboseWrites -ne 1) { throw 'Disable restored a failed write it did not own.' }
    if ($data.WarningBackgroundColor -ne [ConsoleColor]::DarkGreen) { throw 'Disable restored a background it did not own.' }
    if ($data.ErrorForegroundColor -ne [ConsoleColor]::Red) { throw 'Disable changed an unowned host error colour.' }
    if ($data.ProgressForegroundColor -ne [ConsoleColor]::DarkYellow -or $data.ProgressBackgroundColor -ne [ConsoleColor]::DarkRed) { throw 'Native progress ownership did not restore only writable colours.' }
    'SUCCESSFUL_PROPERTY_OWNERSHIP_OK'
} finally { $shell.Dispose(); $runspace.Dispose() }
'@.Replace('__MODULE__', $script:StreamsModule.Replace("'", "''").Replace("'", "''")))
            $text | Should -Match 'SUCCESSFUL_PROPERTY_OWNERSHIP_OK'
        }
    }
}
