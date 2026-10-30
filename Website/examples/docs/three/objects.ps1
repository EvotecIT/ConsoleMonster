Import-Module ConsoleMonster

$servers = @(
    [pscustomobject] @{ Name = 'web01'; Role = 'IIS'; Ram = 8GB; Status = 'Pass' }
    [pscustomobject] @{ Name = 'app01'; Role = 'App'; Ram = 16GB; Status = 'Warn' }
    [pscustomobject] @{ Name = 'db01'; Role = 'SQL'; Ram = 64GB; Status = 'Fail' }
)
$load = 12, 18, 31, 64, 47, 38, 55, 70, 72, 41
New-ConsoleRows -Gap 1 -Content {
    New-ConsoleColumns -Gap 1 -Content {
        New-ConsolePanel -Title 'Servers' -Content {
            $servers | New-ConsoleTable  -ShowTotals -Property @(
                @{ Name = 'Name'; Key = $true }
                'Role'
                @{ Name = 'Ram'; Presenter = 'Bytes' }
                @{ Name = 'Status'; Presenter = 'Status' }
            )
        }
        New-ConsolePanel -Title 'Summary' -Content {
            New-ConsoleRows -Content {
                New-ConsoleKeyValue -Content {
                    [pscustomobject] @{ Label = 'Servers'; Value = 3 }
                    [pscustomobject] @{ Label = 'Failed'; Value = 1 }
                }
                New-ConsoleChart -Type Sparkline -Title 'CPU' -InputObject $load
            }
        }
    }
    New-ConsoleText '[muted]Updated 10:02[/]'
} | Show-Console
