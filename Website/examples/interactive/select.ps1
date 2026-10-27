Import-Module ConsoleMonster

$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Role = 'File' }
    #region more
    [pscustomobject] @{ Name = 'FS02'; Site = 'Warsaw'; Role = 'File' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Warsaw'; Role = 'Web' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Warsaw'; Role = 'Database' }
    [pscustomobject] @{ Name = 'FS11'; Site = 'Berlin'; Role = 'File' }
    [pscustomobject] @{ Name = 'WEB11'; Site = 'Berlin'; Role = 'Web' }
    [pscustomobject] @{ Name = 'PRN11'; Site = 'Berlin'; Role = 'Print'
        Offline = 'offline since 09:12' }
    [pscustomobject] @{ Name = 'BKP21'; Site = 'Dublin'; Role = 'Backup' }
    [pscustomobject] @{ Name = 'SQL21'; Site = 'Dublin'; Role = 'Database' }
    [pscustomobject] @{ Name = 'LAB01'; Site = 'Lab'; Role = 'Test' }
    [pscustomobject] @{ Name = 'LAB02'; Site = 'Lab'; Role = 'Test'
        Offline = 'in maintenance until Friday' }
    #endregion
)

$selectParameters = @{
    Choices = $servers
    Display = 'Name'
    GroupBy = 'Site'
    Badge = 'Role'
    Description = 'Type to filter; offline servers show why they cannot be picked.'
}
$server = Read-ConsoleSelect 'Target server' @selectParameters -DisabledReason {
    $_.Offline
}
Write-ConsoleText -Markup "Connecting to [bold]$($server.Name)[/]"
