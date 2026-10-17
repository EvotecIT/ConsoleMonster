New-ConsoleColumns {
    New-ConsolePanel -Title 'Align' { New-ConsoleAlign { '[bold]centred[/]' } }
    New-ConsolePanel -Title 'Padding' {
        $text = [ConsoleMonster.Widgets.TextBlock]::new('two cells of padding')
        [ConsoleMonster.Layout.Padding]::new($text, 2, 1, 2, 1)
    }
} | Write-ConsoleWidget
