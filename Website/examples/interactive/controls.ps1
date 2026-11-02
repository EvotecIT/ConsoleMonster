Import-Module ConsoleMonster

# A fixed "today", so the calendar opens on the same month every time.
$dates = @{
    Today = [datetime] '2026-11-18'; FirstDayOfWeek = 'Monday'
}

Show-ConsoleApp -Title Maintenance -Content {
    New-ConsoleTabs -Content ([ordered] @{
        Schedule = {
            New-ConsoleToggle -Key Enabled -Label 'Run nightly checks' -Value $true
            New-ConsoleDatePicker -Key Window -Range -Selection $dates
        }
        Notify = {
            New-ConsoleToggle -Key Email -Label 'Email the report'
            New-ConsoleToggle -Key Teams -Label 'Post to Teams'
        }
    })
}
