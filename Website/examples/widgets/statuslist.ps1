Import-Module ConsoleMonster

New-ConsoleStatusList -Content {
    New-ConsoleStatusItem 'Restore NuGet packages' -Status Pass -Detail '1.6 s'
    New-ConsoleStatusItem 'Compile ConsoleMonster' -Status Pass -Detail '4.7 s'
    New-ConsoleStatusItem 'Run Pester on PowerShell 7' -Status Warn -Detail '1 skipped'
    New-ConsoleStatusItem 'Sign module files' -Status Skip -Detail 'skipped'
    New-ConsoleStatusItem 'Pack ConsoleMonster.2.0.0.nupkg' -Status Running
    New-ConsoleStatusItem 'Publish to PowerShell Gallery' -Status Pending
} | Show-Console
