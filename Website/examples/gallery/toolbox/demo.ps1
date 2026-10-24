[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 2)] [int] $Step = 0,
    [hashtable] $AppOptions = @{}
)

Import-Module ConsoleMonster
$AppOptions.StaticSnapshot = $StaticSnapshot -or ([Console]::IsInputRedirected -and -not $AppOptions.InputSource)

$tasks = @('Health report', 'Inspect queue', 'Preview cleanup')
$selected = $tasks[$Step]
Show-ConsoleApp {
    New-ConsoleRows -Gap 1 {
        '[bold]Server toolbox[/] [muted]· safe local previews[/]'
        New-ConsoleSelect -Name Task -Label Task -Choices $tasks -Value $selected
        New-ConsoleBinding {
            param($State)
            New-ConsolePanel -Title Preview {
                switch ([string] $State.Task) {
                    'Health report' { '[pass]web-01 reachable[/]'; 'Sample: CPU 24% · memory 61%' }
                    'Inspect queue' { '[accent]12 jobs waiting[/]'; 'Sample: oldest job 4 minutes' }
                    'Preview cleanup' { '[warn]3 sample artifacts eligible[/]'; 'Preview only; no files are removed.' }
                }
            }
        }
    }
} @AppOptions
