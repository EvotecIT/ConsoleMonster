# Select a server task and show its read-only preview.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0,
    [hashtable] $AppOptions = @{}
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.
$AppOptions.StaticSnapshot = $StaticSnapshot -or ([Console]::IsInputRedirected -and -not $AppOptions.InputSource)

# Data: fictional inputs kept in memory.
$tasks = @('Health report', 'Inspect queue', 'Preview cleanup')
$selected = $tasks[$Step]
# Layout: construct the widgets shown on screen.
Show-ConsoleApp {
    New-ConsoleRows -Gap 1 {
        '[bold]Server toolbox[/] [muted]· operations[/]'
        New-ConsoleSelect -Name Task -Label Task -Choices $tasks -Value $selected
        # Live updates: rebuild this section when an input or button changes state.
        New-ConsoleBinding {
            param($State)
            New-ConsolePanel -Title Preview {
                switch ([string] $State.Task) {
                    'Health report' { '[pass]web-01 reachable[/]'; 'CPU 24% · memory 61%' }
                    'Inspect queue' { '[accent]12 jobs waiting[/]'; 'Oldest job 4 minutes' }
                    'Preview cleanup' { '[warn]3 artifacts eligible[/]'; 'Eligible artifacts: build-cache, test-output, old-logs.' }
                }
            }
        }
    }
} @AppOptions
