Import-Module ConsoleMonster

$completed = foreach ($i in 0..47) { 20 + 9 * [math]::Sin($i / 7) + $i % 3 }
$failed = foreach ($i in 0..47) { 3 + 2 * [math]::Cos($i / 5) }
New-ConsoleLineChart -Height 7 -Content {
    New-ConsoleChartSeries 'completed' -Value $completed -Role Accent
    New-ConsoleChartSeries 'failed' -Value $failed -Role Fail
} | Show-Console
