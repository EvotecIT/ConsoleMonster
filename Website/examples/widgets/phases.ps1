Import-Module ConsoleMonster

New-ConsolePhases -Content {
    New-ConsoleStatusItem 'Discover' -Status Pass -Detail '2.1s'
    New-ConsoleStatusItem 'Collect' -Status Pass -Detail '14s'
    New-ConsoleStatusItem 'Evaluate' -Status Running -Detail '64%'
    New-ConsoleStatusItem 'Report'
} | Show-Console
