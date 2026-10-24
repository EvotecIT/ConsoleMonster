Import-Module ConsoleMonster

$processes = @(
    [pscustomobject]@{ Name = 'web'; CPU = 34 }
    [pscustomobject]@{ Name = 'jobs'; CPU = 72 }
    [pscustomobject]@{ Name = 'api'; CPU = 18 }
    [pscustomobject]@{ Name = 'helper'; CPU = 5 }
)
$latency = 12, 18, 25, 33, 34, 38, 55, 68, 76, 84, 95, 96
$west = 12, 18, 16, 22, 19, 25, 24, 28
$east = 8, 9, 12, 10, 15, 14, 13, 18
$samples = for ($i = 0; $i -lt $west.Count; $i++) {
    [pscustomobject]@{ Host = 'api-west'; Latency = $west[$i] }
    [pscustomobject]@{ Host = 'api-east'; Latency = $east[$i] }
}

New-ConsoleRows -Content {
    New-ConsoleText -Markup '[bold]CPU time - highest first[/]'
    $processes | Show-ConsoleChart -Property CPU -Sort Descending -Top 3 -Unit ' s' -AsWidget
    New-ConsoleText ''
    New-ConsoleText -Markup '[bold]Response-time distribution[/]'
    $latency | Show-ConsoleChart -Type Histogram -Bins 6 -Unit ' ms' -AsWidget
    New-ConsoleText ''
    New-ConsoleText -Markup '[bold]Latency over time (ms) - one line per host[/]'
    $samples | Show-ConsoleChart -Property Latency -Label Host -Type Sparkline -AsWidget
} | Show-Console
