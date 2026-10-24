Import-Module ConsoleMonster

$latency = foreach ($i in 0..59) { 40 + ($i * 37) % 23 + $(if ($i % 7 -eq 0) { 30 } else { 0 }) }
New-ConsoleLineChart -Height 7 -Scatter -Unit 'ms' -Content {
    New-ConsoleChartSeries 'latency' -Value $latency -Role Accent2
} | Show-Console
