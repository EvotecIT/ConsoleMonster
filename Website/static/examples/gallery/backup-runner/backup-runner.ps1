# Run a bounded backup wave and retry only jobs that did not return a result.
Import-Module -Name ConsoleMonster

# Data: synthetic backup jobs; no files, databases or remote services are touched.
$jobs = @(
    [pscustomobject] @{ Name = 'catalog'; Size = '2.4 GiB'; Retry = $false }
    [pscustomobject] @{ Name = 'media'; Size = '8.1 GiB'; Retry = $true }
    [pscustomobject] @{ Name = 'settings'; Size = '14 MiB'; Retry = $false }
    [pscustomobject] @{ Name = 'audit'; Size = '640 MiB'; Retry = $false }
)

$attempts = @{}
$completed = @{}
$attemptErrors = @()

# Layout: show the actual attempt counts and returned archive results.
function New-BackupSummary {
    $rows = foreach ($job in $jobs) {
        $count = if ($attempts.ContainsKey($job.Name)) { $attempts[$job.Name] } else { 0 }
        $status = if ($completed.ContainsKey($job.Name)) { 'Pass' }
        elseif ($count -eq 0) { 'Pending' }
        else { 'Warn' }
        [pscustomobject] @{ Job = $job.Name; Size = $job.Size; Attempts = $count; Status = $status }
    }
    New-ConsoleRows -Gap 1 -Content {
        New-ConsoleRule -Title '[bold]Nightly backup[/] [muted]· throttle 2 · maximum 2 attempts[/]'
        New-ConsoleTable -InputObject $rows -Property Job, Size, Attempts, Status -Expand
        $recovered = @($completed.Keys | Where-Object -FilterScript { $attempts[$_] -gt 1 }).Count
        if ($attempts.Count -eq 0) {
            '[muted]4 jobs queued · destination ./backups/nightly[/]'
        }
        elseif ($completed.Count -lt $jobs.Count) {
            "[pass]$($completed.Count) passed[/]   [warn]$($jobs.Count - $completed.Count) queued for retry[/]"
        }
        else {
            "[pass]$($completed.Count) passed[/]   [accent]$recovered recovered on retry[/]   [muted]retention: 14 days[/]"
        }
    }
}

# Behaviour: run the queued jobs, retain errors, then retry only missing results.
New-BackupSummary | Show-Console
$pending = $jobs
foreach ($attempt in 1..2) {
    if ($pending.Count -eq 0) {
        break
    }
    foreach ($job in $pending) {
        $attempts[$job.Name] = $attempt
    }
    $runOptions = @{
        Parallel = $true
        ThrottleLimit = 2
        Label = 'Name'
        Title = "Backup attempt $attempt"
        ErrorAction = 'SilentlyContinue'
        ErrorVariable = 'failures'
    }
    $results = @($pending | Invoke-ConsoleRun @runOptions -ScriptBlock {
            Start-Sleep -Milliseconds 250
            if ($_.Retry -and $using:attempt -eq 1) {
                throw 'Destination busy; retry this job'
            }
            [pscustomobject] @{ Name = $_.Name; Archive = "./backups/nightly/$($_.Name).zip" }
        })
    foreach ($result in $results) {
        $completed[$result.Name] = $result.Archive
    }
    if ($failures) {
        $attemptErrors += $failures
    }
    $pending = @($pending | Where-Object -FilterScript { -not $completed.ContainsKey($_.Name) })
    New-BackupSummary | Show-Console
}
# $completed holds archive results; $pending and $attemptErrors retain unresolved work and diagnostics.
