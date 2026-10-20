Describe 'Enable-ConsoleFormatting features (ErrorView, Progress) and an exact Disable' {
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

    It 'restores only the current runspace commands when formatting is disabled' {
        $text = Invoke-DefaultsChild @'
$first = [powershell]::Create()
$second = [powershell]::Create()
try {
    $setup = {
        param($modulePath, $label, $alias)
        Import-Module $modulePath
        Set-Item function:global:Out-Default ([scriptblock]::Create("'$label'"))
        Set-Alias Write-Progress $alias -Scope Global
        Enable-ConsoleFormatting -Feature All -Force
    }
    $null = $first.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('FIRST').AddArgument('Write-Output').Invoke()
    $null = $second.AddScript($setup.ToString()).AddArgument((Get-Module ConsoleMonster).Path).AddArgument('SECOND').AddArgument('Write-Host').Invoke()
    $first.Commands.Clear()
    $first.AddScript('Disable-ConsoleFormatting -Feature Progress; "FIRST_ALIAS=$((Get-Alias Write-Progress).Definition)"').Invoke()
    $second.Commands.Clear()
    $second.AddScript('"SECOND_STILL_ENABLED=$((Get-Command Out-Default).Definition -match ''ConsoleMonster'') SECOND_ALIAS=$((Get-Alias Write-Progress).Definition)"; Disable-ConsoleFormatting; "SECOND_FUNCTION=$((Get-Command Out-Default).Definition) SECOND_RESTORED_ALIAS=$((Get-Alias Write-Progress).Definition)"').Invoke()
    $first.Commands.Clear()
    $first.AddScript('Disable-ConsoleFormatting -Feature ErrorView; "FIRST_OUTPUT_STILL_ENABLED=$((Get-Command Out-Default).Definition -match ''ConsoleMonster'')"; Disable-ConsoleFormatting -Feature Output; "FIRST_FUNCTION=$((Get-Command Out-Default).Definition)"').Invoke()
} finally {
    $first.Dispose()
    $second.Dispose()
}
'@
        $text | Should -Match 'FIRST_ALIAS=Write-Output'
        $text | Should -Match 'FIRST_OUTPUT_STILL_ENABLED=True'
        $text | Should -Match "FIRST_FUNCTION='FIRST'"
        $text | Should -Match 'SECOND_STILL_ENABLED=True SECOND_ALIAS=Write-ConsoleProgress'
        $text | Should -Match "SECOND_FUNCTION='SECOND' SECOND_RESTORED_ALIAS=Write-Host"
    }

    It 'restores the replaced function and prior removal hook for <feature>' -ForEach @(
        @{ feature = 'Output' }
        @{ feature = 'ErrorView' }
        @{ feature = 'All' }
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

    It 'turns Write-Progress into an alias of Write-ConsoleProgress and removes it again' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
"ALIAS=$((Get-Alias Write-Progress).Definition)"
foreach ($i in 1..3) { Write-Progress -Activity 'Copying' -PercentComplete ($i * 30) }
Write-Progress -Activity 'Copying' -Completed
"SHIM=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
Disable-ConsoleFormatting
"GONE=$(-not (Get-Alias Write-Progress -ErrorAction Ignore)) NOSHIM=$(-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $text | Should -Match 'ALIAS=Write-ConsoleProgress'
        $text | Should -Match 'SHIM=True'
        $text | Should -Match 'GONE=True NOSHIM=True'
    }

    It 'keeps a user''s Write-Progress alias and Out-Default function, and with -Force puts them back exactly on Disable' {
        $text = Invoke-DefaultsChild @'
function global:Write-MyProgress { param($Activity) }
Set-Alias -Name Write-Progress -Value Write-MyProgress -Scope Global
function global:Out-Default { process { $_ | Microsoft.PowerShell.Core\Out-Default } }
$definition = (Get-Command Out-Default -CommandType Function).Definition
Enable-ConsoleFormatting -Feature Progress -WarningVariable w -WarningAction SilentlyContinue
"KEPT=$((Get-Alias Write-Progress).Definition) WARNED=$($w.Count)"
Enable-ConsoleFormatting -Feature Output, Progress -Force
"REPLACED=$((Get-Alias Write-Progress).Definition) SHIM=$((Get-Command Out-Default -CommandType Function).Definition -match 'Out-ConsoleDefault')"
Disable-ConsoleFormatting
"RESTORED=$((Get-Alias Write-Progress).Definition) SAME=$((Get-Command Out-Default -CommandType Function).Definition -eq $definition)"
'@
        $text | Should -Match 'KEPT=Write-MyProgress WARNED=1'
        $text | Should -Match 'REPLACED=Write-ConsoleProgress SHIM=True'
        $text | Should -Match 'RESTORED=Write-MyProgress SAME=True'
    }

    It 'shows errors with the ConsoleMonster error view, keeps CategoryView, and leaves other output alone without the Output feature' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature ErrorView
'== ours'
Write-Error 'disk is full' 2>&1 | Out-ConsoleDefault -Force -Shim
'== category'
$ErrorView = 'CategoryView'
Write-Error 'disk is full' 2>&1 | Out-ConsoleDefault -Force -Shim
$ErrorView = $null
'== items'
Get-Item $PSHOME | Out-ConsoleDefault -Force -Shim
'== off'
Disable-ConsoleFormatting -Feature ErrorView
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
Enable-ConsoleFormatting -Feature ErrorView, Progress -ProfileSnippet
Enable-ConsoleFormatting -Feature All -ProfileSnippet
'@
        $text | Should -Match '(?m)^Enable-ConsoleFormatting -Feature ErrorView, Progress$'
        $text | Should -Match '(?m)^Enable-ConsoleFormatting -Feature All$'
    }

    It 'honours $ProgressPreference and writes PowerShell''s progress where the area cannot be drawn' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
$ProgressPreference = 'SilentlyContinue'
Write-Progress -Activity 'quiet'
$ProgressPreference = 'Continue'
Write-ConsoleProgress -Activity 'loud' -Status 'half' -PercentComplete 50
'OK'
'@
        $text | Should -Match 'OK'
        $text | Should -Not -Match 'Exception'
    }

    It 'removes the Write-Progress alias with the module and keeps a user''s Write-Progress function unless -Force is used' {
        $text = Invoke-DefaultsChild @'
Enable-ConsoleFormatting -Feature Progress
Remove-Module ConsoleMonster
"GONE=$(-not (Get-Alias Write-Progress -ErrorAction Ignore))"
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
