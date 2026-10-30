# Review certificate renewal deadlines with an owner and a concrete next action.
Import-Module -Name ConsoleMonster

# Data: fixed dates and fictional certificates; no certificate store or private key is opened.
$certificates = @(
    [pscustomobject] @{ Name = 'api.example.test'; Expires = '2026-11-02'; Days = 9
        Owner = 'Platform'; Status = 'Warn' }
    [pscustomobject] @{ Name = 'vpn.example.test'; Expires = '2026-10-26'; Days = 2
        Owner = 'Network'; Status = 'Fail' }
    [pscustomobject] @{ Name = 'packages.example.test'; Expires = '2027-02-14'; Days = 113
        Owner = 'Build'; Status = 'Pass' }
    [pscustomobject] @{ Name = 'portal.example.test'; Expires = '2026-12-18'; Days = 55
        Owner = 'Web'; Status = 'Pass' }
)

# Layout: put urgent renewals beside the inventory, with thresholds visible.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[bold]Certificate renewals[/] [muted]· as of 2026-10-24[/]'
    '[fail]1 urgent[/]   [warn]1 due soon[/]   [pass]2 healthy[/]'
    New-ConsoleTable -InputObject $certificates -Property Name, Expires, Days, Owner, Status -Expand
    New-ConsolePanel -Title 'Renewal policy' -Content {
        New-ConsoleStatusList -Content {
            [pscustomobject]@{ Label = 'vpn.example.test'; Status = 'Fail'
                Detail = 'Network · renew before Monday' }
            [pscustomobject]@{ Label = 'api.example.test'; Status = 'Warn'
                Detail = 'Platform · schedule renewal this week' }
        }
        '[muted]Urgent: 7 days or fewer · due soon: 30 days or fewer[/]'
    }
}

# Behaviour: print once; connect your own inventory reader before rendering.
$report | Show-Console
