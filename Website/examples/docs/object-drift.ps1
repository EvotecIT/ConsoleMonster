Import-Module ConsoleMonster

# Four domain controllers that should be configured alike.
$controllers = @(
    [pscustomobject]@{
        Name = 'dc01'; Site = 'HQ'; LdapPort = 389; GlobalCatalog = $true; DnsEnabled = $true
        Spn = 'ldap/dc01.ad.evotec.xyz'
        Security = [ordered]@{ Signing = 'Required'; ChannelBinding = 'Always' }
    }
    [pscustomobject]@{
        Name = 'dc02'; Site = 'HQ'; LdapPort = 636; GlobalCatalog = $true; DnsEnabled = $true
        Spn = 'ldap/dc02.ad.evotec.xyz'
        Security = [ordered]@{ Signing = 'Required'; ChannelBinding = 'Always' }
    }
    [pscustomobject]@{
        Name = 'dc03'; Site = 'Branch'; LdapPort = 389; GlobalCatalog = $false; DnsEnabled = $true
        Spn = 'ldap/dc03.ad.evotec.xyz'
        Security = [ordered]@{ Signing = 'Negotiate'; ChannelBinding = 'Always' }
    }
    [pscustomobject]@{
        Name = 'dc04'; Site = 'Branch'; LdapPort = 389; GlobalCatalog = $true
        Spn = 'ldap/dc04.ad.evotec.xyz'
        Security = [ordered]@{ Signing = 'Required'; ChannelBinding = 'Never' }
    }
)

# dc01 is the reference; each SPN is compared without its host name, so only real drift remains.
$controllers | Compare-ConsoleObject -View Baseline -NameProperty Name -Baseline dc01 -Ignore Name `
    -Normalize @{ Spn = { $_ -replace '/[^.]+', '/HOST' } } -Title 'Domain controller drift'
