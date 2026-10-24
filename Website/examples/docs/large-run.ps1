Import-Module ConsoleMonster

$checks = @(
    [pscustomobject]@{ Name = 'Read inventory'; Target = 'node-01'; Category = 'Inventory' }
    [pscustomobject]@{ Name = 'Check storage'; Target = 'node-02'; Category = 'Storage' }
)
$run = Start-ConsoleRun -Items $checks -Label Name -Target Target -Group { $_.Category } -Title 'Fleet checks'
try {
    Update-ConsoleRun $run -Index 0 -Status Running -Phase 'collecting'
    # Run your inventory check here, then report its result.
    Update-ConsoleRun $run -Index 0 -Status Pass
    Update-ConsoleRun $run -Index 1 -Status Running -Phase 'evaluating'
    Update-ConsoleRun $run -Index 1 -Status Warn -Message 'Free space is below the preferred threshold'
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
