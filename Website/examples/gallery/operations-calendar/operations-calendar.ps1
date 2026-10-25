# Review fictional backups, change windows and certificate expiries.
Import-Module -Name ConsoleMonster

# Data: fixed civil dates and a fixed report clock.
$today = [datetime]'2026-10-20'
$days = @(
    [pscustomobject] @{ Date = [datetime]'2026-10-02'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-09'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-16'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Warn'; Label = 'Certificate expires' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-28'; Status = 'Info'; Label = 'Change window' }
    [pscustomobject] @{ Date = [datetime]'2026-10-14'; Status = 'Fail'; Label = 'Backup verification failed' }
    [pscustomobject] @{ Date = [datetime]'2026-10-26'; Status = 'Pending'; Label = 'Restore drill scheduled' }
    [pscustomobject] @{ Date = [datetime]'2026-10-30'; Status = 'Pass'; Label = 'Backup verified' }
)

# Behaviour: duplicate dates retain all labels and use the calendar precedence.
$calendar = $days | New-ConsoleCalendar -Year 2026 -Month 10 -FirstDayOfWeek Monday -Today $today
$upcoming = $days | Where-Object { $_.Date -ge $today -and $_.Date -le $today.AddDays(14) } |
    Group-Object { $_.Date.Date } | Sort-Object { $_.Group[0].Date } | ForEach-Object {
        $date = $_.Group[0].Date
        [pscustomobject] @{ Date = $date.ToString('ddd dd MMM', [cultureinfo]'en-US'); Status = $calendar.Data.GetStatus($date); Label = ($_.Group.Label -join ' · ') }
    }

# Layout: a month overview beside the next two weeks from the same rows.
$report = New-ConsoleColumns -Content {
    New-ConsolePanel -Title 'Operations · October' -Content { $calendar }
    New-ConsolePanel -Title 'Upcoming · next 14 days' -Content {
        New-ConsoleRows -Content {
            foreach ($day in $upcoming) {
                New-ConsoleStatusList -Content { New-ConsoleStatusItem -Key $day.Date -Label $day.Date -Status $day.Status }
                New-ConsolePadding -Padding 2, 0, 0, 1 -Content { New-ConsoleText -Text $day.Label }
            }
        }
    }
}
$report | Show-Console
