Import-Module ConsoleMonster

# DNS and mail-security query results, printed once (no live view).
# Works the same in Windows Terminal, conhost, redirected output and CONSOLEMONSTER_MODE=plain.

New-ConsoleRule -Title '[bold]DNS query results[/] [muted]· evotec.xyz[/]'  | Show-Console

[ordered] @{
    Domain       = 'evotec.xyz'
    Resolver     = 'Cloudflare DNS over HTTPS (1.1.1.1)'
    'Query time' = '38 ms for 7 record types'
    Records      = 10
    DNSSEC       = 'not signed'
} | ForEach-Object { New-ConsoleKeyValue -InputObject $_ } | Show-Console

$records = @(
    [pscustomobject] @{ Name = 'evotec.xyz'; Type = 'A'; TTL = 300; Data = '104.21.48.12' }
    [pscustomobject] @{ Name = 'evotec.xyz'; Type = 'AAAA'; TTL = 300; Data = '2606:4700:3036::6815:300c' }
    [pscustomobject] @{ Name = 'evotec.xyz'; Type = 'MX'; TTL = 3600; Data = '10 evotec-xyz.mail.protection.outlook.com' }
    [pscustomobject] @{ Name = 'evotec.xyz'; Type = 'NS'; TTL = 86400; Data = 'dana.ns.cloudflare.com' }
    [pscustomobject] @{ Name = 'evotec.xyz'; Type = 'TXT'; TTL = 3600; Data = 'v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net ip4:51.38.120.9 -all' }
    [pscustomobject] @{ Name = '_dmarc.evotec.xyz'; Type = 'TXT'; TTL = 3600; Data = 'v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; fo=1' }
)
$records | Show-ConsoleTable -Expand -NoHeaderSeparator

$delegation = New-ConsoleTree -Title '. (root)' -InputObject ([ordered] @{
        'xyz.' = [ordered] @{
            'x.nic.xyz'   = '194.169.218.42'
            'evotec.xyz.' = [ordered] @{
                'dana.ns.cloudflare.com' = '108.162.192.103'
                'kirk.ns.cloudflare.com' = '172.64.33.149'
            }
        }
    })

$checks = New-ConsoleStatusList -InputObject ([ordered] @{
        'SPF record present and valid' = 'pass'
        'DMARC policy enforced'        = 'pass'
        'DKIM selector1 / selector2'   = 'pass'
        'MTA-STS policy'               = 'warn'
        'DNSSEC chain of trust'        = 'fail'
        'BIMI record'                  = 'skip'
    })

$latency = New-ConsoleChart -WidgetId latency -Unit ' ms' -InputObject ([ordered] @{ Cloudflare = 12; Google = 18; Quad9 = 24; OpenDNS = 31 }) -Type Bar

New-ConsoleColumns -Content {
    New-ConsolePanel -Title 'Delegation' -Content $delegation
    New-ConsolePanel -Title 'Mail security' -HeaderRight 'evotec.xyz' -Content {
        $checks
        New-ConsoleRule
        $latency
    }
} | Show-Console
