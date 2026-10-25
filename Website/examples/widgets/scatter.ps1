Import-Module ConsoleMonster

$latency = foreach ($i in 0..59) { 40 + ($i * 37) % 23 + $(if ($i % 7 -eq 0) { 30 } else { 0 }) }
New-ConsoleChart -Height 7 -Unit 'ms' -Content {
    [pscustomobject]@{ Key = 'latency'; Role = 'Accent2'; Values = $latency }
} -Type Scatter | Show-Console
