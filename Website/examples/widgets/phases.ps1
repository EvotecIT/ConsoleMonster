$phases = New-ConsolePhases -Phase Discover, Collect, Evaluate, Report
$phases.Set('Discover', 'Pass', 'Discover', '2.1s').
    Set('Collect', 'Pass', 'Collect', '14s').
    Set('Evaluate', 'Running', 'Evaluate', '64%') | Write-ConsoleWidget
