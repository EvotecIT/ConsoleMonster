Import-Module ConsoleMonster

New-ConsoleColumns -Content {
    New-ConsolePanel -Title 'Align' -Content { New-ConsoleAlign -Content { '[bold]centred[/]' } }
    New-ConsolePanel -Title 'Padding' -Content { New-ConsolePadding -Padding 2, 1 -Content { 'two cells of padding' } }
} | Show-Console
