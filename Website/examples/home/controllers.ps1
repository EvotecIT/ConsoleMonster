$controllers = @(
    #region more
    [pscustomobject] @{ Name = 'DC01.ad.adatum.com'; Site = 'Warsaw'; Status = 'Pass'; Latency = 12; LastSync = [timespan]::FromSeconds(42) }
    [pscustomobject] @{ Name = 'DC02.ad.adatum.com'; Site = 'Warsaw'; Status = 'Warn'; Latency = 140; LastSync = [timespan]::FromMinutes(42) }
    [pscustomobject] @{ Name = 'DC03.eu.ad.adatum.com'; Site = 'Dublin'; Status = 'Pass'; Latency = 31; LastSync = [timespan]::FromSeconds(64) }
    [pscustomobject] @{ Name = 'DC04.eu.ad.adatum.com'; Site = 'Dublin'; Status = 'Fail'; Latency = 0; LastSync = [timespan]::FromHours(4) }
    #endregion
)
$controllers | Format-ConsoleTable -Title '[bold]Domain controllers[/]' -Property @(
    @{ Name = 'Name'; Key = $true }
    @{ Name = 'Site'; Priority = 2 }
    @{ Name = 'Status'; Presenter = 'Status'; Priority = 9 }
    @{ Name = 'Latency'; Presenter = 'Number'; Unit = 'ms'; Priority = 5; WarnAbove = 100 }
    @{ Name = 'LastSync'; Header = 'Last sync'; Presenter = 'Duration'; Priority = 4 }
)
