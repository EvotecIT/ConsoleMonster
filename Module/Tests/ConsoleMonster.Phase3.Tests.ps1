Describe 'Composed full-screen controls' {
    BeforeAll {
        if (-not (Get-Module ConsoleMonster)) { Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') }
        $script:Core = [ConsoleMonster.Terminal].Assembly
    }

    It 'keeps two original-object selections separate through the public constructors' {
        $listing = ($script:Core.GetType('ConsoleMonster.Interactive.Grid'))::new('Files')
        $null = $listing.AddColumn('Name')
        $first = [pscustomobject]@{ Name = 'first.log' }
        $second = [pscustomobject]@{ Name = 'second.json' }
        $null = $listing.AddRow($first, @('first.log'))
        $null = $listing.AddRow($second, @('second.json'))
        $left = New-ConsoleDataGrid -Key Left -Grid $listing
        $right = New-ConsoleDataGrid -Key Right -Grid $listing -NoQuickSearch
        try {
            $left.MarkPattern('*.log')
            $right.Move(1)
            [object]::ReferenceEquals($left.Selection[0], $first) | Should -BeTrue
            [object]::ReferenceEquals($right.Selection[0], $second) | Should -BeTrue
            $right.MarkCount | Should -Be 0
            $right.QuickSearch | Should -BeFalse
        } finally { $left.Dispose(); $right.Dispose() }
    }

    It 'builds tab pages and modal fields without starting an input loop' {
        $pages = @(
            New-ConsoleTab -Title First -Content { New-ConsoleTextInput -Key Filter -Value alpha }
            New-ConsoleTab -Title Second -Content { New-ConsoleToggle -Key Enabled -Value $true }
        )
        $tabs = $pages | New-ConsoleTabs -SelectedIndex 1
        $tabs.SelectedIndex | Should -Be 1
        $tabs.Children.Count | Should -Be 2
        $dialog = New-ConsoleDialog -Title Options -InitialFocus Destination -Content { New-ConsoleTextInput -Key Destination -Value backup }
        $dialog.DefaultAccept | Should -BeFalse
        $dialog.InitialFocus | Should -Be Destination
        $dialog.Content.Key | Should -Be Destination
    }

    It 'returns a civil inclusive date range from an unattended explicit default' {
        $date = [datetime]::new(2026, 10, 8, 17, 30, 0, [DateTimeKind]::Utc)
        $answer = Read-ConsoleDate -Title Window -Range -Default $date -Mode None
        $answer | Should -BeOfType ($script:Core.GetType('ConsoleMonster.Dates.DateRange'))
        $answer.Start | Should -Be ([datetime]::new(2026, 10, 8))
        $answer.End | Should -Be $answer.Start
        $answer.Start.Kind | Should -Be Unspecified
    }
}
