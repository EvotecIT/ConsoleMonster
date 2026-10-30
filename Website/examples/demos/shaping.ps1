Import-Module ConsoleMonster

# Arrays in cells and groups: the built-in Format-Table beside Show-ConsoleTable, on the same objects.
$servers = @(
    [pscustomobject] @{ Name = 'DC01'; Site = 'Warsaw'; Roles = 'DNS', 'GC', 'PDC'
        LastSync = (New-TimeSpan -Seconds 42); Status = 'Pass' }
    [pscustomobject] @{ Name = 'DC02'; Site = 'Warsaw'; Roles = 'DNS', 'GC'
        LastSync = (New-TimeSpan -Minutes 42); Status = 'Warn' }
    [pscustomobject] @{ Name = 'DC03'; Site = 'Dublin'
        Roles = 'DNS', 'GC', 'RID', 'Infrastructure', 'Schema', 'Naming'
        LastSync = (New-TimeSpan -Seconds 64); Status = 'Pass' }
    [pscustomobject] @{ Name = 'DC04'; Site = 'Dublin'; Roles = @()
        LastSync = (New-TimeSpan -Hours 4); Status = 'Fail' }
)
New-ConsoleRule -Title 'Format-Table'  | Show-Console
$servers | Format-Table | Out-Host
New-ConsoleRule -Title 'Show-ConsoleTable -GroupBy Site'  | Show-Console
$servers | Show-ConsoleTable -GroupBy Site
