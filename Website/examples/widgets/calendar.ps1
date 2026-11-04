Import-Module ConsoleMonster

# Each date takes the most severe status of its events; today is outlined.
@(
    @{ Date = [datetime]'2026-11-03'; Status = 'Pass'; Label = 'Backup verified' }
    @{ Date = [datetime]'2026-11-10'; Status = 'Fail'; Label = 'Backup failed' }
    @{ Date = [datetime]'2026-11-17'; Status = 'Pass'; Label = 'Backup verified' }
    @{ Date = [datetime]'2026-11-19'; Status = 'Warn'; Label = 'Certificate expires' }
    @{ Date = [datetime]'2026-11-24'; Status = 'Info'; Label = 'Change window' }
    @{ Date = [datetime]'2026-11-27'; Status = 'Pending'; Label = 'Restore drill' }
) | ForEach-Object { [pscustomobject] $_ } |
    New-ConsoleCalendar -Year 2026 -Month 11 -FirstDayOfWeek Monday -Today '2026-11-18' |
    Show-Console
