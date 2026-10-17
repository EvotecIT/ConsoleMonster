Describe 'Show-Console, default formatting and the taskbar indicator' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path

        # A child process with redirected output (plain text) and en-US culture.
        function Invoke-ViewChild {
            param([Parameter(Mandatory)][string] $Script, [hashtable] $Environment = @{})
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_LANGUAGE', 'CONSOLEMONSTER_TASKBAR', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'COLUMNS') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            foreach ($name in $Environment.Keys) {
                $lines.Add("[Environment]::SetEnvironmentVariable('$name', '$($Environment[$name])')")
            }
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            # PowerShell 7 may colour its own table headers (PSStyle); the assertions read the plain text.
            (($output | ForEach-Object { "$_" }) -join "`n") -replace "$([char] 27)\[[0-9;]*m", ''
        }
    }

    It 'picks a view for each shape' {
        $text = Invoke-ViewChild -Script @'
'== one'
[pscustomobject] @{ Name = 'srv01'; Up = $true } | Show-Console -Width 60
'== many'
1..3 | ForEach-Object { [pscustomobject] @{ Name = "srv0$_"; Count = $_ } } | Show-Console -Width 60
'== tree'
[pscustomobject] @{ Name = 'root'; Children = @([pscustomobject] @{ Name = 'leaf'; Children = @() }) } | Show-Console -ChildProperty Children -Width 60
'== numbers'
3, 9 | Show-Console -Width 40
'== cards'
1..2 | ForEach-Object { [pscustomobject] @{ Name = "srv0$_"; Count = $_ } } | Show-Console -View Cards -Width 60
'@
        $text | Should -Match 'PSCustomObject\s+.\s+2 members'
        $text | Should -Match 'Name\s+.\s+Count'
        $text | Should -Match 'root\s*\n\s+.+leaf'
        $text | Should -Match '9'
        ($text -split '== cards')[1] | Should -Match 'Count\s+1'
    }

    It 'leaves redirected output, pipelines and Out-String unchanged and Disable removes the Out-Default function' {
        $text = Invoke-ViewChild -Script @'
$before = Get-Item $PSHOME | Out-String
$pipeBefore = @(Get-ChildItem $PSHOME -File | Select-Object -First 3 | ForEach-Object Name) -join ','
Enable-ConsoleFormatting
"ENABLED=$([bool] (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
$after = Get-Item $PSHOME | Out-String
$pipeAfter = @(Get-ChildItem $PSHOME -File | Select-Object -First 3 | ForEach-Object Name) -join ','
"SAMESTRING=$($before -eq $after)"
"SAMEPIPE=$($pipeBefore -eq $pipeAfter)"
Get-Item $PSHOME
Disable-ConsoleFormatting
"DISABLED=$(-not (Get-Command Out-Default -CommandType Function -ErrorAction Ignore))"
'@
        $text | Should -Match 'ENABLED=True'
        $text | Should -Match 'SAMESTRING=True'
        $text | Should -Match 'SAMEPIPE=True'
        $text | Should -Match 'DISABLED=True'
        # Redirected: Get-Item still prints PowerShell's own view, not a ConsoleMonster table.
        $text | Should -Match 'LastWriteTime\s+Length\s+Name|Mode\s+LastWriteTime'
        $text | Should -Not -Match '[┌╭]'
    }

    It 'keeps an existing Out-Default function unless -Force is used' {
        $text = Invoke-ViewChild -Script @'
function global:Out-Default { process { $_ | Microsoft.PowerShell.Core\Out-Default } }
Enable-ConsoleFormatting -WarningVariable w -WarningAction SilentlyContinue
"KEPT=$((Get-Command Out-Default -CommandType Function).Definition -notmatch 'Out-ConsoleDefault')"
"WARNED=$($w.Count -gt 0)"
'@
        $text | Should -Match 'KEPT=True'
        $text | Should -Match 'WARNED=True'
    }

    It 'renders registered types through Out-ConsoleDefault and passes the rest on' {
        $text = Invoke-ViewChild -Script @'
Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }
$servers = 1..2 | ForEach-Object { $o = [pscustomobject] @{ Name = "srv0$_"; State = 'Running' }; $o.PSObject.TypeNames.Insert(0, 'Contoso.Server'); $o }
@($servers) + @('a plain string') | Out-ConsoleDefault -Force
Register-ConsoleView -TypeName 'Contoso.Server' -Remove
$servers | Out-ConsoleDefault -Force
'@
        $text | Should -Match 'srv01.+Running'
        $text | Should -Match 'a plain string'
        # After -Remove the objects get PowerShell's default output again.
        $text | Should -Match 'Name\s+State\s*\n-+\s+-+'
    }

    It 'never writes the taskbar sequence in redirected output, even in Windows Terminal' {
        $text = Invoke-ViewChild -Environment @{ WT_SESSION = 'test' } -Script @'
Write-ConsoleStatus 'working' -Spinner
Write-ConsoleStatus 'done' -Status Pass -Complete
'@
        $text | Should -Not -Match ([regex]::Escape([char] 27 + ']9;4'))
        $text | Should -Match 'done'
    }
}
