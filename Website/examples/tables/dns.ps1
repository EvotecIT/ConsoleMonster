Import-Module ConsoleMonster


$root = '/var/cache/dns/evotec.xyz/'

$rows = @(
    [pscustomobject] @{
        Name = 'evotec.xyz'
        Type = 'A'
        TTL = 300
        Data = '104.21.48.12'
        Latency = 12
        Cache = ($root + 'A.json')
    }
    [pscustomobject] @{
        Name = 'evotec.xyz'
        Type = 'AAAA'
        TTL = 300
        Data = '2606:4700:3036::6815:300c'
        Latency = 14
        Cache = ($root + 'AAAA.json')
    }
    [pscustomobject] @{
        Name = 'evotec.xyz'
        Type = 'MX'
        TTL = 3600
        Data = '10 evotec-xyz.mail.protection.outlook.com'
        Latency = 18
        Cache = ($root + 'MX.json')
    }
    [pscustomobject] @{
        Name = 'evotec.xyz'
        Type = 'TXT'
        TTL = 3600
        Data = 'v=spf1 include:spf.protection.outlook.com include:_spf.google.com include:sendgrid.net include:mailgun.org ip4:51.38.120.9 ip4:51.38.120.10 -all'
        Latency = 21
        Cache = ($root + 'TXT.json')
    }
    [pscustomobject] @{
        Name = '_dmarc.evotec.xyz'
        Type = 'TXT'
        TTL = 3600
        Data = 'v=DMARC1; p=reject; rua=mailto:dmarc@evotec.xyz; ruf=mailto:forensic@evotec.xyz; fo=1'
        Latency = 133
        Cache = ($root + 'dmarc.TXT.json')
    }
    [pscustomobject] @{
        Name = 'selector1._domainkey.evotec.xyz'
        Type = 'CNAME'
        TTL = 3600
        Data = 'selector1-evotec-xyz._domainkey.evotecpl.onmicrosoft.com'
        Latency = 16
        Cache = ($root + 'selector1.domainkey.CNAME.json')
    }
    [pscustomobject] @{
        Name = 'evotec.xyz'
        Type = 'CAA'
        TTL = 3600
        Data = '0 issue "letsencrypt.org"'
        Latency = 15
        Cache = ($root + 'CAA.json')
    }
)

$rows |
    Show-ConsoleTable -Title '[bold]evotec.xyz[/] [muted]· DNS records via Cloudflare DoH[/]' -Property @(
        @{ Name = 'Name'; Key = $true; Overflow = 'TruncateMiddle'; MinWidth = 10 }
        @{ Name = 'Type'; Priority = 9; Overflow = 'Never' }
        @{ Name = 'TTL'; Priority = 2; Presenter = 'Duration' }
        @{ Name = 'Data'; Priority = 10; Overflow = 'Wrap'; MaxLines = 3; Flexible = $true
            MinWidth = 28 }
        @{ Name = 'Latency'; Priority = 3; Presenter = 'Number'; Unit = 'ms'; WarnAbove = 100 }
        @{ Name = 'Cache'; Header = 'Cache file'; Priority = 1; Overflow = 'TruncateMiddle'
            MinWidth = 20; MaxWidth = 36 }
    )
