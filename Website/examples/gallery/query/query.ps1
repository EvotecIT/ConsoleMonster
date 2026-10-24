# DNS and mail security: summary, wrapped records, delegation and resolver latency.
[CmdletBinding()]
param(
    [switch] $StaticSnapshot,
    [ValidateRange(0, 0)] [int] $Step = 0
)
Import-Module ConsoleMonster

# Data: reserved synthetic names and documentation addresses. No DNS requests run.
$domain = 'example.test'
$records = @(
    [pscustomobject] @{ Name = $domain; Type = 'A'; TTL = 300; Data = '192.0.2.12' }
    [pscustomobject] @{ Name = $domain; Type = 'A'; TTL = 300; Data = '198.51.100.94' }
    [pscustomobject] @{ Name = $domain; Type = 'AAAA'; TTL = 300; Data = '2001:db8::12' }
    [pscustomobject] @{ Name = $domain; Type = 'MX'; TTL = 3600; Data = '10 mail.example.test' }
    [pscustomobject] @{ Name = $domain; Type = 'NS'; TTL = 86400; Data = 'ns1.example.test' }
    [pscustomobject] @{ Name = $domain; Type = 'NS'; TTL = 86400; Data = 'ns2.example.test' }
    [pscustomobject] @{ Name = $domain; Type = 'TXT'; TTL = 3600; Data = 'v=spf1 include:spf.example.test include:relay.example.test ip4:203.0.113.9 -all' }
    [pscustomobject] @{ Name = "_dmarc.$domain"; Type = 'TXT'; TTL = 3600; Data = 'v=DMARC1; p=reject; rua=mailto:dmarc@example.test; ruf=mailto:forensic@example.test; fo=1' }
    [pscustomobject] @{ Name = $domain; Type = 'CAA'; TTL = 3600; Data = '0 issue "ca.example.test"' }
    [pscustomobject] @{ Name = $domain; Type = 'SOA'; TTL = 3600; Data = 'ns1.example.test. hostmaster.example.test. 2026102401 10000 2400 604800 3600' }
)
$checks = @(
    ,@('SPF record present and valid', 'Pass', '9 lookups')
    ,@('DMARC policy enforced', 'Pass', 'p=reject')
    ,@('DKIM selector1 / selector2', 'Pass', '2048-bit')
    ,@('MTA-STS policy', 'Warn', 'not published')
    ,@('TLS-RPT reporting', 'Warn', 'not published')
    ,@('DNSSEC chain of trust', 'Fail', 'zone not signed')
    ,@('BIMI record', 'Skip', 'optional')
)
$resolvers = [ordered] @{ 'Edge west' = 12; 'Edge east' = 18; 'Cache north' = 24; 'Cache south' = 31; 'Local resolver' = 47 }

# Layout: keep the dense report readable through wrapping and two balanced panels.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule '[bold]DNS query results[/] [muted]· example.test[/]'
    New-ConsolePadding -Padding 1, 0 -Content {
        New-ConsoleKeyValue -Content {
            New-ConsoleKeyValueItem Domain '[bold]example.test[/]' -Markup
            New-ConsoleKeyValueItem Resolver 'Edge west DNS over HTTPS [muted](192.0.2.53)[/]' -Markup
            New-ConsoleKeyValueItem 'Query time' '38 ms [muted]for 7 record types[/]' -Markup
            New-ConsoleKeyValueItem Records '10'
            New-ConsoleKeyValueItem DNSSEC 'not signed' -Status Warn
        }
    }
    $records | New-ConsoleTable -Property Name, Type, TTL, Data -Expand -NoHeaderSeparator
    New-ConsoleColumns -Content {
        New-ConsolePanel -Title Delegation -Content {
            New-ConsoleTree '[bold].[/] [muted]root[/]' -Content {
                New-ConsoleTreeNode '[bold]test.[/]' -Detail 'reserved namespace' -Content {
                    New-ConsoleTreeNode 'ns.test' -Detail '192.0.2.42'
                    New-ConsoleTreeNode '[bold]example.test.[/]' -Detail '2 name servers' -Content {
                        New-ConsoleTreeNode 'ns1.example.test' -Status Pass -Detail '198.51.100.103 · 12 ms'
                        New-ConsoleTreeNode 'ns2.example.test' -Status Pass -Detail '203.0.113.149 · 14 ms'
                        New-ConsoleTreeNode mail -Detail 'MX target' -Content {
                            New-ConsoleTreeNode 'mail.example.test' -Status Pass -Detail '192.0.2.17'
                        }
                    }
                }
            }
        }
        New-ConsolePanel -Title 'Mail security' -HeaderRight $domain -Content {
            New-ConsoleRows -Gap 0 -Content {
                New-ConsoleStatusList -Content {
                    foreach ($check in $checks) { New-ConsoleStatusItem $check[0] -Status $check[1] -Detail $check[2] }
                }
                New-ConsoleRule
                New-ConsoleBarChart -WidgetId latency -Unit ' ms' -InputObject $resolvers
            }
        }
    }
}

# Behaviour: a print-once report works interactively and in redirected snapshots.
$report | Show-Console
