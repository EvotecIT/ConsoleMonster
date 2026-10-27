Import-Module ConsoleMonster

$checks = @(
    [pscustomobject]@{ Name = 'Read inventory'; Target = 'node-01'; Category = 'Inventory' }
    [pscustomobject]@{ Name = 'Check storage'; Target = 'node-02'; Category = 'Storage' }
)
$run = Start-ConsoleRun -Items $checks -Label Name -Target Target -Group {
    $_.Category
} -Title 'Fleet checks'
try {
    Update-Console -Run $run -Index 0 -Status Running -Label 'collecting'
    # Run your inventory check here, then report its result.
    Update-Console -Run $run -Index 0 -Status Pass
    Update-Console -Run $run -Index 1 -Status Running -Label 'evaluating'
    $consoleParameters = @{
        Run = $run
        Index = 1
        Status = 'Warn'
        Detail = 'Free space is below the preferred threshold'
    }
    Update-Console @consoleParameters
} finally {
    $summary = Complete-ConsoleRun $run -PassThru
}
