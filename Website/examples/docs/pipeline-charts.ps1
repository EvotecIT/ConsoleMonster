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
    $processes | Sort-Object CPU -Descending | Select-Object -First 3 |
        New-ConsoleChart -Property CPU -Unit ' s'
    New-ConsoleText ''
    New-ConsoleText -Markup '[bold]Response-time distribution[/]'
    $latency | New-ConsoleChart -Type Histogram -Edges 12, 26, 40, 54, 68, 82, 96 -Unit ' ms'
    New-ConsoleText ''
    New-ConsoleText -Markup '[bold]Latency over time (ms) - one line per host[/]'
    foreach ($group in $samples | Group-Object Host | Sort-Object Name -Descending) {
        New-ConsoleChart -Type Sparkline -Title $group.Name -Values $group.Group.Latency
    }
} | Show-Console
