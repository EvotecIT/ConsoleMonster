Import-Module ConsoleMonster

$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
$servers | Show-ConsoleTable -Property Name, Site, Pending, Status -Title 'Patch inventory'
