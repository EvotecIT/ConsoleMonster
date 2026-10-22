Describe 'ConsoleMonster host overview' {
    BeforeAll {
        $modulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        Import-Module $modulePath -Force -ErrorAction Stop
    }

    It 'exports the compiled command through the source module manifest' {
        $command = Get-Command Show-ConsoleOverview -Module ConsoleMonster -ErrorAction Stop
        $command.CommandType | Should -Be 'Cmdlet'
        $command.Parameters.Keys | Should -Contain 'AsObject'
        $command.Parameters.Keys | Should -Contain 'Section'
    }

    It 'returns native resource observations as one snapshot' {
        $snapshot = Show-ConsoleOverview -Section Resources -AsObject
        $snapshot.GetType().FullName | Should -Be 'ConsoleMonster.Overview.OverviewSnapshot'
        $snapshot.Sections.Count | Should -Be 1
        $snapshot.Sections[0].Name | Should -Be 'Resources'
        $snapshot.Sections[0].IsPending | Should -BeFalse
        @($snapshot.Sections[0].Items | Where-Object Label -eq CPU).Count | Should -Be 1
        @($snapshot.Sections[0].Items | Where-Object Label -eq Memory).Count | Should -Be 1
    }

    It 'captures plain text within the requested narrow width' {
        $text = Show-ConsoleOverview -Section Resources,Disks,Network -Width 60 -AsString
        $text.GetType().FullName | Should -Be 'System.String'
        $text.IndexOf([char]27) | Should -Be -1
        foreach ($line in $text.Split([char]10)) {
            [ConsoleMonster.Text.DisplayWidth]::Measure($line) | Should -BeLessOrEqual 60
        }
    }

    It 'rejects unknown section names' {
        { Show-ConsoleOverview -Section NotRegistered -AsObject -ErrorAction Stop } | Should -Throw
    }

    It 'rejects an out of range collection budget' {
        { Show-ConsoleOverview -BudgetMilliseconds 901 -AsObject -ErrorAction Stop } | Should -Throw
    }

    It 'keeps snapshot and text output parameter sets separate' {
        { Show-ConsoleOverview -AsObject -AsString -ErrorAction Stop } | Should -Throw
    }

    It 'loads generated help for every public parameter and the examples' {
        $help = Get-Help Show-ConsoleOverview -Full
        [string]$help.Synopsis | Should -Match 'read-only summary'
        @($help.examples.example).Count | Should -Be 3
        foreach ($name in 'Section', 'AsObject', 'Width', 'AsString', 'BudgetMilliseconds') {
            $parameter = @($help.parameters.parameter | Where-Object Name -eq $name)
            $parameter.Count | Should -Be 1
            [string]($parameter[0].description.Text -join ' ') | Should -Not -BeNullOrEmpty
        }
    }
}
