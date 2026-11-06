Import-Module ConsoleMonster

$before = @(
    [pscustomobject] @{ Name = 'web01'; Port = 80 }
    [pscustomobject] @{ Name = 'web02'; Port = 443 }
    [pscustomobject] @{ Name = 'sql01'; Port = 1433 }
)
$after = @(
    [pscustomobject] @{ Name = 'web01'; Port = 443 }
    [pscustomobject] @{ Name = 'web02'; Port = 443 }
    [pscustomobject] @{ Name = 'dc01'; Port = 389 }
)

# PowerShell's own output
Compare-Object $before $after -Property Name, Port

Enable-ConsoleFormatting

# The same comparison now
Compare-Object $before $after -Property Name, Port -IncludeEqual
