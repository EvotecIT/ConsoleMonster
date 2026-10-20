# The mail queues every 1.5 seconds; -Differences marks the cells that changed since the last run.
$script:tick = 0
Watch-Console -Title 'Mail queues' -Interval 1.5 -Count 6 -Differences {
    $script:tick++
    $servers = 'MX01', 'MX02', 'MX03', 'MX04'
    for ($i = 0; $i -lt $servers.Count; $i++) {
        $load = ($script:tick * 7 + $i * 3) % 11
        [pscustomobject] @{
            Server    = "$($servers[$i]).evotec.xyz"
            Queued    = $load * 4
            Deferred  = if ($i -eq 2) { 12 + $script:tick * 5 } else { $load % 3 }
            Delivered = 1200 + $script:tick * 85 + $load * 9
            Status    = if ($i -eq 2 -and $script:tick -gt 2) { 'Warn' } else { 'Pass' }
        }
    }
}
