Import-Module ConsoleMonster

New-ConsoleColumns {
    New-ConsolePanel -Title 'Align' { New-ConsoleAlign { '[bold]centred[/]' } }
    New-ConsolePanel -Title 'Padding' { New-ConsolePadding -Padding 2, 1 { 'two cells of padding' } }
} | Write-ConsoleWidget
