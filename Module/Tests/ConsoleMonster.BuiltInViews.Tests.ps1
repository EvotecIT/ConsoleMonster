Describe 'Everyday built-in view contracts' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
        . (Join-Path $PSScriptRoot 'Support/Convert-TestNativeView.ps1')
        $script:members = @('synthetic' | Get-Member -Name Length, Substring)
        $script:demoModule = New-Module -Name Demo.Inventory -ScriptBlock {
            function Get-DemoItem { 'synthetic' }
            function Test-DemoItem { $true }
            Set-Alias gdi Get-DemoItem
            Export-ModuleMember -Function Get-DemoItem, Test-DemoItem -Alias gdi
        }
        $script:commands = @($script:demoModule.ExportedCommands.Values)
        $script:modules = @($script:demoModule)
        $script:history = @(
            foreach ($id in 101, 102) {
                $value = [pscustomobject]@{
                    Id = $id
                    StartExecutionTime = [datetime]'2026-10-22T10:00:00'
                    EndExecutionTime = [datetime]'2026-10-22T10:00:02'
                    ExecutionStatus = if ($id -eq 101) { 'Completed' } else { 'Failed' }
                    CommandLine = 'Get-DemoItem -Name "synthetic"'
                }
                $value.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.HistoryInfo')
                $value
            }
        )
    }

    It 'groups members and preserves complete overloaded signatures' {
        $text = $script:members | Show-ConsoleTable -AsString -Width 160
        $text | Should -Match 'Method'
        $text | Should -Match 'Property'
        $text | Should -Match 'string Substring\(int startIndex\), string Substring\(int startIndex, int length\)'
        $text | Should -Match 'int Length \{get;\}'
        $worker = [System.ComponentModel.BackgroundWorker]::new()
        try {
            $worker | Get-Member -Name DoWork | Show-ConsoleTable -AsString -Width 160 | Should -Match 'Event.*1'
        } finally {
            $worker.Dispose()
        }
    }

    It 'keeps member type captions and full-width section rows at both widths' {
        $other = @([datetime]'2026-10-22' | Get-Member -Name Day, AddDays)
        $rows = @($script:members) + $other
        foreach ($width in 60, 120) {
            $layout = $rows | Show-ConsoleTable -AsWidget
            $widgets = @($layout.Children)
            $widgets.Count | Should -Be 2
            $widgets[0].Title | Should -Be 'TypeName: System.String'
            $widgets[1].Title | Should -Be 'TypeName: System.DateTime'
            foreach ($widget in $widgets) {
                $widget.GroupHeadersAsRows | Should -BeTrue
                $text = $widget | Export-ConsoleFrame -Format Text -Width $width
                $groups = @($text -split "`n" | Where-Object { $_ -match '(Method|Property) · [0-9]+' })
                $groups.Count | Should -Be 2
                foreach ($group in $groups) {
                    ([regex]::Matches($group, '│')).Count | Should -Be 2
                    $group | Should -Not -Match '┼|─'
                }
                $text | Should -Not -Match '┼'
            }
            $native = Convert-TestNativeView -Rows $script:members
            $nativeText = $native | Export-ConsoleFrame -Format Text -Width $width
            $nativeText | Should -Match 'TypeName: System.String'
            # Format-Table keeps the selected native view's TypeName group, while Show-ConsoleTable above uses sections.
            $native.GroupHeadersAsRows | Should -BeFalse
            $nativeText | Should -Match 'Substring'
            $nativeText | Should -Match 'Length'
        }
    }

    It 'shows small command results without module groups and keeps literal type chips' {
        $text = $script:commands | Show-ConsoleTable -AsString -Width 120
        $text | Should -Not -Match 'Demo.Inventory ·'
        $text | Should -Match '\[Function\]'
        $text | Should -Match '\[Alias\]'
        $text | Should -Match 'Get-DemoItem'
    }

    It 'shows module versions shortened paths and exported command counts' {
        $value = [psobject]::AsPSObject($script:demoModule)
        $value | Add-Member -NotePropertyName Path -NotePropertyValue 'C:\Demo\Inventory\Demo.Inventory.psm1' -Force
        $widget = $value | Show-ConsoleTable -AsWidget
        $text = $widget | Export-ConsoleFrame -Format Text -Width 160
        $text | Should -Match 'Version'
        $text | Should -Match 'Inventory\\Demo.Inventory.psm1'
        $text | Should -Match 'Commands'
        $text | Should -Match '\s3\s'
        $widget.Columns[4].Overflow | Should -Be 'TruncateMiddle'
    }

    It 'shows history identifiers durations statuses and literal command text' {
        $text = $script:history | Show-ConsoleTable -AsString -Width 120
        $text | Should -Match '101'
        $text | Should -Match '2[.,]0 s'
        $text | Should -Match 'Completed'
        $text | Should -Match 'Failed'
        $text | Should -Match 'Get-DemoItem -Name "synthetic"'
    }

    It 'honours explicit properties and calculated expressions for every built-in family' {
        foreach ($rows in @($script:members), @($script:commands), @($script:modules), @($script:history)) {
            $script:reads = 0
            $text = $rows | Show-ConsoleTable -Property @{ Label = 'Chosen'; Expression = { $script:reads++; 'literal [pass]value[/]' } } -AsString
            $text | Should -Match 'Chosen'
            $text | Should -Match '\[pass\]value\[/\]'
            $script:reads | Should -Be $rows.Count
            $text | Should -Not -Match 'Definition|Commands|Duration|ModuleName'
        }
    }

    It 'keeps native column labels and expressions for Table and List without adding registered columns' {
        foreach ($rows in @($script:members), @($script:commands), @($script:modules), @($script:history)) {
            foreach ($kind in 'Table', 'List') {
                $widget = Convert-TestNativeView -Rows $rows -Kind $kind
                $text = $widget | Export-ConsoleFrame -Format Text -Width 160
                $native = @($rows | & ('Microsoft.PowerShell.Utility\Format-' + $kind))
                if ($kind -eq 'Table') {
                    $shape = $native | Where-Object { $_.GetType().Name -eq 'FormatStartData' } | Select-Object -First 1
                    foreach ($column in $shape.shapeInfo.tableColumnInfoList) {
                        $label = if ($column.label) { $column.label } else { $column.propertyName }
                        $text | Should -Match ([regex]::Escape($label))
                    }
                }
                if ($rows[0] -is [Management.Automation.PSModuleInfo]) { $text | Should -Not -Match '\bVariables\b' }
                $script:reads = 0
                $explicit = Convert-TestNativeView -Rows $rows -Kind $kind -Arguments @{ Property = @(@{ Label = 'OnlyChosen'; Expression = { $script:reads++; 12.5 }; FormatString = 'F1' }) }
                $rendered = $explicit | Export-ConsoleFrame -Format Text -Width 120
                $rendered | Should -Match 'OnlyChosen'
                $rendered | Should -Match '12.5'
                $script:reads | Should -Be $rows.Count
                $rendered | Should -Not -Match '\[Function\]|MemberType|Definition|Duration|Commands'
            }
        }
    }

    It 'lets user registrations override built-ins and restores them after removal' {
        try {
            Register-ConsoleView -TypeName Microsoft.PowerShell.Commands.MemberDefinition -View Table -Property @{ Name = 'OwnerChoice'; Expression = { 'custom' } }
            $text = $script:members | Show-ConsoleTable -AsString
            $text | Should -Match 'OwnerChoice'
            $text | Should -Not -Match 'Definition'
        } finally {
            Unregister-ConsoleView -TypeName Microsoft.PowerShell.Commands.MemberDefinition
        }
        $script:members | Show-ConsoleTable -AsString | Should -Match 'Definition'
    }

    It 'preserves the native empty shape when an explicit wildcard matches no history properties' {
        $records = @($script:history | Microsoft.PowerShell.Utility\Format-Table N*)
        $widget = @($records | Out-ConsoleFormatCommand -AsWidget)
        $widget.Count | Should -Be 0
    }
}
