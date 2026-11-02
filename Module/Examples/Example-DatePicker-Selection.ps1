Import-Module ConsoleMonster

$picker = New-ConsoleDatePicker -Key Window -Range -Selection @{
    Today = [datetime]'2026-10-08'
    FirstDayOfWeek = 'Monday'
    Minimum = [datetime]'2026-10-01'
    Maximum = [datetime]'2026-10-31'
}

Show-ConsoleApp -Title 'Maintenance window' -Content { $picker }
