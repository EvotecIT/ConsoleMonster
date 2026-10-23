Describe 'Unavailable Out-GridView fallback' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-GridViewSession.ps1')
    }

    It 'uses the terminal fallback for the Microsoft desktop stub on non-Windows hosts' -Skip:($env:OS -eq 'Windows_NT') {
        Invoke-GridViewSession -Native @'
$native = Get-Command Out-GridView -CommandType Cmdlet
if ($native.ImplementingType.FullName -ne 'Microsoft.PowerShell.Commands.OutGridViewCommand') { throw 'The desktop stub baseline is missing.' }
$options = [ConsoleMonster.Interactive.InteractiveOptions]::new()
$options.Capture = [ConsoleMonster.Interactive.ScreenCapture]::new(80, 22)
$options.Input = [ConsoleMonster.Input.ScriptedInput]::new('F10')
[ConsoleMonster.Interactive.InteractiveOptions]::Default = $options
$row = [pscustomobject]@{ Name = 'alpha'; Id = 1 }
Enable-ConsoleFormatting -Feature GridView
if ((Get-Command Out-GridView).CommandType -ne 'Function') { throw 'The unusable desktop stub won command resolution.' }
$picked = @($row | Out-GridView -PassThru -ErrorAction Stop)
if ($picked.Count -ne 1 -or -not [object]::ReferenceEquals($picked[0], $row)) { throw 'Terminal selection lost the original row.' }
Disable-ConsoleFormatting
if ((Get-Command Out-GridView -CommandType Cmdlet).ImplementingType -ne $native.ImplementingType) { throw 'The native stub was not restored.' }
'STUB_FALLBACK_OK'
'@ | Should -Be 'STUB_FALLBACK_OK'
    }

    It 'keeps an available native cmdlet and every existing command unchanged, even with Force' -Skip:($env:OS -ne 'Windows_NT') {
        Invoke-GridViewSession -Native @'
$before = Get-Command Out-GridView -CommandType Cmdlet -ErrorAction Ignore
if (-not $before) { throw 'The Windows native baseline is missing.' }
function global:Out-GridView { 'user native wrapper' }
$saved = ${function:global:Out-GridView}
Enable-ConsoleFormatting -Feature GridView -Force
if ($saved -cne ${function:global:Out-GridView}) { throw 'Native wrapper changed.' }
if ((Get-Command Out-GridView -CommandType Cmdlet).ImplementingType -ne $before.ImplementingType) { throw 'Native changed.' }
Disable-ConsoleFormatting
if ($saved -cne ${function:global:Out-GridView}) { throw 'Native wrapper changed on disable.' }
'NATIVE_OK'
'@ | Should -Be 'NATIVE_OK'
    }

    It 'requires explicit opt-in, excludes GridView from All, and installs no Out-Default or Write proxy' {
        Invoke-GridViewSession @'
if (Get-Command Out-GridView -ErrorAction Ignore) { throw 'Missing-cmdlet namespace is not isolated.' }
if (([int][ConsoleMonster.PowerShell.ConsoleFormattingFeature]::All -band 256) -ne 0) { throw 'GridView is in All.' }
$write = @(Get-Command Write-* | ForEach-Object { $_.Name + ':' + $_.CommandType + ':' + $_.Definition }) -join "`n"
$default = ${function:global:Out-Default}
Enable-ConsoleFormatting -Feature GridView
if ((Get-Command Out-GridView).CommandType -ne 'Function') { throw 'Fallback missing.' }
if ($default -cne ${function:global:Out-Default}) { throw 'GridView installed Out-Default.' }
if ($write -cne (@(Get-Command Write-* | ForEach-Object { $_.Name + ':' + $_.CommandType + ':' + $_.Definition }) -join "`n")) { throw 'Write command changed.' }
Disable-ConsoleFormatting -Feature All
if (-not (Get-Command Out-GridView -ErrorAction Ignore)) { throw 'All removed opt-in.' }
Disable-ConsoleFormatting
if (Get-Command Out-GridView -ErrorAction Ignore) { throw 'Fallback not removed.' }
'OPT_IN_OK'
'@ | Should -Be 'OPT_IN_OK'
    }

    It 'prints a profile snippet without changing commands' {
        Invoke-GridViewSession @'
$snippet = Enable-ConsoleFormatting -Feature GridView -ProfileSnippet
if (($snippet -join "`n") -notmatch 'Enable-ConsoleFormatting -Feature GridView') { throw 'Snippet lost feature.' }
if (Get-Command Out-GridView -ErrorAction Ignore) { throw 'Snippet installed commands.' }
'SNIPPET_OK'
'@ | Should -Be 'SNIPPET_OK'
    }

    It 'recognizes an SSH context with a present Windows cmdlet without changing that cmdlet' {
        $previous = [Environment]::GetEnvironmentVariable('SSH_CONNECTION')
        try {
            [Environment]::SetEnvironmentVariable('SSH_CONNECTION', '192.0.2.1 1234 192.0.2.2 22')
            Invoke-GridViewSession -Native @'
$native = Get-Command Out-GridView -CommandType Cmdlet
Enable-ConsoleFormatting -Feature GridView
if ((Get-Command Out-GridView).CommandType -ne 'Function') { throw 'SSH did not get a terminal fallback.' }
Disable-ConsoleFormatting
if ((Get-Command Out-GridView) -ne $native) { throw 'Native SSH command was changed.' }
'SSH_OK'
'@ | Should -Be 'SSH_OK'
        } finally { [Environment]::SetEnvironmentVariable('SSH_CONNECTION', $previous) }
    }

    It 'keeps custom aliases and functions without Force and restores their metadata exactly with Force' {
        Invoke-GridViewSession @'
function global:Out-GridView { param($a) 'custom-function:' + $a }
$function = Get-Item Function:\Out-GridView
$function.Description = 'original function description'
$body = $function.ScriptBlock
Set-Alias -Name Out-GridView -Value Get-Date -Scope Global -Option ReadOnly -Description 'original alias description'
$warnings = @()
Enable-ConsoleFormatting -Feature GridView -WarningVariable warnings
if ($warnings.Count -ne 1 -or (Get-Alias Out-GridView).Definition -ne 'Get-Date') { throw 'Custom alias was not kept.' }
Enable-ConsoleFormatting -Feature GridView -Force
Enable-ConsoleFormatting -Feature GridView -Force
Disable-ConsoleFormatting -Feature GridView
$alias = Get-Alias Out-GridView
$restored = Get-Item Function:\Out-GridView
if ($alias.Definition -ne 'Get-Date' -or $alias.Options -ne 'ReadOnly' -or $alias.Description -ne 'original alias description') { throw 'Alias metadata differs.' }
if ($body -cne $restored.ScriptBlock -or $restored.Description -ne 'original function description') { throw 'Function metadata differs.' }
Remove-Item Alias:\Out-GridView -Force
if ((Out-GridView abc) -ne 'custom-function:abc') { throw 'Function behavior lost.' }
'RESTORE_OK'
'@ | Should -Be 'RESTORE_OK'
    }

    It 'keeps a pre-import <Options> alias with an accurate warning instead of claiming success' -ForEach @(
        @{ Options = 'AllScope' }
        @{ Options = 'ReadOnly, AllScope' }
    ) {
        $body = @'
$savedModulePath = (Get-Module ConsoleMonster).Path
Remove-Module ConsoleMonster
Set-Alias -Name Out-GridView -Value Get-Date -Scope Global -Option '__OPTIONS__' -Description 'pre-import global alias'
$original = Get-Alias Out-GridView -Scope Global
$definition = $original.Definition
$options = $original.Options
$description = $original.Description
$visibility = $original.Visibility
Import-Module $savedModulePath
$warnings = @()
Enable-ConsoleFormatting -Feature GridView -Force -WarningVariable warnings
if ($warnings.Count -ne 1 -or $warnings[0].Message -notmatch 'cannot be replaced in this scope') { throw 'The unsupported alias scope was not reported.' }
if (${function:global:Out-GridView}) { throw 'An unreachable fallback function was left installed.' }
if ((Get-Command Out-GridView).CommandType -ne 'Alias') { throw 'Original alias resolution changed.' }
Disable-ConsoleFormatting
Remove-Module ConsoleMonster
$restored = Get-Alias Out-GridView -Scope Global
if ($restored.Definition -cne $definition -or $restored.Options -ne $options -or $restored.Description -cne $description -or $restored.Visibility -ne $visibility) { throw 'Original alias metadata changed.' }
'ALLSCOPE_KEPT_OK'
'@.Replace('__OPTIONS__', $Options)
        Invoke-GridViewSession $body | Should -Be 'ALLSCOPE_KEPT_OK'
    }

    It 'rolls back a global installation when a caller-local alias still wins resolution' {
        Invoke-GridViewSession @'
Set-Alias -Name Out-GridView -Value Get-Date -Scope Global
function Invoke-LocalGridProbe {
    Set-Alias -Name Out-GridView -Value Write-Output -Scope Local
    $warnings = @()
    Enable-ConsoleFormatting -Feature GridView -Force -WarningVariable warnings
    if ($warnings.Count -ne 1) { throw 'Local command precedence was not reported.' }
    if ((Get-Command Out-GridView).Definition -ne 'Write-Output') { throw 'Caller-local alias was changed.' }
    if (${function:global:Out-GridView}) { throw 'Unreachable global fallback survived rollback.' }
}
Invoke-LocalGridProbe
if ((Get-Alias Out-GridView -Scope Global).Definition -ne 'Get-Date') { throw 'Rollback did not restore the global alias.' }
Disable-ConsoleFormatting
'LOCAL_ALIAS_ROLLBACK_OK'
'@ | Should -Be 'LOCAL_ALIAS_ROLLBACK_OK'
    }

    It 'restores on module removal and repeated enable/disable without changing later user commands' {
        Invoke-GridViewSession @'
function global:Out-GridView { 'original' }
Enable-ConsoleFormatting -Feature GridView -Force
Disable-ConsoleFormatting
Enable-ConsoleFormatting -Feature GridView -Force
Remove-Module ConsoleMonster
if ((Out-GridView) -ne 'original') { throw 'Module removal lost function.' }
'REMOVE_OK'
'@ | Should -Be 'REMOVE_OK'
        Invoke-GridViewSession @'
Enable-ConsoleFormatting -Feature GridView
function global:Out-GridView { 'new user function' }
Disable-ConsoleFormatting
if ((Out-GridView) -ne 'new user function') { throw 'Later user replacement was removed.' }
'USER_OK'
'@ | Should -Be 'USER_OK'
    }

    It 'maps <Mode> to the existing grid and returns the original selected objects' -ForEach @(
        @{ Mode = 'Single'; Arguments = '-OutputMode Single'; Keys = 'Down F10'; Expected = 'beta' }
        @{ Mode = 'Multiple'; Arguments = '-OutputMode Multiple'; Keys = 'Space Down Space F10'; Expected = 'alpha,beta' }
        @{ Mode = 'PassThru'; Arguments = '-PassThru'; Keys = 'Space Down Space F10'; Expected = 'alpha,beta' }
        @{ Mode = 'Browse'; Arguments = ''; Keys = 'Escape'; Expected = '' }
        @{ Mode = 'Wait'; Arguments = '-Wait'; Keys = 'Escape'; Expected = '' }
    ) {
        $body = @'
$options = [ConsoleMonster.Interactive.InteractiveOptions]::new()
$capture = [ConsoleMonster.Interactive.ScreenCapture]::new(80, 22)
$options.Capture = $capture
$options.Input = [ConsoleMonster.Input.ScriptedInput]::new('__KEYS__')
[ConsoleMonster.Interactive.InteractiveOptions]::Default = $options
$values = @([pscustomobject]@{ Name = 'alpha'; Id = 1 }, [pscustomobject]@{ Name = 'beta'; Id = 2 })
Enable-ConsoleFormatting -Feature GridView
$selected = @($values | Out-GridView -Title 'My selection' __ARGS__)
if (($selected.Name -join ',') -cne '__EXPECTED__') { throw ('Wrong selection: ' + ($selected.Name -join ',')) }
foreach ($row in $selected) { if (-not [object]::ReferenceEquals($row, $values[$row.Id - 1])) { throw 'Object identity lost.' } }
if (($capture.Frames | ForEach-Object { $_.ToPlainText() }) -join "`n" -notmatch 'My selection') { throw 'Title not mapped.' }
Disable-ConsoleFormatting
'SELECTION_OK'
'@.Replace('__KEYS__', $Keys).Replace('__ARGS__', $Arguments).Replace('__EXPECTED__', $Expected)
        Invoke-GridViewSession $body | Should -Be 'SELECTION_OK'
    }

    It 'keeps native parameter-set conflicts and rejects invalid output modes' {
        Invoke-GridViewSession @'
Enable-ConsoleFormatting -Feature GridView
foreach ($arguments in @(@{PassThru=$true;Wait=$true}, @{PassThru=$true;OutputMode='Single'}, @{OutputMode='invalid'})) {
    $failed = $false
    try { 1 | Out-GridView @arguments } catch [Management.Automation.ParameterBindingException] { $failed = $true }
    if (-not $failed) { throw 'Invalid arguments accepted.' }
}
'BINDING_OK'
'@ | Should -Be 'BINDING_OK'
    }

    It 'maps explicit InputObject and output-variable captures without losing collection items' {
        Invoke-GridViewSession @'
$options = [ConsoleMonster.Interactive.InteractiveOptions]::new()
$options.Capture = [ConsoleMonster.Interactive.ScreenCapture]::new(80, 22)
$options.Input = [ConsoleMonster.Input.ScriptedInput]::new('Ctrl+A F10')
[ConsoleMonster.Interactive.InteractiveOptions]::Default = $options
$values = @([pscustomobject]@{Name='alpha'}, [pscustomobject]@{Name='beta'})
Enable-ConsoleFormatting -Feature GridView
$selected = @(Out-GridView -InputObject $values -PassThru -OutVariable captured)
if (($selected.Name -join ',') -ne 'alpha,beta' -or ($captured.Name -join ',') -ne 'alpha,beta') { throw 'Explicit collection/capture lost rows.' }
'INPUT_OK'
'@ | Should -Be 'INPUT_OK'
    }

    It 'restores a function without an alias and preserves a user alias added while enabled' {
        Invoke-GridViewSession @'
function global:Out-GridView { 'original' }
Enable-ConsoleFormatting -Feature GridView -Force
Disable-ConsoleFormatting -Feature GridView
if ((Out-GridView) -ne 'original') { throw 'Original function lost.' }
Enable-ConsoleFormatting -Feature GridView -Force
Set-Alias -Name Out-GridView -Value Get-Date -Scope Global -Description 'later alias'
Disable-ConsoleFormatting
if ((Get-Alias Out-GridView).Description -ne 'later alias') { throw 'Later alias lost.' }
Remove-Item Alias:\Out-GridView
if ((Out-GridView) -ne 'original') { throw 'Original function under later alias lost.' }
'LATER_OK'
'@ | Should -Be 'LATER_OK'
    }

    It 'restores ReadOnly and AllScope function options and keeps constants even with Force' {
        Invoke-GridViewSession @'
Set-Item Function:\global:Out-GridView -Value { 'original-options' } -Options ReadOnly,AllScope
Enable-ConsoleFormatting -Feature GridView -Force
Disable-ConsoleFormatting
if ((Out-GridView) -ne 'original-options' -or (Get-Item Function:\Out-GridView).Options -ne 'ReadOnly, AllScope') { throw 'Function options lost.' }
'OPTIONS_OK'
'@ | Should -Be 'OPTIONS_OK'
        Invoke-GridViewSession @'
Set-Alias -Name Out-GridView -Value Get-Date -Scope Global -Option Constant
$warnings=@()
Enable-ConsoleFormatting -Feature GridView -Force -WarningVariable warnings
if ($warnings.Count -ne 1 -or (Get-Alias Out-GridView).Options -ne 'Constant') { throw 'Constant was replaced.' }
Disable-ConsoleFormatting
'CONSTANT_OK'
'@ | Should -Be 'CONSTANT_OK'
    }

    It 'uses the existing noninteractive table fallback with no invented selections for captures and jobs' {
        Invoke-GridViewSession @'
[ConsoleMonster.Interactive.InteractiveOptions]::Default = [ConsoleMonster.Interactive.InteractiveOptions]::new()
[ConsoleMonster.Interactive.InteractiveOptions]::Default.NonInteractive = $true
Enable-ConsoleFormatting -Feature GridView
$values = @([pscustomobject]@{Name='alpha'; Id=1}, [pscustomobject]@{Name='beta';Id=2})
$warnings = @()
$capture = @($values | Out-GridView -PassThru -WarningVariable warnings)
if ($capture.Count -ne 0 -or $warnings.Count -ne 1) { throw 'Noninteractive result differs.' }
$text = $values | Out-GridView -OutputMode Single -WarningAction SilentlyContinue | Out-String
if ($text) { throw 'Display text leaked into success output.' }
Disable-ConsoleFormatting
'FALLBACK_OK'
'@ | Should -Be 'FALLBACK_OK'
    }
}
