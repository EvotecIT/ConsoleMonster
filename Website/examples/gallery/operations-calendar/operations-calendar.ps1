# Review fictional backups, change windows and certificate expiries.
Import-Module -Name ConsoleMonster

# Data: fixed civil dates and a fixed report clock.
$today = [datetime]'2026-10-20'
$days = @(
    [pscustomobject] @{ Date = [datetime]'2026-10-02'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-09'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-16'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Warn'
        Label = 'Certificate expires' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Pending'
        Label = 'Backup scheduled' }
    [pscustomobject] @{ Date = [datetime]'2026-10-28'; Status = 'Info'; Label = 'Change window' }
    [pscustomobject] @{ Date = [datetime]'2026-10-14'; Status = 'Fail'
        Label = 'Backup verification failed' }
    [pscustomobject] @{ Date = [datetime]'2026-10-26'; Status = 'Pending'
        Label = 'Restore drill scheduled' }
    [pscustomobject] @{ Date = [datetime]'2026-10-30'; Status = 'Pending'
        Label = 'Backup scheduled' }
)

# Behaviour: duplicate dates retain all labels and use the calendar precedence.
# Pin the English fixture labels instead of inheriting ICU/NLS shortest names.
$calendarCulture = ([cultureinfo]::GetCultureInfo('en-US')).Clone()
$calendarCulture.DateTimeFormat.ShortestDayNames = @('Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa')
$calendar = $days | New-ConsoleCalendar -Year 2026 -Month 10 -FirstDayOfWeek Monday -Today $today -Culture $calendarCulture
$upcoming = $days | Where-Object { $_.Date -ge $today -and $_.Date -le $today.AddDays(14) } |
    Group-Object { $_.Date.Date } | Sort-Object { $_.Group[0].Date }

# Layout: a month overview beside the next two weeks from the same rows.
$report = New-ConsoleColumns -Content {
    New-ConsolePanel -Title 'Operations' -Content { $calendar }
    New-ConsolePanel -Title 'Upcoming · next 14 days' -Content {
        New-ConsoleRows -Content {
            foreach ($group in $upcoming) {
                New-ConsoleText -Text $group.Group[0].Date.ToString('ddd dd MMM',
                    [cultureinfo]'en-US')
                New-ConsolePadding -Padding 2, 0, 0, 1 -Content {
                    New-ConsoleStatusList -Content {
                        foreach ($day in $group.Group) {
                            [pscustomobject] @{ Key = $day.Label; Label = $day.Label
                                Status = $day.Status }
                        }
                    }
                }
            }
        }
    }
}
$report | Show-Console
