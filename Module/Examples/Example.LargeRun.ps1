# Monitor work your script already schedules. Indices refer to this input order.
$checks = @(
    [pscustomobject]@{ Name = 'Read inventory'; Target = 'node-01'; Category = 'Inventory' }
    [pscustomobject]@{ Name = 'Check storage'; Target = 'node-02'; Category = 'Storage' }
)
$run = New-ConsoleRun -Items $checks -Name Name -Target Target -Group { $_.Category } -Title 'Fleet checks'
try {
    Update-ConsoleRun $run -Index 0 -State Running -Phase 'collecting'
    Update-ConsoleRun $run -Index 0 -State Pass
    Update-ConsoleRun $run -Index 1 -State Running -Phase 'evaluating'
    Update-ConsoleRun $run -Index 1 -State Warn -Message 'Free space is below the preferred threshold'
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
$summary.Items
