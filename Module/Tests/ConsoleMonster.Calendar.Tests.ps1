Describe 'Calendar display' {
    BeforeAll {
        Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    }

    It 'groups duplicate rows without shifting offset-bearing dates' {
        $rows = @(
            [pscustomobject] @{ Date = '2026-10-25T23:50:00-07:00'; Status = 'Pass'; Label = 'Backup' }
            [pscustomobject] @{ Date = [datetime]'2026-10-25'; Status = 'Warn'; Label = 'Expiry' }
        )
        $calendar = $rows | New-ConsoleCalendar -Year 2026 -Month 10 -FirstDayOfWeek Sunday
        $marks = $calendar.Data.GetMarks([datetime]'2026-10-25')
        $marks.Count | Should -Be 2
        $marks[0].Label | Should -BeExactly 'Backup'
        $marks[0].Date.Kind.ToString() | Should -BeExactly 'Unspecified'
        $calendar.Data.GetStatus([datetime]'2026-10-25').ToString() | Should -BeExactly 'Warn'
        $calendar.Data.GetMarks([datetime]'2026-10-26').Count | Should -Be 0
    }

    It 'renders a calendar through Show-Console' {
        $text = New-ConsoleCalendar -Year 2026 -Month 10 -Culture en-US | Export-ConsoleFrame -Format Text -Width 40
        $text | Should -Match 'October 2026'
        $text | Should -Not -Match 'CalendarData'
    }

    It 'captures each emitted date status and label when a producer reuses its row' {
        $row = [pscustomobject] @{ Date = [datetime]'2026-10-01'; Status = 'Pass'; Label = 'First' }
        $calendar = 1..2 | ForEach-Object {
            $row.Date = [datetime]'2026-10-01' + [timespan]::FromDays($_ - 1)
            $row.Status = if ($_ -eq 1) { 'Pass' } else { 'Warn' }
            $row.Label = if ($_ -eq 1) { 'First' } else { 'Second' }
            $row
        } | New-ConsoleCalendar -Year 2026 -Month 10
        $first = $calendar.Data.GetMarks([datetime]'2026-10-01')
        $second = $calendar.Data.GetMarks([datetime]'2026-10-02')
        $first.Count | Should -Be 1
        $first[0].Label | Should -BeExactly 'First'
        $first[0].Status.ToString() | Should -BeExactly 'Pass'
        $second.Count | Should -Be 1
        $second[0].Label | Should -BeExactly 'Second'
        $second[0].Status.ToString() | Should -BeExactly 'Warn'
    }

    It 'creates a passive year view and rejects contradictory month input' {
        $calendar = New-ConsoleCalendar -Year 2026 -View Year
        $calendar.View.ToString() | Should -BeExactly 'Year'
        { New-ConsoleCalendar -Year 2026 } | Should -Throw '*requires*Month*'
        { New-ConsoleCalendar -Year 2026 -View Year -Month 10 } | Should -Throw '*without*Month*'
    }

    It 'shows the current month without Year or Month, like Ui.Calendar()' {
        (Get-Command New-ConsoleCalendar).Parameters['Year'].Attributes.Where({ $_ -is [System.Management.Automation.ParameterAttribute] }).Mandatory | Should -Not -Contain $true
        $calendar = New-ConsoleCalendar
        $calendar.Year | Should -Be ([datetime]::Today.Year)
        $calendar.Month | Should -Be ([datetime]::Today.Month)
        $fixed = New-ConsoleCalendar -Today ([datetime]'2025-02-14') -Culture en-US
        $fixed.Year | Should -Be 2025
        $fixed.Month | Should -Be 2
        ($fixed | Export-ConsoleFrame -Format Text -Width 40) | Should -Match 'February 2025'
        (New-ConsoleCalendar -View Year).Year | Should -Be ([datetime]::Today.Year)
    }
}
