Import-Module ConsoleMonster

#region Using the object directly
# A custom presenter fixes the bar scale at 100 and adds a percent unit.
$dataBarType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.DataBarPresenter',
    $true)
$numberType = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Presenters.NumberPresenter',
    $true)
$scorePresenter = $dataBarType::new(100, $numberType::new('%'))
#endregion


$rows = @(
    [pscustomobject] @{
        Group = 'Domains'
        Target = 'ad.adatum.com'
        Result = 'Fail'
        Passed = 9
        Failed = 2
        Warnings = 5
        Duration = [timespan]::FromSeconds(24)
        Score = 56
    }
    [pscustomobject] @{
        Group = 'Domains'
        Target = 'eu.ad.adatum.com'
        Result = 'Fail'
        Passed = 13
        Failed = 1
        Warnings = 1
        Duration = [timespan]::FromSeconds(16)
        Score = 87
    }
    [pscustomobject] @{
        Group = 'Domain controllers'
        Target = 'DC01.ad.adatum.com'
        Result = 'Warn'
        Passed = 26
        Failed = 0
        Warnings = 3
        Duration = [timespan]::FromSeconds(17)
        Score = 90
    }
    [pscustomobject] @{
        Group = 'Domain controllers'
        Target = 'DC02.ad.adatum.com'
        Result = 'Warn'
        Passed = 23
        Failed = 0
        Warnings = 6
        Duration = [timespan]::FromSeconds(17)
        Score = 79
    }
    [pscustomobject] @{
        Group = 'Domain controllers'
        Target = 'DC03.ad.adatum.com'
        Result = 'Fail'
        Passed = 23
        Failed = 3
        Warnings = 4
        Duration = [timespan]::FromSeconds(19)
        Score = 77
    }
    [pscustomobject] @{
        Group = 'Domain controllers'
        Target = 'DC07.eu.ad.adatum.com'
        Result = 'Fail'
        Passed = 21
        Failed = 4
        Warnings = 4
        Duration = [timespan]::FromSeconds(16)
        Score = 72
    }
    [pscustomobject] @{
        Group = 'Domain controllers'
        Target = 'DC09.eu.ad.adatum.com'
        Result = 'Pass'
        Passed = 30
        Failed = 0
        Warnings = 0
        Duration = [timespan]::FromSeconds(15)
        Score = 100
    }
)

$tableParameters = @{
    Title = '[bold]Nightly checks[/] [muted]· run summary[/]'
    GroupBy = 'Group'
    ShowSubtotals = $true
    ShowTotals = $true
    NoSort = $true
}
$rows |
    Show-ConsoleTable @tableParameters -Property @(
        @{ Name = 'Target'; Key = $true; Overflow = 'TruncateMiddle'; MinWidth = 12 }
        @{ Name = 'Result'; Priority = 10; Presenter = 'Status' }
        @{ Name = 'Passed'; Priority = 8; Presenter = 'Number'; Aggregate = 'Sum' }
        @{ Name = 'Failed'; Priority = 9; Presenter = 'Number'; Aggregate = 'Sum'; FailAbove = 0 }
        @{ Name = 'Warnings'; Priority = 6; Presenter = 'Number'; Aggregate = 'Sum'; WarnAbove = 0 }
        @{ Name = 'Duration'; Priority = 4; Presenter = 'Duration'; Aggregate = 'Sum' }
        @{ Name = 'Score'; Priority = 2; Presenter = $scorePresenter; Aggregate = 'Average' }
    )
