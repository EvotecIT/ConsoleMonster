Describe 'ConsoleMonster Markdown, HTML and clipboard output' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '..\ConsoleMonster.psd1') -Force
    }

    It 'exports every Format-Console view as one Markdown string or HTML page' {
        $commands = @(
            @{ Name = 'Format-ConsoleTable'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Format-ConsoleList'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Format-ConsoleTree'; Input = [pscustomobject] @{ Name = 'export-marker'; Count = 12 } }
            @{ Name = 'Format-ConsoleCode'; Input = 'export-marker' }
            @{ Name = 'Format-ConsoleJson'; Input = '{"Name":"export-marker"}' }
            @{ Name = 'Format-ConsoleError'; Input = [InvalidOperationException]::new('export-marker') }
        )
        foreach ($command in $commands) {
            foreach ($format in 'Markdown', 'Html') {
                $result = @($command.Input | & $command.Name -As $format -Width 70)
                $result.Count | Should -Be 1 -Because $command.Name
                $result[0] | Should -BeOfType string
                $result[0] | Should -Match 'export-marker'
                if ($format -eq 'Markdown') {
                    $result[0] | Should -Match '^```text\n'
                    $result[0] | Should -Match '\n```\n$'
                } else {
                    $result[0] | Should -Match '^<!DOCTYPE html>'
                }
                $result[0] | Should -Not -Match ([char] 27)
            }
        }
    }

    It 'rejects incompatible output switches before processing even empty input' {
        foreach ($name in 'Format-ConsoleTable', 'Format-ConsoleList', 'Format-ConsoleTree', 'Format-ConsoleCode', 'Format-ConsoleJson', 'Format-ConsoleError') {
            { & $name -As Markdown -AsString -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
        }
        foreach ($name in 'Format-ConsoleTable', 'Format-ConsoleCode', 'Format-ConsoleJson', 'Format-ConsoleError') {
            { & $name -As Html -AsWidget -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
        }
        foreach ($name in 'Format-ConsoleTable', 'Format-ConsoleList', 'Format-ConsoleTree') {
            { & $name -As Markdown -PassThru -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
            { & $name -AsString -PassThru -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
        }
        { Format-ConsoleTable -AsWidget -AsString -ErrorAction Stop } | Should -Throw '*Choose one output mode*'
    }

    It 'preserves widget, string and input-object contracts' {
        $row = [pscustomobject] @{ Name = 'source-marker'; Count = 12 }
        ($row | Format-ConsoleTable -AsWidget) | Should -BeOfType ConsoleMonster.Widgets.Table
        ($row | Format-ConsoleTable -AsString) | Should -BeOfType string
        $pass = @($row | Format-ConsoleTable -PassThru)
        $pass.Count | Should -Be 1
        [object]::ReferenceEquals($row, $pass[0]) | Should -BeTrue
        ($row | Format-ConsoleTable -AsWidget:$false -As Markdown) | Should -Match 'source-marker'
    }

    It 'infers Markdown paths and safely fences embedded backticks and literal markup' {
        $path = Join-Path $TestDrive 'literal.md'
        '````' | Export-ConsoleFrame -Path $path
        $document = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $document | Should -Be ('`````text' + "`n" + '````' + "`n" + '`````' + "`n")
        '[pass]literal[/] <tag>' | Export-ConsoleFrame -Format Markdown | Should -Match '\[pass\]literal\[/\] <tag>'
    }

    It 'exports mixed frames widgets and objects in pipeline order' {
        $options = [ConsoleMonster.Export.ExportOptions]::new()
        $options.Width = 30
        $frame = [ConsoleMonster.Export.ConsoleExporter]::Render((New-ConsoleText 'frame-marker'), $options, [ConsoleMonster.Styling.Theme]::Dark)
        $document = @(
            $frame
            New-ConsoleText 'widget-marker'
            [pscustomobject] @{ Name = 'object-marker' }
        ) | Export-ConsoleFrame -Format Markdown -Width 70
        $document.IndexOf('frame-marker') | Should -BeLessThan $document.IndexOf('widget-marker')
        $document.IndexOf('widget-marker') | Should -BeLessThan $document.IndexOf('object-marker')
    }

    It 'uses a registered view and returns a document from Out-ConsoleDefault without enabling takeover' {
        $row = [pscustomobject] @{ Name = 'view-marker'; Secret = 'must-stay-hidden' }
        $row.PSObject.TypeNames.Insert(0, 'ConsoleMonster.Tests.ExportRecord')
        try {
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportRecord' -Property Name
            foreach ($format in 'Markdown', 'Html') {
                $document = @($row | Out-ConsoleDefault -As $format -Width 65)
                $document.Count | Should -Be 1
                $document[0] | Should -Match 'view-marker'
                $document[0] | Should -Not -Match 'must-stay-hidden'
            }
        } finally {
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportRecord' -Remove
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
            $document = @($first; 'middle-marker'; $last) | Out-ConsoleDefault -As Markdown
            $document.IndexOf('first-marker') | Should -BeLessThan $document.IndexOf('middle-marker')
            $document.IndexOf('middle-marker') | Should -BeLessThan $document.IndexOf('last-marker')
        } finally {
            Register-ConsoleView -TypeName 'ConsoleMonster.Tests.ExportFirst', 'ConsoleMonster.Tests.ExportLast' -Remove
        }
    }

    It 'returns no document for empty export input' {
        @(@() | Format-ConsoleTable -As Markdown).Count | Should -Be 0
        @(@() | Out-ConsoleDefault -As Markdown).Count | Should -Be 0
    }

    It 'previews clipboard copying for every input kind and format without accessing any provider' {
        foreach ($format in 'Text', 'Markdown', 'Html') {
            foreach ($inputValue in @('literal text', (New-ConsoleText 'widget'), [pscustomobject] @{ Name = 'row' })) {
                @($inputValue | Copy-ConsoleOutput -As $format -WhatIf -PassThru).Count | Should -Be 0
            }
        }
    }

    It 'reports SSH clipboard failure without overwriting any clipboard and respects ErrorAction' {
        $saved = [Environment]::GetEnvironmentVariable('SSH_CONNECTION', 'Process')
        try {
            [Environment]::SetEnvironmentVariable('SSH_CONNECTION', 'synthetic-test-session', 'Process')
            $copyErrors = @()
            $result = @('synthetic clipboard text' | Copy-ConsoleOutput -As Markdown -PassThru -ErrorAction SilentlyContinue -ErrorVariable copyErrors)
            $result.Count | Should -Be 0
            $copyErrors.Count | Should -Be 1
            $copyErrors[0].FullyQualifiedErrorId | Should -Match '^ConsoleClipboardUnavailable'
            $copyErrors[0].ToString() | Should -Match 'SSH'
            { 'synthetic clipboard text' | Copy-ConsoleOutput -As Html -ErrorAction Stop } | Should -Throw '*SSH*'
        } finally {
            [Environment]::SetEnvironmentVariable('SSH_CONNECTION', $saved, 'Process')
        }
    }
}
