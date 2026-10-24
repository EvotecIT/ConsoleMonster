param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 0)][int]$Step = 0
)

Import-Module ConsoleMonster

# Fixed sample results: no checks run against your machine or any service.
# Replace these items with results you already have in memory.
$checks = @(
    @{ Name = 'Configuration'; Status = 'Pass'; Detail = 'All required settings present' }
    @{ Name = 'Dependencies'; Status = 'Pass'; Detail = 'No conflicting versions' }
    @{ Name = 'Test coverage'; Status = 'Warn'; Detail = 'Two scenarios need attention' }
    @{ Name = 'Documentation'; Status = 'Pass'; Detail = 'Examples match the current API' }
    @{ Name = 'Recovery plan'; Status = 'Fail'; Detail = 'Restore rehearsal still needed' }
)
$scores = [ordered]@{ Configuration = 100; Coverage = 72; Recovery = 35 }

New-ConsoleRows -Gap 1 {
    New-ConsoleRule '[bold]Readiness assessment[/] [muted]| sample workspace[/]'
    New-ConsoleText -Markup '[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]'
    New-ConsolePanel -Title 'Checks' {
        New-ConsoleStatusList {
            foreach ($check in $checks) {
                New-ConsoleStatusItem $check.Name -Status $check.Status -Detail $check.Detail
            }
        }
    }
    New-ConsolePanel -Title 'Readiness by area' -Caption 'score / 100' {
        New-ConsoleBarChart -Data $scores -Maximum 100 -Unit '%' -Warning 80 -Critical 50
    }
    New-ConsoleText -Markup '[bold]Next:[/] rehearse a restore, then add the two missing test scenarios.'
    New-ConsoleText -Markup '[muted]Illustrative results only. No live assessment has been performed.[/]'
} | Write-ConsoleWidget
