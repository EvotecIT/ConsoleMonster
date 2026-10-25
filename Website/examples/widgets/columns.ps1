Import-Module ConsoleMonster

New-ConsoleRows -Content {
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Results' -Content { "[pass]√[/] 412 passed`n[warn]▲[/] 51 warnings" }
        New-ConsolePanel -Title 'Problems' -Content { "[fail]✗[/] 23 failed`n[muted]-[/] 14 skipped" }
    }
    New-ConsoleRule '[muted]2 domains, 10 controllers[/]'
} | Show-Console
