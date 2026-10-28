Describe 'Enable-ConsoleFormatting features (Errors, Progress) and an exact Disable' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (plain text) and en-US culture.
        function Invoke-DefaultsChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')"
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    It 'reports short enable and disable operations without exposing proxy source' {
        $text = Invoke-DefaultsChild @'
$VerbosePreference = 'SilentlyContinue'
$before = $VerbosePreference
$features = @('Output', 'FormatCommands', 'Progress', 'Help', 'Prompt', 'GridView')
$on = @(Enable-ConsoleFormatting -Feature $features -Force -Verbose 4>&1)
if ((Get-Command Format-Table).CommandType -ne 'Function') { throw 'Format-Table was not enabled.' }
if ((Get-Command Write-Progress).CommandType -ne 'Function') { throw 'Progress was not enabled.' }
$off = @(Disable-ConsoleFormatting -Verbose 4>&1)
if ((Get-Command Format-Table).CommandType -ne 'Cmdlet') { throw 'Format-Table was not restored.' }
if ((Get-Command Write-Progress).CommandType -ne 'Cmdlet') { throw 'Progress was not restored.' }
if ($VerbosePreference -ne $before) { throw 'VerbosePreference changed.' }
$messages = @($on + $off | ForEach-Object { [string] $_ })
if (($messages -join "`n") -match '(?i)\[CmdletBinding|\bparam\s*\(|Set Item|function:\\') { throw 'Internal function source appeared in verbose output.' }
if (-not ($on -match 'operation "Enable" on target "ConsoleMonster formatting: Output, FormatCommands, Progress, Help, GridView, Prompt"')) { throw 'Missing enable description.' }
if (-not ($off -match 'operation "Disable" on target "ConsoleMonster formatting:')) { throw 'Missing disable description.' }
'VERBOSE_OK'
'@
        $text | Should -Match 'VERBOSE_OK'
    }

    It 'keeps formatting unchanged for WhatIf and supports explicit confirmation' {
        $text = Invoke-DefaultsChild @'
$before = (Get-Command Format-Table).CommandType
Enable-ConsoleFormatting -Feature Output, FormatCommands, Progress -WhatIf
if ((Get-Command Format-Table).CommandType -ne $before) { throw 'WhatIf installed Format-Table.' }
if ((Get-Command Write-Progress).CommandType -ne 'Cmdlet') { throw 'WhatIf installed Progress.' }
Enable-ConsoleFormatting -Feature FormatCommands -Confirm:$false
Disable-ConsoleFormatting -Feature FormatCommands -WhatIf
if ((Get-Command Format-Table).CommandType -ne 'Function') { throw 'WhatIf removed Format-Table.' }
Disable-ConsoleFormatting -Feature FormatCommands -Confirm:$false
if ((Get-Command Format-Table).CommandType -ne $before) { throw 'Confirmed disable did not restore Format-Table.' }
$WhatIfPreference = $true
Enable-ConsoleFormatting -Feature FormatCommands
if ((Get-Command Format-Table).CommandType -ne $before) { throw 'WhatIfPreference installed Format-Table.' }
'WHATIF_OK'
'@
        $text | Should -Match 'operation "Enable" on target "ConsoleMonster formatting: Output, FormatCommands, Progress"'
        $text | Should -Match 'operation "Disable" on target "ConsoleMonster formatting: FormatCommands"'
        $text | Should -Not -Match '(?i)\[CmdletBinding|\bparam\s*\(|Set Item'
        $text | Should -Match 'WHATIF_OK'
    }

    It 'keeps foreign progress aliases while restoring each runspace output function' {
        $text = Invoke-DefaultsChild @'
$first = [powershell]::Create()
$second = [powershell]::Create()
try {
    $setup = {
        param($modulePath, $label, $alias)
        Import-Module $modulePath
        Set-Item function:global:Out-Default ([scriptblock]::Create("'$label'"))
        Set-Alias Write-Progress $alias -Scope Global
        Enable-ConsoleFormatting -Feature Output, Errors, Progress -Force
    }
    $null = $first.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('FIRST').AddArgument('Write-Output').Invoke()
    $null = $second.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('SECOND').AddArgument('Write-Host').Invoke()
    $first.Commands.Clear()
    $first.AddScript('Disable-ConsoleFormatting -Feature Progress; "FIRST_ALIAS=$((Get-Alias Write-Progress).Definition)"').Invoke()
    $second.Commands.Clear()
    $second.AddScript('"SECOND_STILL_ENABLED=$((Get-Command Out-Default).Definition -match ''ConsoleMonster'') SECOND_ALIAS=$((Get-Alias Write-Progress).Definition)"; Disable-ConsoleFormatting; "SECOND_FUNCTION=$((Get-Command Out-Default).Definition) SECOND_RESTORED_ALIAS=$((Get-Alias Write-Progress).Definition)"').Invoke()
    $first.Commands.Clear()
    $first.AddScript('Disable-ConsoleFormatting -Feature Errors; "FIRST_OUTPUT_STILL_ENABLED=$((Get-Command Out-Default).Definition -match ''ConsoleMonster'')"; Disable-ConsoleFormatting -Feature Output; "FIRST_FUNCTION=$((Get-Command Out-Default).Definition)"').Invoke()
} finally {
    $first.Dispose()
    $second.Dispose()
}
'@
        $text | Should -Match 'FIRST_ALIAS=Write-Output'
        $text | Should -Match 'FIRST_OUTPUT_STILL_ENABLED=True'
        $text | Should -Match "FIRST_FUNCTION='FIRST'"
        $text | Should -Match 'SECOND_STILL_ENABLED=True SECOND_ALIAS=Write-Host'
        $text | Should -Match "SECOND_FUNCTION='SECOND' SECOND_RESTORED_ALIAS=Write-Host"
    }

    It 'restores the replaced function and prior removal hook for <feature>' -ForEach @(
        @{ feature = 'Output' }
        @{ feature = 'Errors' }
        @{ feature = 'Output, FormatCommands, Errors, Streams, ReadLine' }
    ) {
        $text = Invoke-DefaultsChild (@'
function global:Out-Default { 'ORIGINAL_CALLBACK' }
$owner = (Get-Command Enable-ConsoleFormatting).Module
$owner.OnRemove = { 'PREVIOUS_REMOVE' }
Enable-ConsoleFormatting -Feature __FEATURE__ -Force
Remove-Module ConsoleMonster
"RESTORED_FUNCTION=$((Get-Command Out-Default -CommandType Function).Definition)"
'@.Replace('__FEATURE__', $feature))
        $text | Should -Match 'RESTORED_FUNCTION=.*ORIGINAL_CALLBACK'
        $text | Should -Match 'PREVIOUS_REMOVE'
    }

    It 'restores the latest alias on module removal after disabling and enabling again' {
        $text = Invoke-DefaultsChild @'
Set-Alias Write-Progress Write-Output -Scope Global
Enable-ConsoleFormatting -Feature Progress -Force
Disable-ConsoleFormatting
Set-Alias Write-Progress Write-Host -Scope Global
Enable-ConsoleFormatting -Feature Progress -Force
Remove-Module ConsoleMonster
"RESTORED_ALIAS=$((Get-Alias Write-Progress).Definition)"
'@
        $text | Should -Match 'RESTORED_ALIAS=Write-Host'
    }

    It 'installs a native-parameter Write-Progress function and restores the cmdlet' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
"PROXY=$((Get-Command Write-Progress).CommandType) NATIVE_ID=$((Get-Command Write-Progress).Parameters['Id'].ParameterType.FullName)"
foreach ($i in 1..3) { Write-Progress -Activity 'Copying' -PercentComplete ($i * 30) }
Write-Progress -Activity 'Copying' -Completed
"SHIM=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
Disable-ConsoleFormatting
"GONE=$(-not (Get-Alias Write-Progress -ErrorAction Ignore)) NOSHIM=$(-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $text | Should -Match 'PROXY=Function NATIVE_ID=System.Int32'
        $text | Should -Match 'SHIM=True'
        $text | Should -Match 'GONE=True NOSHIM=True'
    }

    It 'preserves a foreign progress alias even with Force and restores a forced output function' {
        $text = Invoke-DefaultsChild @'
function global:Write-MyProgress { param($Activity) }
Set-Alias -Name Write-Progress -Value Write-MyProgress -Scope Global
function global:Out-Default { process { $_ | Microsoft.PowerShell.Core\Out-Default } }
$definition = (Get-Command Out-Default -CommandType Function).Definition
Enable-ConsoleFormatting -Feature Progress -WarningVariable w -WarningAction SilentlyContinue
"KEPT=$((Get-Alias Write-Progress).Definition) WARNED=$($w.Count)"
Enable-ConsoleFormatting -Feature Output, Progress -Force
"KEPT_FORCE=$((Get-Alias Write-Progress).Definition) SHIM=$((Get-Command Out-Default -CommandType Function).Definition -match 'Out-ConsoleDefault')"
Disable-ConsoleFormatting
"RESTORED=$((Get-Alias Write-Progress).Definition) SAME=$((Get-Command Out-Default -CommandType Function).Definition -eq $definition)"
'@
        $text | Should -Match 'KEPT=Write-MyProgress WARNED=1'
        $text | Should -Match 'KEPT_FORCE=Write-MyProgress SHIM=True'
        $text | Should -Match 'RESTORED=Write-MyProgress SAME=True'
    }

    It 'shows errors with the ConsoleMonster error view, keeps CategoryView, and leaves other output alone without the Output feature' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Errors
'== ours'
Write-Error 'disk is full' 2>&1 | Out-ConsoleDefault -Force -Shim
'== category'
$ErrorView = 'CategoryView'
Write-Error 'disk is full' 2>&1 | Out-ConsoleDefault -Force -Shim
$ErrorView = $null
'== items'
Get-Item $PSHOME | Out-ConsoleDefault -Force -Shim
'== off'
Disable-ConsoleFormatting -Feature Errors
"SHIM=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $ours = ($text -split '== category')[0]
        $ours | Should -Match 'WriteErrorException'
        $ours | Should -Match 'disk is full'
        # CategoryView: PowerShell's own view (written to the error stream), not ours.
        (($text -split '== category')[1] -split '== items')[0] | Should -Not -Match 'WriteErrorException'
        # Without the Output feature, Get-Item keeps PowerShell's own view (no ConsoleMonster table border).
        (($text -split '== items')[1] -split '== off')[0] | Should -Not -Match '[┌╭]'
        $text | Should -Match 'SHIM=False'
    }

    It 'prints a profile snippet with the features' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Errors, Progress -ProfileSnippet
Enable-ConsoleFormatting -ProfileSnippet
'@
        $text | Should -Match '(?m)^Enable-ConsoleFormatting -Feature Errors, Progress$'
        $text | Should -Match '(?m)^Enable-ConsoleFormatting$'
    }

    It 'honours $ProgressPreference and writes PowerShell''s progress where the area cannot be drawn' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
$ProgressPreference = 'SilentlyContinue'
Write-Progress -Activity 'quiet'
$ProgressPreference = 'Continue'
Write-Progress -Activity 'loud' -Status 'half' -PercentComplete 50
'OK'
'@
        $text | Should -Match 'OK'
        $text | Should -Not -Match 'Exception'
    }

    It 'restores native Write-Progress on module removal and preserves custom functions unless Force is used' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
Remove-Module ConsoleMonster
"GONE=$((Get-Command Write-Progress).CommandType -eq 'Cmdlet')"
Write-Progress -Activity 'after removal' -Completed
'OK'
'@
        $text | Should -Match 'GONE=True'
        $text | Should -Match 'OK'
        $text = Invoke-DefaultsChild @'
function global:Write-Progress { param($Activity) 'MINE' }
$owner = (Get-Command Enable-ConsoleFormatting).Module
$before = $owner.OnRemove
Enable-ConsoleFormatting -Feature Progress -WarningVariable w -WarningAction SilentlyContinue
"WARNED=$($w.Count) ALIAS=$([bool] (Get-Alias Write-Progress -ErrorAction Ignore)) CALL=$(Write-Progress -Activity x)"
"SHIM=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore)) HOOK_UNCHANGED=$([object]::ReferenceEquals($before, $owner.OnRemove))"
Enable-ConsoleFormatting -Feature Output, Progress -WarningAction SilentlyContinue
"MIXED_OUTPUT=$((Get-Command Out-Default -CommandType Function).Definition -match 'ConsoleMonster') MIXED_ALIAS=$([bool] (Get-Alias Write-Progress -ErrorAction Ignore))"
Disable-ConsoleFormatting
'@
        $text | Should -Match 'WARNED=1 ALIAS=False CALL=MINE'
        $text | Should -Match 'SHIM=False HOOK_UNCHANGED=True'
        $text | Should -Match 'MIXED_OUTPUT=True MIXED_ALIAS=False'
    }
}
