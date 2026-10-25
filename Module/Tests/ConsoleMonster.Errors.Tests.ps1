Describe 'Show-ConsoleError' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        Import-Module $script:ModulePath -Force

        function Invoke-ErrorChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            (Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")) -join "`n"
        }
    }

    It 'shows an ErrorRecord with its type, message, category, error id and position' {
        # A path that cannot exist under the temp directory, so every platform reports ItemNotFound (on Unix a C:\ path
        # would be DriveNotFound instead).
        $missing = Join-Path ([System.IO.Path]::GetTempPath()) ('cm-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        $text = Invoke-ErrorChild ("try { Get-Item '" + $missing + "' -ErrorAction Stop } catch { `$_ | Show-ConsoleError -Width 200 }")
        $text | Should -Match 'ItemNotFoundException'
        $text | Should -Match ("Cannot find path '" + [regex]::Escape($missing) + "'")
        # The facts line only adds what the message does not say: the category and the short error id, no target path.
        $text | Should -Match 'ObjectNotFound \S PathNotFound'
        $text | Should -Not -Match 'GetItemCommand'
        $text | Should -Not -Match 'ObjectNotFound \('
        $text | Should -Match 'char:\d+'
    }

    It 'shows a script stack and a .NET exception chain' {
        $text = Invoke-ErrorChild @'
function Get-Inventory { [int]::Parse('abc') }
try { Get-Inventory } catch { Show-ConsoleError $_ -Width 120 }
$inner = [System.InvalidOperationException]::new('Loading failed.', [System.FormatException]::new('Not a number.'))
Show-ConsoleError $inner -Width 120
'@
        $text | Should -Match 'at Get-Inventory'
        $text | Should -Match 'InvalidOperationException'
        $text | Should -Match 'Caused by FormatException'
        $text | Should -Match 'Not a number\.'
    }

    It 'returns the views with -AsWidget' {
        try { [int]::Parse('x') } catch { $views = @($_ | Show-ConsoleError -AsWidget) }
        $views.Count | Should -Be 1
        $views[0].GetType().Name | Should -Be 'ExceptionView'
    }
}
