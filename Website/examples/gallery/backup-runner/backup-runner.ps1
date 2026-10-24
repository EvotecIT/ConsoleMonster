# Run a bounded backup wave and retry only jobs that did not return a result.
param([switch] $StaticSnapshot, [ValidateRange(0, 2)][int] $Step = 0)
Import-Module ConsoleMonster

# Data: synthetic backup jobs; no files, databases or remote services are touched.
$jobs = @(
    [pscustomobject] @{ Name = 'catalog'; Size = '2.4 GiB'; Retry = $false }
    [pscustomobject] @{ Name = 'media'; Size = '8.1 GiB'; Retry = $true }
    [pscustomobject] @{ Name = 'settings'; Size = '14 MiB'; Retry = $false }
    [pscustomobject] @{ Name = 'audit'; Size = '640 MiB'; Retry = $false }
)

# Layout: queued, first-wave and recovered states use the same result columns.
function New-BackupFrame($Tick) {
    $rows = foreach ($job in $jobs) {
        $status = if ($Tick -eq 0) { 'Pending' } elseif ($Tick -eq 1 -and $job.Retry) { 'Warn' } else { 'Pass' }
        $attempts = if ($Tick -eq 0) { 0 } elseif ($Tick -eq 2 -and $job.Retry) { 2 } else { 1 }
        [pscustomobject] @{ Job = $job.Name; Size = $job.Size; Attempts = $attempts; Status = $status }
    }
    New-ConsoleRows -Gap 1 {
        New-ConsoleRule '[bold]Nightly backup[/] [muted]· throttle 2 · maximum 2 attempts[/]'
        New-ConsoleTable -InputObject $rows -Property Job, Size, Attempts, Status -Expand
        if ($Tick -eq 0) { '[muted]4 jobs queued · destination ./backups/nightly[/]' }
        elseif ($Tick -eq 1) { '[pass]3 passed[/]   [warn]1 queued for retry[/]' }
        else { '[pass]4 passed[/]   [accent]1 recovered on retry[/]   [muted]retention: 14 days[/]' }
    }
}

# Behaviour: headless snapshots do not start workers. Live jobs emit their result on success.
if ($StaticSnapshot -or [Console]::IsInputRedirected) {
    New-BackupFrame $Step | Write-ConsoleWidget
    return
}
$pending = $jobs
$completed = @()
$attemptErrors = @()
foreach ($attempt in 1..2) {
    if ($pending.Count -eq 0) { break }
    $results = @($pending | ForEach-ConsoleItem -Parallel -ThrottleLimit 2 -Name Name -Title "Backup attempt $attempt" -ErrorAction SilentlyContinue -ErrorVariable failures {
        Start-Sleep -Milliseconds 250
        if ($_.Retry -and $using:attempt -eq 1) { throw 'Destination busy; retry this job' }
        [pscustomobject] @{ Name = $_.Name; Archive = "./backups/nightly/$($_.Name).zip" }
    })
    $completed += $results
    $attemptErrors += $failures
    $pending = @($pending | Where-Object { $_.Name -notin $results.Name })
}
# $completed holds archive results; $pending and $attemptErrors retain unresolved work and diagnostics.
New-BackupFrame 2 | Write-ConsoleWidget
