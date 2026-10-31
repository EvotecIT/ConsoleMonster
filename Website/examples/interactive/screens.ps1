Import-Module ConsoleMonster

$servers = @(
    [pscustomobject] @{ Name = 'WEB01'; Role = 'Web'; Site = 'Warsaw' }
    [pscustomobject] @{ Name = 'SQL01'; Role = 'Database'; Site = 'Warsaw' }
    [pscustomobject] @{ Name = 'FS11'; Role = 'File'; Site = 'Berlin' }
)

Show-ConsoleApp -Title Inventory -Content {
    New-ConsoleScreen -Key list -Title Servers -KeyBinding ([ordered]@{
        '1-3' = 'open', {
            param($Number, $State)
            $State.Server = $Number
            $State.Screen = 'server'
        }
    }) -Content {
        '[muted]Type a number to open a server.[/]'
        for ($i = 0; $i -lt $servers.Count; $i++) {
            $server = $servers[$i]
            "[bold]$($i + 1)[/]  $($server.Name)  [muted]$($server.Role), $($server.Site)[/]"
        }
    }
    New-ConsoleScreen -Key server -Title { $servers[$State.Server - 1].Name } -Content {
        New-ConsoleBinding -Content {
            param($State)
            $server = $servers[$State.Server - 1]
            "[bold]$($server.Name)[/] is the $($server.Role) server in $($server.Site)."
        }
    }
}