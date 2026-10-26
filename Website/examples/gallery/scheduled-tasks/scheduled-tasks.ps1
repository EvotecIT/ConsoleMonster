# Check scheduled task health and trace each failure from its trigger to the action that broke.
Import-Module -Name ConsoleMonster

# Data: fictional tasks on one server; Task Scheduler is not queried and nothing is run.
$tasks = @(
    [pscustomobject] @{ Task = 'Nightly SQL backup'; Trigger = 'Daily 02:00'; LastRun = '10-26 02:00'; Result = '0x1'; NextRun = '10-27 02:00'; Status = 'Fail' }
    [pscustomobject] @{ Task = 'Certificate renewal'; Trigger = 'Mon 03:00'; LastRun = '10-26 03:00'; Result = '0x80070005'; NextRun = '11-02 03:00'; Status = 'Fail' }
    [pscustomobject] @{ Task = 'Report export'; Trigger = 'Daily 06:30'; LastRun = '10-26 06:30'; Result = '0x41301'; NextRun = '10-27 06:30'; Status = 'Info' }
    [pscustomobject] @{ Task = 'Log cleanup'; Trigger = 'Daily 01:00'; LastRun = '10-26 01:00'; Result = '0x0'; NextRun = '10-27 01:00'; Status = 'Pass' }
    [pscustomobject] @{ Task = 'Inventory sync'; Trigger = 'Hourly'; LastRun = '10-26 09:00'; Result = '0x0'; NextRun = '10-26 10:00'; Status = 'Pass' }
    [pscustomobject] @{ Task = 'Defragment volumes'; Trigger = 'Sun 04:00'; LastRun = '10-25 04:00'; Result = '0x0'; NextRun = '11-01 04:00'; Status = 'Pass' }
    [pscustomobject] @{ Task = 'Legacy FTP upload'; Trigger = 'Disabled'; LastRun = '09-30 23:00'; Result = '0x0'; NextRun = '-'; Status = 'Skip' }
)
$failures = @(
    [pscustomobject] @{ Task = 'Nightly SQL backup'; Trigger = 'Daily 02:00'; Action = 'Backup-Sql.ps1'; Outcome = 'exit 1'; Cause = 'Target share \\nas-01\sql is full'; Fix = 'Free space or move the target, then run the task once by hand.' }
    [pscustomobject] @{ Task = 'Certificate renewal'; Trigger = 'Mon 03:00'; Action = 'Renew-Certificates.ps1'; Outcome = 'access denied'; Cause = 'svc-certs lost Log on as a batch job'; Fix = 'Restore the right in the server baseline policy.' }
)

# Layout: the task table first, then one trigger → task → action trace per failure, side by side.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Scheduled tasks[/] [muted]· app-01.adatum.example.test · 2026-10-26 09:05[/]'
    New-ConsoleTable -InputObject $tasks -Property Task, Trigger, LastRun, Result, NextRun, Status -Expand
    New-ConsoleColumns -Content {
        foreach ($failure in $failures) {
            New-ConsolePanel -Title "$($failure.Task) · $($failure.Outcome)" -Status Fail -Content {
                New-ConsoleRows -Gap 1 -Content {
                    @(
                        [pscustomobject] @{ Kind = 'Node'; Key = 'trigger'; Label = $failure.Trigger; Status = 'Pass' }
                        [pscustomobject] @{ Kind = 'Node'; Key = 'task'; Label = $failure.Task; Status = 'Pass' }
                        [pscustomobject] @{ Kind = 'Node'; Key = 'action'; Label = $failure.Action; Status = 'Fail' }
                        [pscustomobject] @{ Kind = 'Edge'; Key = 'fires'; From = 'trigger'; To = 'task'; Label = 'fired' }
                        [pscustomobject] @{ Kind = 'Edge'; Key = 'runs'; From = 'task'; To = 'action'; Label = $failure.Outcome; Status = 'Fail'; Cut = $true }
                    ) | New-ConsoleFlow
                    New-ConsoleKeyValue -KeyRole Fail -Content {
                        [pscustomobject] @{ Key = 'Cause'; Value = $failure.Cause }
                        [pscustomobject] @{ Key = 'Fix'; Value = $failure.Fix }
                    }
                }
            }
        }
    }
}

# Behaviour: print once; fix the cause, then start the task by hand and rerun this check.
$report | Show-Console
