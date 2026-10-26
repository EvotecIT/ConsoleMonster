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
        $text = New-ConsoleCalendar -Year 2026 -Month 10 -Culture en-US | Show-Console -AsString -Width 40
        $text | Should -Match 'October 2026'
        $text | Should -Not -Match 'CalendarData'
    }

    It 'creates a passive year view and rejects contradictory month input' {
        $calendar = New-ConsoleCalendar -Year 2026 -View Year
        $calendar.View.ToString() | Should -BeExactly 'Year'
        { New-ConsoleCalendar -Year 2026 } | Should -Throw '*requires*Month*'
        { New-ConsoleCalendar -Year 2026 -View Year -Month 10 } | Should -Throw '*without*Month*'
    }
}
