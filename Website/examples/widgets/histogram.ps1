Import-Module ConsoleMonster

$seconds = 0.1, 0.2, 0.3, 0.3, 0.4, 0.6, 0.7, 0.7, 0.8, 0.9, 1.2, 1.4, 1.5, 1.9, 2.4, 3.1, 5.2
New-ConsoleChart -Height 5 -Unit 's' -Type Histogram -Values $seconds -Edges 0, 0.25, 0.5, 1, 2, 4,
    8 |
    Show-Console
