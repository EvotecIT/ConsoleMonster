# Review synthetic backups, change windows and certificate expiries by date.
Import-Module -Name ConsoleMonster

# Data: fixed dates and fictional outcomes, without reading local services or stores.
$days = @(
    [pscustomobject] @{ Date = [datetime]'2026-10-02'; Status = 'Pass'; Label = 'Backup verified' }
    [pscustomobject] @{ Date = [datetime]'2026-10-08'; Status = 'Info'; Label = 'Change window' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Warn'; Label = 'Certificate expires' }
    [pscustomobject] @{ Date = [datetime]'2026-10-23'; Status = 'Pass'; Label = 'Backup verified' }
)

# Layout: one month with an ISO week gutter and a status legend.
$calendar = $days | New-ConsoleCalendar -Year 2026 -Month 10 -FirstDayOfWeek Monday

# Behaviour: print once; convert source timestamps to the business time zone before binding.
$report = New-ConsoleRows -Content {
    $calendar
    foreach ($day in $days) { '{0:dd}  {1}' -f $day.Date, $day.Label }
}
$report | Show-Console
