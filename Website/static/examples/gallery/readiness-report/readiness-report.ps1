# Show a readiness report with status checks and score bars.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
# Fixed sample results: no checks run against your machine or any service.
# Replace these items with results you already have in memory.
$checks = @(
    [pscustomobject] @{ Name = 'Configuration'; Status = 'Pass'; Detail = 'All required settings present' }
    [pscustomobject] @{ Name = 'Dependencies'; Status = 'Pass'; Detail = 'No conflicting versions' }
    [pscustomobject] @{ Name = 'Test coverage'; Status = 'Warn'; Detail = 'Two scenarios need attention' }
    [pscustomobject] @{ Name = 'Documentation'; Status = 'Pass'; Detail = 'Examples match the current API' }
    [pscustomobject] @{ Name = 'Recovery plan'; Status = 'Fail'; Detail = 'Restore rehearsal still needed' }
)
$scores = [ordered]@{
    Configuration = 100
    Coverage = 72
    Recovery = 35
}

# Layout: compose the screen from the data above.
$screen = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Readiness assessment[/] [muted]| workspace[/]'
    New-ConsoleText -Markup '[pass]3 passed[/]   [warn]1 warning[/]   [fail]1 failed[/]   [muted]5 checks total[/]'
    New-ConsolePanel -Title 'Checks' -Content {
        New-ConsoleStatusList -Content {
            foreach ($check in $checks) {
                [pscustomobject]@{ Label = $check.Name; Status = $check.Status; Detail = $check.Detail }
            }
        }
    }
    New-ConsolePanel -Title 'Readiness by area' -HeaderRight 'score / 100' -Content {
        New-ConsoleChart -InputObject $scores -Maximum 100 -Unit '%' -Warning 80 -Critical 50 -Type Bar
    }
    New-ConsoleText -Markup '[bold]Next:[/] rehearse a restore, then add the two missing test scenarios.'
    New-ConsoleText -Markup '[muted]Review owner: Platform team · next review: Friday[/]'
}

# Behaviour: print the screen once and return.
$screen | Show-Console
