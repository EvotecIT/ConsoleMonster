# Animate a build checklist and overall progress without running a build.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$stages = @('Restore', 'Compile', 'Test', 'Package')
# Layout: construct the widgets shown on screen.
function New-DemoFrame {
    param(
        $Tick
    )

    New-ConsoleRows -Gap 1 -Content {
        '[bold]Build pipeline[/] [muted]· Orbit.Tools[/]'
        New-ConsolePanel -Title 'Release checklist' -Content {
            foreach ($index in 0..3) {
                $status = if ($index -lt $Tick) { 'pass' } elseif ($index -eq $Tick) { 'accent' } else { 'muted' }
                $label = if ($index -lt $Tick) { 'complete' } elseif ($index -eq $Tick) { 'running' } else { 'pending' }
                "[$status]$($stages[$index])[/]  $label"
            }
        }
        New-ConsoleProgress -Label Overall -Value $Tick -Total 4
        New-ConsoleKeyValue -InputObject ([ordered] @{
                'Project' = 'Orbit.Tools'
                'Version' = '1.2.0'
                'Artifacts' = './artifacts/'
                'Publish' = 'manual approval'
            })
        '[muted]Ctrl+C stops the live run.[/]'
    }
}

# Behaviour: update each frame in a bounded live run.
$frames = @(0..4 | ForEach-Object -Process { New-DemoFrame -Tick $_ })
$screen = New-ConsoleRegion -WidgetId demo -Widget $frames[0]
Invoke-ConsoleDashboard -Dashboard $screen -ArgumentList (, $frames) -ScriptBlock {
    param($Frames)
    foreach ($frame in $Frames) {
        if ($ConsoleSession.Cancellation.IsCancellationRequested) {
            break
        }
        Set-ConsoleRegion -WidgetId demo -Widget $frame
        Wait-ConsoleDashboard -Milliseconds 650
    }
}
