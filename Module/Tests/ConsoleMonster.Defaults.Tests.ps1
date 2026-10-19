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
Enable-ConsoleFormatting -Feature Progress -WarningVariable w -WarningAction SilentlyContinue
"WARNED=$($w.Count) ALIAS=$([bool] (Get-Alias Write-Progress -ErrorAction Ignore)) CALL=$(Write-Progress -Activity x)"
'@
        $text | Should -Match 'WARNED=1 ALIAS=False CALL=MINE'
    }
}
