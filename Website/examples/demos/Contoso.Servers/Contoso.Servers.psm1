function Get-ContosoServer {
    <#
    .SYNOPSIS
    Returns the servers in the Contoso inventory as Contoso.Server objects.
    #>
    [CmdletBinding()]
    param()
    $servers = @(
        @{ Name = 'web01'; Site = 'Warsaw'; State = 'Running'; Hours = 412 }
        @{ Name = 'web02'; Site = 'Warsaw'; State = 'Running'; Hours = 9 }
        @{ Name = 'sql01'; Site = 'Warsaw'; State = 'Degraded'; Hours = 1210 }
        @{ Name = 'app01'; Site = 'Krakow'; State = 'Running'; Hours = 77 }
        @{ Name = 'app02'; Site = 'Krakow'; State = 'Stopped'; Hours = 0 }
    )
    foreach ($server in $servers) {
        [pscustomobject] @{
            PSTypeName = 'Contoso.Server'
            Name       = $server.Name
            Site       = $server.Site
            State      = $server.State
            Uptime     = New-TimeSpan -Hours $server.Hours
        }
    }
}