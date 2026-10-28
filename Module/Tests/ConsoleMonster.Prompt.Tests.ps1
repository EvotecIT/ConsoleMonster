Describe 'Opt-in shell prompt contracts' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:PromptRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:PromptModule = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        function Invoke-PromptChild {
            param([string] $Script)
            $source = "`$ErrorActionPreference = 'Stop'`nImport-Module '$($script:PromptModule.Replace("'", "''"))' -Force`ntry {`n" + $Script + "`n} catch { [Console]::Out.WriteLine(`$_.ToString()); [Console]::Out.WriteLine(`$_.ScriptStackTrace); exit 1 }"
            $result = Invoke-TestChild -Runtime $script:PromptRuntime -Script $source
            if ($LASTEXITCODE -ne 0) { throw ($result -join "`n") }
            $result -join "`n"
        }
    }

    It 'prints an explicit Prompt profile snippet without enabling it and keeps Prompt out of the default set' {
        Invoke-PromptChild @'
$original = ${function:global:prompt}
$snippet = Enable-ConsoleFormatting -Feature Prompt -ProfileSnippet
if (($snippet -join "`n") -notmatch 'Enable-ConsoleFormatting -Feature Prompt') { throw 'Missing opt-in' }
if ([int]((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.ConsoleFormattingFeature'))::Prompt -ne 512) { throw 'Wrong reserved bit' }
Enable-ConsoleFormatting
if ((Get-ConsoleFormatting).Features -contains 'Prompt') { throw 'Defaults include Prompt' }
Disable-ConsoleFormatting
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Snippet installed a prompt' }
'OPT_IN_OK'
'@ | Should -Match 'OPT_IN_OK'
    }

    It 'restores the exact original closure on <Ending> after repeated enables' -ForEach @(
        @{ Ending = 'Disable-ConsoleFormatting -Feature Prompt' }
        @{ Ending = 'Disable-ConsoleFormatting' }
        @{ Ending = 'Remove-Module ConsoleMonster -Force' }
    ) {
        $source = @'
$privateValue = 'ORIGINAL_CLOSURE'
$original = { $privateValue }.GetNewClosure()
Set-Item Function:global:prompt $original
Enable-ConsoleFormatting -Feature Prompt
Enable-ConsoleFormatting -Feature Prompt
if (${function:global:prompt}.ToString() -ceq $original.ToString()) { throw 'Prompt was not installed' }
__ENDING__
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Definition not restored' }
if ((prompt) -cne 'ORIGINAL_CLOSURE') { throw 'Closure scope not restored' }
'RESTORE_OK'
'@
        Invoke-PromptChild $source.Replace('__ENDING__', $Ending) | Should -Match 'RESTORE_OK'
    }

    It 'keeps a protected <Engine> prompt unless Force is explicit' -ForEach @(
        @{ Engine = 'oh-my-posh' }
        @{ Engine = 'starship' }
    ) {
        $source = @'
$original = [scriptblock]::Create("'__ENGINE__ ORIGINAL'")
Set-Item Function:global:prompt $original
Enable-ConsoleFormatting -Feature Prompt -WarningVariable warnings
if (@($warnings).Count -ne 1 -or ${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Protected prompt replaced' }
Enable-ConsoleFormatting -Feature Prompt -Force
if (${function:global:prompt}.ToString() -ceq $original.ToString()) { throw 'Force did not install' }
Disable-ConsoleFormatting
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Force lost original' }
'PROTECTED_OK'
'@
        Invoke-PromptChild $source.Replace('__ENGINE__', $Engine) | Should -Match 'PROTECTED_OK'
    }

    It 'preserves later user replacement and does not install core or stream proxies' {
        Invoke-PromptChild @'
$names = 'Out-Default','Write-Error','Write-Warning','Write-Verbose','Write-Debug','Write-Information','Write-Progress','Get-History'
$before = @($names | ForEach-Object { (Get-Command $_).Definition }) -join "`n"
Enable-ConsoleFormatting -Feature Prompt
$after = @($names | ForEach-Object { (Get-Command $_).Definition }) -join "`n"
if ($before -cne $after) { throw 'A core command changed' }
function global:prompt { 'LATER_USER_PROMPT' }
Disable-ConsoleFormatting
if ((prompt) -cne 'LATER_USER_PROMPT') { throw 'User replacement lost' }
'OWNERSHIP_OK'
'@ | Should -Match 'OWNERSHIP_OK'
    }

    It 'establishes failed native process state with exit code <ExitCode> independently of Prompt' -ForEach @(
        @{ ExitCode = 7 }
        @{ ExitCode = 11 }
    ) {
        $source = @'
$Error.Clear()
& ([Diagnostics.Process]::GetCurrentProcess().Path) -NoLogo -NoProfile -NonInteractive -Command 'exit __EXIT__'
$state = "$?/$LASTEXITCODE"
if ($state -cne 'False/__EXIT__' -or $Error.Count -ne 0) { throw "Native fixture did not establish failed process state: $state/$($Error.Count)" }
'NATIVE_EXIT_OK'
'@
        Invoke-PromptChild $source.Replace('__EXIT__', [string]$ExitCode) | Should -Match 'NATIVE_EXIT_OK'
    }

    It 'keeps captured prompt text, native error state, exit status and preferences' {
        Invoke-PromptChild @'
$native = [Diagnostics.Process]::GetCurrentProcess().Path
function global:prompt {
    "ORIGINAL:$?/$global:LASTEXITCODE"
    if ($args.Count) { 'ARGS:' + ($args -join '|') }
}
$Error.Clear()
$baseline = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
Enable-ConsoleFormatting -Feature Prompt
& $native -NoLogo -NoProfile -NonInteractive -Command 'exit 7'
$assigned = prompt
if ($assigned -cne 'ORIGINAL:False/7' -or $LASTEXITCODE -ne 7) { throw "Status lost: $assigned/$LASTEXITCODE" }
& $native -NoLogo -NoProfile -NonInteractive -Command 'exit 7'
$piped = prompt | Out-String
if ($piped.TrimEnd() -cne 'ORIGINAL:False/7') { throw "Piped status lost: $piped" }
$arguments = @(prompt 'one' 'two')
if ($arguments[-1] -cne 'ARGS:one|two') { throw 'Original prompt arguments lost' }
if ($Error.Count -ne 0) { throw "Prompt changed Error: $($Error[0])" }
$after = $ErrorActionPreference, $WarningPreference, $VerbosePreference, $DebugPreference, $InformationPreference, $ProgressPreference
if (($baseline -join '|') -cne ($after -join '|')) { throw 'Preferences changed' }
Set-Variable LASTEXITCODE -Value 7 -Scope Global -Option ReadOnly
$text = prompt
if ($text -cne 'ORIGINAL:True/7') { throw 'Read-only exit state changed' }
Remove-Variable LASTEXITCODE -Scope Global -Force
Disable-ConsoleFormatting
'CAPTURE_STATE_OK'
'@ | Should -Match 'CAPTURE_STATE_OK'
    }

    It 'preserves original native prompt errors through <Route>' -ForEach @(
        @{ Route = 'SimpleMerge' }
        @{ Route = 'Merge' }
        @{ Route = 'AllStreams' }
        @{ Route = 'Suppress' }
        @{ Route = 'File' }
        @{ Route = 'ErrorVariable' }
        @{ Route = 'AppendVariables' }
        @{ Route = 'PreferenceContinue' }
        @{ Route = 'PreferenceSilent' }
        @{ Route = 'Stop' }
        @{ Route = 'Throw' }
        @{ Route = 'Recursive' }
        @{ Route = 'Recording' }
    ) {
        $helper = Join-Path $PSScriptRoot 'Support/Assert-PromptFallback.ps1'
        $source = ". '$($helper.Replace("'", "''"))'`nAssert-PromptFallback -Route '$Route'"
        Invoke-PromptChild $source | Should -Match $Route
    }

    It 'restores an absent original prompt and absent LASTEXITCODE' {
        Invoke-PromptChild @'
Remove-Item Function:prompt
Remove-Variable LASTEXITCODE -Scope Global -ErrorAction Ignore
Enable-ConsoleFormatting -Feature Prompt
$text = prompt
if ($text -notlike 'PS *> ') { throw "Default fallback lost: $text" }
if ($null -ne $ExecutionContext.SessionState.PSVariable.Get('global:LASTEXITCODE')) { throw 'Created LASTEXITCODE' }
Disable-ConsoleFormatting
if ($null -ne ${function:global:prompt}) { throw 'Created an original prompt' }
'ABSENT_OK'
'@ | Should -Match 'ABSENT_OK'
    }

    It 'leaves Prompt enabled when Disable All removes only the default features' {
        Invoke-PromptChild @'
$original = ${function:global:prompt}
Enable-ConsoleFormatting -Feature Prompt
$installed = ${function:global:prompt}.ToString()
Disable-ConsoleFormatting -Feature Output, FormatCommands, Errors, Streams, ReadLine
if (${function:global:prompt}.ToString() -cne $installed) { throw 'All removed explicit prompt' }
Disable-ConsoleFormatting
if (${function:global:prompt}.ToString() -cne $original.ToString()) { throw 'Final disable lost prompt' }
'SELECTIVE_OK'
'@ | Should -Match 'SELECTIVE_OK'
    }

    It 'caches git with a controlled clock, coalesces probes and never shows another directory state' {
        Invoke-PromptChild @'
$flags = [Reflection.BindingFlags]'Instance,Static,NonPublic,Public'
$type = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletEnableConsoleFormatting')).Assembly.GetType('ConsoleMonster.PowerShell.PromptGitCache')
$script:now = 100L
$script:probes = 0
$queue = [Collections.Generic.List[Action]]::new()
$clock = [Func[long]] { $script:now }
$probe = [Func[string,ValueTuple[string,bool]]] { param($path) $script:probes++; [ValueTuple[string,bool]]::new($path, $true) }
$schedule = [Action[Action]] { param($action) $queue.Add($action) }
$constructor = $type.GetConstructors($flags) | Where-Object { $_.GetParameters().Count -eq 3 }
$cache = $constructor.Invoke(@($clock, $probe, $schedule))
$read = $type.GetMethod('Read', $flags)
$null = $read.Invoke($cache, @('/one'))
$null = $read.Invoke($cache, @('/one'))
if ($queue.Count -ne 1) { throw 'Concurrent probe duplicate' }
$queue[0].Invoke()
$queue.Clear()
$result = $read.Invoke($cache, @('/one'))
if ($result.Item1 -cne '/one' -or -not $result.Item2 -or $script:probes -ne 1) { throw 'Cached result missing' }
$script:now = 5099L
$null = $read.Invoke($cache, @('/one'))
if ($queue.Count -ne 0) { throw 'Cache expired early' }
$script:now = 5100L
$null = $read.Invoke($cache, @('/one'))
if ($queue.Count -ne 1) { throw 'Cache did not expire' }
$result = $read.Invoke($cache, @('/two'))
if ($null -ne $result.Item1) { throw 'Old directory state leaked' }
$cache.Dispose()
$queue[0].Invoke()
$null = $read.Invoke($cache, @('/two'))
if ($queue.Count -ne 1) { throw 'Disposed cache queued work' }
$parse = $type.GetMethod('ParseBranch', $flags)
if ($parse.Invoke($null, @('## main...origin/main [ahead 1]')) -cne 'main') { throw 'Upstream metadata leaked' }
if ($parse.Invoke($null, @('## No commits yet on new')) -cne 'new') { throw 'Unborn branch lost' }
'CACHE_OK'
'@ | Should -Match 'CACHE_OK'
    }

    It 'keeps installations and original prompt restoration local to each runspace' {
        $source = @'
$first = [powershell]::Create()
$second = [powershell]::Create()
try {
    # The setup is literal source in each runspace, rather than interpolation of the test's own prompt.
    $setup = 'Import-Module ''__MODULE__''; function global:prompt { ''__VALUE__'' }; Enable-ConsoleFormatting -Feature Prompt; ${function:global:prompt}.ToString()'
    $a = $first.AddScript($setup.Replace('__VALUE__', 'FIRST')).Invoke()
    $b = $second.AddScript($setup.Replace('__VALUE__', 'SECOND')).Invoke()
    $first.Commands.Clear()
    $restored = $first.AddScript('Disable-ConsoleFormatting; prompt').Invoke()
    if ([string]$restored[-1] -cne 'FIRST') { throw 'First original lost' }
    $second.Commands.Clear()
    $still = $second.AddScript('${function:global:prompt}.ToString()').Invoke()
    if ([string]$still[-1] -cne [string]$b[-1]) { throw 'Other runspace installation changed' }
    $second.Commands.Clear()
    $restored = $second.AddScript('Disable-ConsoleFormatting; prompt').Invoke()
    if ([string]$restored[-1] -cne 'SECOND') { throw 'Second original lost' }
    if ($first.HadErrors -or $second.HadErrors) { throw 'Runspace errors' }
} finally {
    $first.Dispose()
    $second.Dispose()
}
'RUNSPACES_OK'
'@
        Invoke-PromptChild $source.Replace('__MODULE__', $script:PromptModule.Replace("'", "''")) | Should -Match 'RUNSPACES_OK'
    }
}
