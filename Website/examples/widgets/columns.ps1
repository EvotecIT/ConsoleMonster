Import-Module ConsoleMonster

New-ConsoleRows {
    New-ConsoleColumns {
        New-ConsolePanel -Title 'Results' { "[pass]√[/] 412 passed`n[warn]▲[/] 51 warnings" }
        New-ConsolePanel -Title 'Problems' { "[fail]✗[/] 23 failed`n[muted]-[/] 14 skipped" }
    }
    New-ConsoleRule '[muted]2 domains, 10 controllers[/]'
} | Write-ConsoleWidget
