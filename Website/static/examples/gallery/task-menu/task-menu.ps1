# Select a server task and show its read-only preview.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
$tasks = @('Health report', 'Inspect queue', 'Preview cleanup')
$selected = $tasks[0]
# Layout: construct the widgets shown on screen.
$content = {
    New-ConsoleRows -Gap 1 -Content {
        '[bold]Server toolbox[/] [muted]· operations[/]'
        New-ConsoleSelect -Name Task -Label Task -Choices $tasks -Value $selected
        # Rebuild this section when an input or button changes state.
        New-ConsoleBinding -Content {
            param($State)
            New-ConsolePanel -Title Preview -Content {
                switch ([string] $State.Task) {
                    'Health report' {
                        '[pass]web-01 reachable[/]'
                        'CPU 24% · memory 61%'
                    }
                    'Inspect queue' {
                        '[accent]12 jobs waiting[/]'
                        'Oldest job 4 minutes'
                    }
                    'Preview cleanup' {
                        '[warn]3 artifacts eligible[/]'
                        'Eligible artifacts: build-cache, test-output, old-logs.'
                    }
                }
            }
        }
    }
}

# Behaviour: edit controls to update the view; Escape exits the app.
Show-ConsoleApp -Content $content
