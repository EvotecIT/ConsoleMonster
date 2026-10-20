Describe 'Format-ConsoleCode, Format-ConsoleJson and smart highlighting' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        Import-Module $script:ModulePath -Force

        function Invoke-HighlightChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                "Import-Module '$($script:ModulePath)' -Force"
                $Script
            )
            (Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")) -join "`n"
        }
    }

    It 'shows a file with its name, line numbers and a marked range' {
        $file = Join-Path $TestDrive 'deploy.ps1'
        Set-Content -LiteralPath $file -Value "param(`$Path)`nGet-Item `$Path`nWrite-Host 'done'" -Encoding UTF8
        $text = Invoke-HighlightChild "Format-ConsoleCode -Path '$file' -LineNumbers -Highlight '2-3' -Width 60"
        $text | Should -Match 'deploy\.ps1'
        $text | Should -Match 'param\(\$Path\)'
        $text | Should -Match 'Get-Item \$Path'
    }

    It 'returns a view with -AsWidget, detects the language and rejects unknown languages' {
        $view = '{ "a": 1 }' | Format-ConsoleCode -AsWidget
        $view.GetType().Name | Should -Be 'CodeView'
        $view.Highlighter.Name | Should -Be 'json'
        { 'x' | Format-ConsoleCode -Language Klingon } | Should -Throw '*Unknown language*'
    }

    It 'converts objects to JSON with Format-ConsoleJson' {
        $view = [pscustomobject] @{ Name = 'alpha'; Count = 7; Tags = @('a', 'b') } | Format-ConsoleJson -AsWidget
        $view.Text | Should -Match '"Name":\s+"alpha"'
        $view.Highlighter.Name | Should -Be 'json'
        $text = Invoke-HighlightChild "'{""ok"": true}' | Format-ConsoleJson"
        $text | Should -Match '"ok": true'
    }

    It 'writes JSON through the shared value rules: re-indented strings, mixed arrays, any key, cycles, no warnings' {
        $text = Invoke-HighlightChild @'
'== string'; '{"a":1,"b":[1,2]}' | Format-ConsoleJson -AsString
'== plain'; 'hello' | Format-ConsoleJson -AsString
'== mixed'; Format-ConsoleJson -InputObject @(1, 'two', @{ k = 3 }, $null) -AsString
'== keys'; @{ 1 = 'one'; [guid]::Empty = 2 } | Format-ConsoleJson -AsString
'== cycle'; $a = [pscustomobject] @{ Name = 'a' }; $a | Add-Member -NotePropertyName Self -NotePropertyValue $a; $a | Format-ConsoleJson -AsString 3>&1
'@
        $parts = $text -split '== \w+'
        $parts[1] | Should -Match '"b": \[\s*1,\s*2\s*\]'
        $parts[2].Trim() | Should -Be '"hello"'
        $parts[3] | Should -Match '\[\s*1,\s*"two",\s*\{\s*"k": 3\s*\},\s*null\s*\]'
        $parts[4] | Should -Match '"1": "one"'
        $parts[4] | Should -Match '"00000000-0000-0000-0000-000000000000": 2'
        $parts[5] | Should -Match '"Self": "\(cycle\)"'
        $parts[5] | Should -Not -Match 'WARNING|truncated'
    }

    It 'highlights table columns and log markup with Smart' {
        $text = Invoke-HighlightChild @'
[pscustomobject] @{ Host = 'srv1'; Address = '10.0.0.12'; Seen = '2026-10-17 14:03' } | Format-ConsoleTable -Property Host, @{ Name = 'Address'; Highlight = 'Smart' }, @{ Name = 'Seen'; Highlight = 'Smart' }
try { [pscustomobject] @{ A = 1 } | Format-ConsoleTable -Property @{ Name = 'A'; Highlight = 'Nope' } } catch { "ERR=$($_.Exception.Message)" }
'@
        $text | Should -Match '10\.0\.0\.12'
        $text | Should -Match 'ERR=.*Unknown highlight language'
        $markup = [ConsoleMonster.Highlighting.HighlightStyles]::ToMarkup('took 412ms from 10.0.0.12 [x]', [ConsoleMonster.Highlighting.Highlighters]::Smart)
        $markup | Should -Be 'took [series3]412ms[/] from [series6]10.0.0.12[/] [[x]]'
    }
}
