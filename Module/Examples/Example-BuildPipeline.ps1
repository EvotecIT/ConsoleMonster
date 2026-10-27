Import-Module ConsoleMonster

# A simulated module build pipeline: phases, steps with timings, module facts and the tool output.
# The script runs in a runspace ConsoleMonster owns: Write-Host, Write-Warning and Write-Verbose land in the log
# region instead of writing over the live screen.

$steps = @(
    @{ Phase = 'Prepare'; Key = 'config'; Label = 'Read Build/project.build.json'; Seconds = 0.5; Result = 'Pass' }
    @{ Phase = 'Restore'; Key = 'restore'; Label = 'Restore NuGet packages'; Seconds = 1.5; Result = 'Pass' }
    @{ Phase = 'Build'; Key = 'core'; Label = 'Compile ConsoleMonster (3 frameworks)'; Seconds = 2.5; Result = 'Pass' }
    @{ Phase = 'Build'; Key = 'ps'; Label = 'Compile ConsoleMonster.PowerShell'; Seconds = 1.5; Result = 'Pass' }
    @{ Phase = 'Test'; Key = 'xunit'; Label = 'Run xUnit tests'; Seconds = 2.5; Result = 'Pass' }
    @{ Phase = 'Test'; Key = 'pester'; Label = 'Run Pester tests'; Seconds = 2.0; Result = 'Warn' }
    @{ Phase = 'Docs'; Key = 'docs'; Label = 'Generate help (38 commands)'; Seconds = 1.5; Result = 'Pass' }
    @{ Phase = 'Package'; Key = 'pack'; Label = 'Pack module and NuGet package'; Seconds = 1.5; Result = 'Pass' }
    @{ Phase = 'Publish'; Key = 'publish'; Label = 'Publish to PowerShell Gallery'; Seconds = 0.3; Result = 'Skip' }
)

$dashboard = New-ConsoleDashboard -Title '[bold]Module build[/]' -Subtitle 'Build-Module ConsoleMonster' -Gap 1 -Content {
    New-ConsolePhases -Phase 'Prepare', 'Restore', 'Build', 'Test', 'Docs', 'Package', 'Publish'
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title 'Steps' -Weight 1.5 -Content { New-ConsoleStatusList -WidgetId steps -Item $steps.Label }
        New-ConsolePanel -Title 'Module' -Content {
            New-ConsoleKeyValue -WidgetId module -InputObject ([ordered] @{ Module = 'ConsoleMonster'; Version = '2.0.0'; Frameworks = 'netstandard2.0 · net472 · net8.0'; Elapsed = '-' })
            New-ConsoleRule
            New-ConsoleProgress -WidgetId overall -Label 'Overall' -Total $steps.Count
        }
    }
    New-ConsolePanel -Title 'Output' -Fill -Content { New-ConsoleLogRegion }
}

# Planned step durations drive the displayed clocks; dashboard waits keep normal playback paced in real time.
$clock = New-ConsoleSimulatedClock
Invoke-ConsoleDashboard $dashboard -Clock $clock -ArgumentList $clock, $steps {
    param($Clock, $Steps)
    $phase = $null
    $index = 0
    foreach ($step in $Steps) {
        if ($step.Phase -ne $phase) {
            if ($phase) {
                Update-Console -Key $phase -Status Pass -WidgetId 'phases'
            }
            $phase = $step.Phase
            Update-Console -Key $phase -Status Running -WidgetId 'phases'
        }
        Update-Console -WidgetId steps -Key $step.Label -Status Running
        Write-Host "$($step.Label)..."
        Wait-ConsoleDashboard -Milliseconds ([int] ($step.Seconds * 1000))
        $Clock.Advance($step.Seconds)
        if ($step.Result -eq 'Warn') {
            Write-Warning '1 Pester test skipped on PowerShell 7 (requires conhost)'
        }
        Update-Console -WidgetId steps -Key $step.Label -Status $step.Result -Detail ($step.Seconds.ToString('0.0', [cultureinfo]::InvariantCulture) + 's')
        $index++
        Update-Console -WidgetId overall -Value $index
        Update-Console -WidgetId module -Key Elapsed -Value ($Clock.Elapsed.TotalSeconds.ToString('0.0', [cultureinfo]::InvariantCulture) + 's')
    }
    Update-Console -Key $phase -Status Skip -WidgetId 'phases'
    Update-Console -WidgetId overall -Status Pass
    Write-Host 'Build completed: Artefacts\Packed\ConsoleMonster-PowerShellModule.v2.0.0.zip'
    Wait-ConsoleDashboard -Milliseconds 2000
}
