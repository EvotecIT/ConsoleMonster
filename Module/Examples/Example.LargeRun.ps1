# Monitor work your script already schedules. Indices refer to this input order.
$checks = @(
    [pscustomobject]@{ Name = 'Read inventory'; Target = 'node-01'; Category = 'Inventory' }
    [pscustomobject]@{ Name = 'Check storage'; Target = 'node-02'; Category = 'Storage' }
)
$run = Start-ConsoleRun -InputObject $checks -Label Name -Target Target -Group { $_.Category } -Title 'Fleet checks'
try {
    Update-Console -Run $run -Index 0 -Status Running -Label 'collecting'
    Update-Console -Run $run -Index 0 -Status Pass
    Update-Console -Run $run -Index 1 -Status Running -Label 'evaluating'
    Update-Console -Run $run -Index 1 -Status Warn -Detail 'Free space is below the preferred threshold'
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
$summary.Items
