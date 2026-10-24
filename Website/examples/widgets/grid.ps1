Import-Module ConsoleMonster

New-ConsoleGrid -Columns 3 -Content {
    New-ConsolePanel -Title 'DC01' -Content { '[pass]√ healthy[/]' }
    New-ConsolePanel -Title 'DC02' -Content { '[warn]▲ lagging[/]' }
    New-ConsolePanel -Title 'DC03' -Content { '[pass]√ healthy[/]' }
    New-ConsolePanel -Title 'DC04' -Content { '[fail]✗ offline[/]' }
    New-ConsolePanel -Title 'DC05' -Content { '[pass]√ healthy[/]' }
} | Show-Console
