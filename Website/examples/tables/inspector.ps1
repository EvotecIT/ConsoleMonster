Import-Module ConsoleMonster

$zone = [ordered] @{
    Name = 'evotec.xyz'
    Serial = 2351234567L
    Signed = $false
    Soa = [ordered] @{
        Primary = 'dana.ns.cloudflare.com'
        Refresh = [timespan]::FromSeconds(10000)
        Expire = [timespan]::FromSeconds(604800)
    }
    NameServers = @(
        [pscustomobject] @{ Host = 'dana.ns.cloudflare.com'; Address = '108.162.192.103'
            LatencyMs = 12 }
        [pscustomobject] @{ Host = 'kirk.ns.cloudflare.com'; Address = '172.64.33.149'
            LatencyMs = 14 }
    )
    Tags = @('production', 'cloudflare', 'doh')
}
$zone | Show-ConsoleObject -Depth 3 -Mode None -Title 'evotec.xyz zone'
