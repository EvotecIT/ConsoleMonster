Describe 'ConsoleMonster Markdown, HTML and clipboard output' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
    }

    It 'exports every display view as one Markdown string or HTML page' {
        $commands = @(
            @{ Name = 'Show-ConsoleTable'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Show-ConsoleList'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Show-Console'; View = 'Tree'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Show-Console'; View = 'Code'; Input = 'export-marker' }
            @{ Name = 'Show-Console'; View = 'Json'; Input = '{"Name":"export-marker"}' }
            @{ Name = 'Show-Console'; View = 'Error'; Input = [InvalidOperationException]::new('export-marker') }
        )
        foreach ($command in $commands) {
            foreach ($format in 'FencedText', 'Markdown', 'Html') {
                $viewArgs = @{}
                if ($command.ContainsKey('View')) { $viewArgs.View = $command.View }
                $result = @($command.Input | & $command.Name @viewArgs -AsWidget | Export-ConsoleFrame -Format $format -Width 70)
                $result.Count | Should -Be 1 -Because $command.Name
                $result[0] | Should -BeOfType string
                $result[0] | Should -Match 'export-marker'
                if ($format -eq 'FencedText') {
                    $result[0] | Should -Match '^```text\n'
                    $result[0] | Should -Match '\n```\n$'
                } elseif ($format -eq 'Html') {
                    $result[0] | Should -Match '^<!DOCTYPE html>'
                } elseif ($command.Name -in @('Show-ConsoleTable','Show-ConsoleList')) {
                    $result[0] | Should -Match '(?m)^\|.*\|$'
                    $result[0] | Should -Match '(?m)^\| :---'
                } elseif ($command.View -eq 'Tree') {
                    $result[0] | Should -Match '(?m)^\s*- '
                }
                $result[0] | Should -Not -Match ([char] 27)
            }
        }
    }

    It 'rejects incompatible output switches before processing even empty input' {
        foreach ($view in 'Auto', 'Table', 'List', 'Tree', 'Code', 'Json', 'Error', 'Markdown', 'Rule') {
            { Show-Console -View $view -AsWidget -AsString -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
            { Show-Console -View $view -AsWidget -PassThru -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
            { Show-Console -View $view -AsString -PassThru -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
        }
        foreach ($name in 'Show-ConsoleTable', 'Show-ConsoleList') {
            { & $name -AsWidget -AsString -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
            { & $name -AsWidget -PassThru -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
        }
    }

    It 'preserves widget, string and input-object contracts' {
        $row = [pscustomobject] @{ Name = 'source-marker'; Count = 12 }
        ($row | Show-ConsoleTable -AsWidget) | Should -BeOfType ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Widgets.Table'))
        ($row | Show-ConsoleTable -AsString) | Should -BeOfType string
        $pass = @($row | Show-ConsoleTable -PassThru)
        $pass.Count | Should -Be 1
        [object]::ReferenceEquals($row, $pass[0]) | Should -BeTrue
        ($row | Show-ConsoleTable -AsString:$false -AsWidget | Export-ConsoleFrame -Format Markdown) | Should -Match 'source-marker'
    }

    It 'infers semantic Markdown paths and escapes literal markup and embedded backticks' {
        $path = Join-Path $TestDrive 'literal.md'
        '````' | Export-ConsoleFrame -Path $path
        $document = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $document | Should -Be ('\`\`\`\`' + "`n`n")
        '[pass]literal[/] <tag>' | Export-ConsoleFrame -Format Markdown | Should -Match ([regex]::Escape('\[pass\]literal\[/\] &lt;tag&gt;'))
    }

    It 'exports mixed frames widgets and objects in pipeline order' {
        $options = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Export.ExportOptions'))::new()
        $options.Width = 30
        $frame = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Export.ConsoleExporter'))::Render((New-ConsoleText 'frame-marker'), $options, [ConsoleMonster.Styling.Theme]::Dark)
        $document = @(
            $frame
            New-ConsoleText 'widget-marker'
            [pscustomobject] @{ Name = 'object-marker' }
        ) | Export-ConsoleFrame -Format Markdown -Width 70
        $document.IndexOf('frame-marker') | Should -BeLessThan $document.IndexOf('widget-marker')
        $document.IndexOf('widget-marker') | Should -BeLessThan $document.IndexOf('object-marker')
    }

    It 'exports registered views through the canonical frame exporter without enabling takeover' {
        $row = [pscustomobject] @{ Name = 'view-marker'; Secret = 'must-stay-hidden' }
        $row.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Tests.ExportRecord')
        try {
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportRecord' -Property Name
            foreach ($format in 'Markdown', 'Html') {
                $document = @($row | Export-ConsoleFrame -Format $format -Width 65)
                $document.Count | Should -Be 1
                $document[0] | Should -Match 'view-marker'
                $document[0] | Should -Not -Match 'must-stay-hidden'
            }
        } finally {
            Unregister-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportRecord'
        }
    }

    It 'keeps fallback objects and different registered views in their original order' {
        $first = [pscustomobject] @{ Name = 'first-marker' }
        $last = [pscustomobject] @{ Name = 'last-marker' }
        $first.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Tests.ExportFirst')
        $last.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Tests.ExportLast')
        try {
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportFirst' -Property Name
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportLast' -Property Name
            $document = @($first; 'middle-marker'; $last) | Export-ConsoleFrame -Format Markdown
            $document.IndexOf('first-marker') | Should -BeLessThan $document.IndexOf('middle-marker')
            $document.IndexOf('middle-marker') | Should -BeLessThan $document.IndexOf('last-marker')
        } finally {
            Unregister-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportFirst', 'ConsoleMonster.Tests.ExportLast'
        }
    }

    It 'returns no document for empty export input' {
        @(@() | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Markdown).Count | Should -Be 0
        @(@() | Export-ConsoleFrame -Format Markdown).Count | Should -Be 0
    }

    It 'previews clipboard copying for every input kind and format without accessing any provider' {
        foreach ($format in 'Text', 'Markdown', 'Html') {
            foreach ($inputValue in @('literal text', (New-ConsoleText 'widget'), [pscustomobject] @{ Name = 'row' })) {
                @($inputValue | Copy-ConsoleOutput -Format $format -WhatIf -PassThru).Count | Should -Be 0
            }
        }
    }

    It 'reports SSH clipboard failure without overwriting any clipboard and respects ErrorAction' {
        $saved = [Environment]::GetEnvironmentVariable('SSH_CONNECTION', 'Process')
        try {
            [Environment]::SetEnvironmentVariable('SSH_CONNECTION', 'synthetic-test-session', 'Process')
            $copyErrors = @()
            $result = @('synthetic clipboard text' | Copy-ConsoleOutput -Format Markdown -PassThru -ErrorAction SilentlyContinue -ErrorVariable copyErrors)
            $result.Count | Should -Be 0
            $copyErrors.Count | Should -Be 1
            $copyErrors[0].FullyQualifiedErrorId | Should -Match '^ConsoleClipboardUnavailable'
            $copyErrors[0].ToString() | Should -Match 'SSH'
            { 'synthetic clipboard text' | Copy-ConsoleOutput -Format Html -ErrorAction Stop } | Should -Throw '*SSH*'
        } finally {
            [Environment]::SetEnvironmentVariable('SSH_CONNECTION', $saved, 'Process')
        }
    }
}
