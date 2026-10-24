Import-Module ConsoleMonster

New-ConsoleBarChart -Stacked -Content {
    New-ConsoleBarItem 'Passed' 412 -Role Pass
    New-ConsoleBarItem 'Warnings' 51 -Role Warn
    New-ConsoleBarItem 'Failed' 23 -Role Fail
    New-ConsoleBarItem 'Skipped' 14 -Role Skip
} | Show-Console
