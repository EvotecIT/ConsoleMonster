Import-Module ConsoleMonster

$services = @(
    [pscustomobject] @{ Name = 'Spooler'; Status = 'Pass'; StartType = 'Automatic'; Memory = 14221312 }
    #region more
    [pscustomobject] @{ Name = 'MSSQLSERVER'; Status = 'Pass'; StartType = 'Automatic'; Memory = 812466176 }
    [pscustomobject] @{ Name = 'SQLSERVERAGENT'; Status = 'Warn'; StartType = 'Manual'; Memory = 42004480 }
    [pscustomobject] @{ Name = 'W32Time'; Status = 'Pass'; StartType = 'Automatic'; Memory = 3276800 }
    [pscustomobject] @{ Name = 'WinRM'; Status = 'Pass'; StartType = 'Automatic'; Memory = 9830400 }
    [pscustomobject] @{ Name = 'DFSR'; Status = 'Fail'; StartType = 'Automatic'; Memory = 0 }
    [pscustomobject] @{ Name = 'LanmanServer'; Status = 'Pass'; StartType = 'Automatic'; Memory = 6553600 }
    #endregion
)

$picked = $services | Show-ConsoleGrid -Title 'Services on FS01' -Property @(
    @{ Name = 'Name'; Key = $true }
    @{ Name = 'Status'; Presenter = 'Status' }
    @{ Name = 'StartType'; Header = 'Start type' }
    @{ Name = 'Memory'; Presenter = 'Bytes' }
)
Write-ConsoleMarkup "Picked [bold]$(@($picked).Count)[/] services"
