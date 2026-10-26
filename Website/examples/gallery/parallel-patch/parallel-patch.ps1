# Patch a forty-host wave with bounded concurrency, timeouts and retained failures.
Import-Module -Name ConsoleMonster

# Data: all hosts, outcomes and delays are synthetic. No remote command runs.
$servers = @(
    [pscustomobject] @{ Name = 'web01'; DelayMilliseconds = 637; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web02'; DelayMilliseconds = 774; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web03'; DelayMilliseconds = 911; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web04'; DelayMilliseconds = 1048; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web05'; DelayMilliseconds = 1185; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web06'; DelayMilliseconds = 1322; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web07'; DelayMilliseconds = 559; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web08'; DelayMilliseconds = 696; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web09'; DelayMilliseconds = 833; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web10'; DelayMilliseconds = 970; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web11'; DelayMilliseconds = 1107; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web12'; DelayMilliseconds = 1244; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web13'; DelayMilliseconds = 1381; RestartFailure = $true; TimesOut = $false }
    [pscustomobject] @{ Name = 'web14'; DelayMilliseconds = 618; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web15'; DelayMilliseconds = 755; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web16'; DelayMilliseconds = 892; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web17'; DelayMilliseconds = 1029; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web18'; DelayMilliseconds = 1166; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web19'; DelayMilliseconds = 1303; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web20'; DelayMilliseconds = 540; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web21'; DelayMilliseconds = 677; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web22'; DelayMilliseconds = 814; RestartFailure = $false; TimesOut = $true }
    [pscustomobject] @{ Name = 'web23'; DelayMilliseconds = 951; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web24'; DelayMilliseconds = 1088; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web25'; DelayMilliseconds = 1225; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web26'; DelayMilliseconds = 1362; RestartFailure = $true; TimesOut = $false }
    [pscustomobject] @{ Name = 'web27'; DelayMilliseconds = 599; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web28'; DelayMilliseconds = 736; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web29'; DelayMilliseconds = 873; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web30'; DelayMilliseconds = 1010; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web31'; DelayMilliseconds = 1147; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web32'; DelayMilliseconds = 1284; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web33'; DelayMilliseconds = 521; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web34'; DelayMilliseconds = 658; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web35'; DelayMilliseconds = 795; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web36'; DelayMilliseconds = 932; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web37'; DelayMilliseconds = 1069; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web38'; DelayMilliseconds = 1206; RestartFailure = $false; TimesOut = $false }
    [pscustomobject] @{ Name = 'web39'; DelayMilliseconds = 1343; RestartFailure = $true; TimesOut = $false }
    [pscustomobject] @{ Name = 'web40'; DelayMilliseconds = 580; RestartFailure = $false; TimesOut = $false }
)
$throttle = 8
$timeoutSeconds = 4

# Layout: a wave summary, every host, eight lane details and retained failures.
function New-PatchSummary {
    param(
        [object[]] $Outcomes
    )

    $byHost = @{}
    foreach ($outcome in $Outcomes) {
        $byHost[$outcome.Host] = $outcome
    }
    $done = $Outcomes.Count
    $finished = $done -eq $servers.Count
    $states = @(foreach ($server in $servers) {
            if ($byHost.ContainsKey($server.Name)) { $byHost[$server.Name].Status } else { 'Pending' }
        })
    $failed = @($Outcomes | Where-Object -Property Status -EQ -Value Fail).Count
    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleRule -Title '[bold]Patching the web farm[/]'
        '40 hosts · throttle 8 · per-host timeout 4s'
        New-ConsoleSummaryBar -Total $servers.Count -Counter ([ordered] @{
                passed = $done - $failed
                failed = $failed
                running = 0
                pending = $servers.Count - $done
            }) -Value $done
        New-ConsolePanel -Title 'Host outcomes' -Content {
            $mapOptions = @{
                Status = $states
                Columns = 10
                LegendLines = 2
                RowLabels = 'web01–10', 'web11–20', 'web21–30', 'web31–40'
            }
            New-ConsoleCellMap @mapOptions
        }
        $laneHosts = if ($finished) { $servers | Select-Object -Last $throttle } else { $servers | Select-Object -First $throttle }
        New-ConsolePanel -Title $(if ($finished) { 'Last eight hosts' } else { 'Worker lanes' }) -Content {
            New-ConsoleLanes -Content {
                foreach ($server in $laneHosts) {
                    $outcome = $byHost[$server.Name]
                    $laneOptions = @{
                        Key = $server.Name
                        Total = 4
                        Value = if ($outcome) { 4 } else { 0 }
                        Status = if ($outcome) { $outcome.Status } else { 'Pending' }
                        Passed = if (-not $outcome) { 0 } elseif ($outcome.Status -eq 'Pass') { 4 } else { 3 }
                        Failed = if ($outcome -and $outcome.Status -eq 'Fail') { 1 } else { 0 }
                        Detail = if (-not $outcome) { 'Waiting for a worker' } else { $outcome.Detail }
                    }
                    [pscustomobject]$laneOptions
                }
            }
        }
        New-ConsolePanel -Title 'Retained failures' -Content {
            if ($failed -eq 0) {
                '[muted]No failures recorded[/]'
            }
            else {
                New-ConsoleStatusList -Content {
                    foreach ($outcome in $Outcomes | Where-Object -Property Status -EQ -Value Fail) {
                        [pscustomobject]@{ Label = $outcome.Host; Status = 'Fail'; Detail = $outcome.Detail }
                    }
                }
            }
        }
        if ($finished) { '[bold]Wave finished[/] · failed hosts remain available for a separate retry' } else { '[muted]Ctrl+C cancels active work and skips queued hosts[/]' }
    }
}

# Behaviour: the native run monitor owns scheduling, cancellation and the live UI.
New-PatchSummary -Outcomes @() | Show-Console
$runOptions = @{
    Parallel = $true
    ThrottleLimit = $throttle
    TimeoutSeconds = $timeoutSeconds
    Title = 'Patching the web farm'
    ErrorAction = 'SilentlyContinue'
    ErrorVariable = 'retainedErrors'
}
$successes = @($servers | Invoke-ConsoleRun @runOptions -Label Name -ScriptBlock {
        Start-Sleep -Milliseconds $_.DelayMilliseconds
        if ($_.RestartFailure) {
            throw "$($_.Name) restart verification failed"
        }
        if ($_.TimesOut) {
            Start-Sleep -Seconds 10
        }
        $_.Name
    })
# Keep both success objects and ErrorRecords; never retry automatically.
$outcomes = foreach ($server in $servers) {
    $passed = $server.Name -in $successes
    [pscustomobject] @{ Host = $server.Name; Status = if ($passed) { 'Pass' } else { 'Fail' }; Detail = if ($passed) { 'Health verified' } elseif ($server.TimesOut) { '4s timeout · retry separately' } else { 'Restart verification failed' } }
}
New-PatchSummary -Outcomes @($outcomes) | Show-Console
