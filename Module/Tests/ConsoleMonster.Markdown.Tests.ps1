Describe 'Markdown reader adapter contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'renders inline content and literal markup without interpreting document text as console markup' {
        $text = Show-ConsoleMarkdown -Content '# Notes' -AsString -Width 60
        $text | Should -Match 'Notes'
        $text | Should -Not -Match '# Notes'
        Show-ConsoleMarkdown -Content '`[fail]literal[/]`' -AsString | Should -Match '\[fail\]literal\[/\]'
        Show-ConsoleMarkdown -Content '' -AsString | Should -BeOfType ([string])
    }

    It 'joins source lines before parsing reference links and block structure' {
        $lines = @('[Read the guide][guide]', '', '[guide]: https://example.org/guide')
        $actual = $lines | Show-ConsoleMarkdown -AsString -Width 60
        $actual.Trim() | Should -BeExactly 'Read the guide (https://example.org/guide)'
    }

    It 'resolves literal paths against the PowerShell location and FileSystem drives' {
        $path = Join-Path $TestDrive 'notes [final].md'
        [IO.File]::WriteAllText($path, '# Saved notes')
        Push-Location $TestDrive
        try {
            $expected = Show-ConsoleMarkdown -Content '# Saved notes' -AsString -Width 60
            Show-ConsoleMarkdown -Path './notes [final].md' -AsString -Width 60 | Should -BeExactly $expected
            Show-ConsoleMarkdown -Path 'TestDrive:/notes [final].md' -AsString -Width 60 | Should -BeExactly $expected
            Show-ConsoleMarkdown -LiteralPath ('Microsoft.PowerShell.Core\FileSystem::' + $path) -AsString -Width 60 | Should -BeExactly $expected
        } finally {
            Pop-Location
        }
    }

    It 'rejects non-file providers and reports missing files' {
        { Show-ConsoleMarkdown -Path 'Variable:/PSVersionTable' -AsString -ErrorAction Stop } | Should -Throw '*FileSystem*'
        { Show-ConsoleMarkdown -Path (Join-Path $TestDrive 'missing.md') -AsString -ErrorAction Stop } | Should -Throw
    }

    It 'uses native code highlighting, quotes and table rendering at the requested width' {
        $source = @'
> Keep the rollback plan.

| Host | Count |
| :--- | ---: |
| web-01 | 12 |
| web-02 | 3 |

```powershell
Get-Service | Select-Object Name
```
'@
        $actual = Show-ConsoleMarkdown -Content $source -AsString -Width 60
        $actual | Should -Match 'Keep the rollback plan'
        $actual | Should -Match 'web-01'
        $actual | Should -Match 'powershell'
        $actual | Should -Match 'Get-Service'
        foreach ($line in ($actual -split '\r?\n')) {
            [ConsoleMonster.Text.DisplayWidth]::Measure($line) | Should -BeLessOrEqual 60
        }
    }

    It 'pipes the complete long document as plain lines without reading input' {
        $source = (1..120 | ForEach-Object { 'Paragraph ' + $_ + '.' }) -join "`n`n"
        $actual = Show-ConsoleMarkdown -Content $source -Width 60 | Out-String -Width 200
        $actual | Should -Match 'Paragraph 1\.'
        $actual | Should -Match 'Paragraph 120\.'
        $actual | Should -Not -Match ([char]27)
        $actual | Should -Not -Match 'pgup/pgdn'
    }

    It 'keeps the complete rendering in files and explicit string assignment' {
        $source = '# Assignment' + "`n`n" + '[Guide](https://example.org/guide)'
        $text = Show-ConsoleMarkdown -Content $source -AsString -Width 60
        $path = Join-Path $TestDrive 'rendered.txt'
        Show-ConsoleMarkdown -Content $source -NoPager -Width 60 | Set-Content -LiteralPath $path -Encoding UTF8
        (Get-Content -LiteralPath $path -Raw).Trim() | Should -BeExactly $text.Trim()
        $text | Should -Not -Match ([char]27)
    }

    It 'keeps the parser dependency outside the core assembly reference graph' {
        $core = [ConsoleMonster.Terminal].Assembly.GetReferencedAssemblies().Name
        $core | Should -Not -Contain 'OfficeIMO.Markdown'
        $core | Should -Not -Contain 'OfficeIMO.Core'
        $core | Should -Not -Contain 'ConsoleMonster.Markdown'
        [ConsoleMonster.Markdown.MarkdownConsole].Assembly.GetReferencedAssemblies().Name | Should -Contain 'OfficeIMO.Markdown'
    }

    It 'uses the same reader for explicit about-topic help' {
        Import-Module (Join-Path $PSScriptRoot 'Fixtures/HelpFormatting/HelpFormatting.psd1') -Force
        $actual = Show-ConsoleHelp about_ConsoleMonsterMarkdown -AsString -Width 60
        $actual | Should -Match 'Markdown topic'
        $actual | Should -Not -Match '# Markdown topic'
        $actual | Should -Match '\[literal brackets\]'
        $actual | Should -Match 'web-01'
        $actual | Should -Match 'Confirm the target'
    }
}
