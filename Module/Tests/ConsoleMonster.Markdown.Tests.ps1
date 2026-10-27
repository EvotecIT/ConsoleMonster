Describe 'Markdown reader adapter contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'loads the bundled Markdown exporter only when a Markdown operation is requested' {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script = @'
$ErrorActionPreference = 'Stop'
Import-Module '__MODULE__' -Force
function Assert-MarkdownNotLoaded {
    if (@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -in 'ConsoleMonster.Markdown', 'OfficeIMO.Markdown' }).Count) { throw 'A core operation eagerly loaded Markdown.' }
}
Assert-MarkdownNotLoaded
$rows = @([pscustomobject]@{ Host = 'first'; Count = 2 }, [pscustomobject]@{ Host = 'second'; Count = 3 })
$null = $rows | Export-ConsoleFrame -Format Text
Assert-MarkdownNotLoaded
$markdown = $rows | Export-ConsoleFrame -Format Markdown
if ($markdown -notmatch '\| first \| 2 \|' -or $markdown -notmatch '\| second \| 3 \|') { throw 'The lazy exporter lost ordinary object rows.' }
if (@([AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq 'ConsoleMonster.Markdown' }).Count -ne 1) { throw 'The bundled add-on was not loaded exactly once.' }
$fenced = $rows | Export-ConsoleFrame -Format FencedText
if ($fenced -notmatch '^```text') { throw 'FencedText did not use the add-on.' }
'LAZY-MARKDOWN-OK'
'@
        $output = Invoke-TestChild -Runtime ([Diagnostics.Process]::GetCurrentProcess().Path) -Script ($script.Replace('__MODULE__', $module.Replace("'", "''")))
        $LASTEXITCODE | Should -Be 0 -Because ($output -join "`n")
        ($output -join "`n") | Should -Match LAZY-MARKDOWN-OK
    }

    It 'renders inline content and literal markup without interpreting document text as console markup' {
        $text = Show-Console -Content '# Notes' -AsString -Width 60 -View Markdown
        $text | Should -Match 'Notes'
        $text | Should -Not -Match '# Notes'
        Show-Console -Content '`[fail]literal[/]`' -AsString -View Markdown | Should -Match '\[fail\]literal\[/\]'
        Show-Console -Content '' -AsString -View Markdown | Should -BeOfType ([string])
    }

    It 'joins source lines before parsing reference links and block structure' {
        $lines = @('[Read the guide][guide]', '', '[guide]: https://example.org/guide')
        $actual = $lines | Show-Console -AsString -Width 60 -View Markdown
        $actual.Trim() | Should -BeExactly 'Read the guide (https://example.org/guide)'
    }

    It 'resolves literal paths against the PowerShell location and FileSystem drives' {
        $path = Join-Path $TestDrive 'notes [final].md'
        [IO.File]::WriteAllText($path, '# Saved notes')
        Push-Location $TestDrive
        try {
            $expected = Show-Console -Content '# Saved notes' -AsString -Width 60 -View Markdown
            Show-Console -Path './notes [final].md' -AsString -Width 60 -View Markdown | Should -BeExactly $expected
            Show-Console -Path 'TestDrive:/notes [final].md' -AsString -Width 60 -View Markdown | Should -BeExactly $expected
            Show-Console -Path ('Microsoft.PowerShell.Core\FileSystem::' + $path) -AsString -Width 60 -View Markdown | Should -BeExactly $expected
        } finally {
            Pop-Location
        }
    }

    It 'rejects non-file providers and reports missing files' {
        { Show-Console -Path 'Variable:/PSVersionTable' -AsString -ErrorAction Stop -View Markdown } | Should -Throw '*FileSystem*'
        { Show-Console -Path (Join-Path $TestDrive 'missing.md') -AsString -ErrorAction Stop -View Markdown } | Should -Throw
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
        $actual = Show-Console -Content $source -AsString -Width 60 -View Markdown
        $actual | Should -Match 'Keep the rollback plan'
        $actual | Should -Match 'web-01'
        $actual | Should -Match 'powershell'
        $actual | Should -Match 'Get-Service'
        foreach ($line in ($actual -split '\r?\n')) {
            ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.DisplayWidth'))::Measure($line) | Should -BeLessOrEqual 60
        }
    }

    It 'pipes the complete long document as plain lines without reading input' {
        $source = (1..120 | ForEach-Object { 'Paragraph ' + $_ + '.' }) -join "`n`n"
        $actual = Show-Console -Content $source -Width 60 -View Markdown | Out-String -Width 200
        $actual | Should -Match 'Paragraph 1\.'
        $actual | Should -Match 'Paragraph 120\.'
        $actual | Should -Not -Match ([char]27)
        $actual | Should -Not -Match 'pgup/pgdn'
    }

    It 'keeps the complete rendering in files and explicit string assignment' {
        $source = '# Assignment' + "`n`n" + '[Guide](https://example.org/guide)'
        $text = Show-Console -Content $source -AsString -Width 60 -View Markdown
        $path = Join-Path $TestDrive 'rendered.txt'
        Show-Console -Content $source -NoPager -Width 60 -View Markdown | Set-Content -LiteralPath $path -Encoding UTF8
        (Get-Content -LiteralPath $path -Raw).Replace("`r", '').Trim() | Should -BeExactly $text.Replace("`r", '').Trim()
        $text | Should -Not -Match ([char]27)
    }

    It 'keeps the parser dependency outside the core assembly reference graph' {
        $core = [ConsoleMonster.Terminal].Assembly.GetReferencedAssemblies().Name
        $core | Should -Not -Contain 'OfficeIMO.Markdown'
        $core | Should -Not -Contain 'OfficeIMO.Core'
        $core | Should -Not -Contain 'ConsoleMonster.Markdown'
        ([AppDomain]::CurrentDomain.GetAssemblies().Where({ $_.GetName().Name -eq 'ConsoleMonster.Markdown' })[0].GetType('ConsoleMonster.Markdown.MarkdownConsole')).Assembly.GetReferencedAssemblies().Name | Should -Contain 'OfficeIMO.Markdown'
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
