Import-Module ConsoleMonster

# Invented results; nothing is queried.
$layout = New-ConsoleRows -Gap 1 -Content {
    # Side by side: -Weight 2 gives the first panel twice the width of the second.
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Results' -Weight 2 -Content {
            "[pass]√[/] 412 passed`n[warn]▲[/] 51 warnings`n[fail]✗[/] 23 failed"
        }
        New-ConsolePanel -Title 'Owner' -Content { "Platform team`n[muted]ops@example.com[/]" }
    }
    # A grid: cells fill three equal columns, row by row.
    New-ConsoleLayoutGrid -Columns 3 -Content {
        New-ConsolePanel -Title 'DC01' -Content { '[pass]√ healthy[/]' }
        New-ConsolePanel -Title 'DC02' -Content { '[warn]▲ lagging[/]' }
        New-ConsolePanel -Title 'DC03' -Content { '[pass]√ healthy[/]' }
        New-ConsolePanel -Title 'DC04' -Content { '[fail]✗ offline[/]' }
        New-ConsolePanel -Title 'DC05' -Content { '[pass]√ healthy[/]' }
        New-ConsolePanel -Title 'DC06' -Content { '[pass]√ healthy[/]' }
    }
}

$layout | Show-Console
