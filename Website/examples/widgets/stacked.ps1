Import-Module ConsoleMonster

New-ConsoleBarChart -Stacked {
    New-ConsoleBarItem 'Passed' 412 -Tone Pass
    New-ConsoleBarItem 'Warnings' 51 -Tone Warn
    New-ConsoleBarItem 'Failed' 23 -Tone Fail
    New-ConsoleBarItem 'Skipped' 14 -Tone Skip
} | Write-ConsoleWidget
