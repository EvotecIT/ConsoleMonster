Import-Module ConsoleMonster

New-ConsoleGrid -Columns 3 {
    New-ConsolePanel -Title 'DC01' { '[pass]√ healthy[/]' }
    New-ConsolePanel -Title 'DC02' { '[warn]▲ lagging[/]' }
    New-ConsolePanel -Title 'DC03' { '[pass]√ healthy[/]' }
    New-ConsolePanel -Title 'DC04' { '[fail]✗ offline[/]' }
    New-ConsolePanel -Title 'DC05' { '[pass]√ healthy[/]' }
} | Write-ConsoleWidget
