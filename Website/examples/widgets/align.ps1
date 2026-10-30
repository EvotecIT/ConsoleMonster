Import-Module ConsoleMonster

New-ConsoleColumns -Content {
    New-ConsolePanel -Title 'Align' -Content {
        New-ConsoleRegion -Horizontal Center -Content { '[bold]centred[/]' }
    }
    New-ConsolePanel -Title 'Padding' -Content {
        New-ConsoleRegion -Padding 2, 1 -Content { 'two cells of padding' }
    }
} | Show-Console
