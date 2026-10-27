Import-Module ConsoleMonster

$rate = 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57
New-ConsolePanel -Title '[bold]Nightly checks[/]' -HeaderRight 'lab01.contoso.com' -Content {
    New-ConsoleRows -Content {
        New-ConsolePhases -Content {
            [pscustomobject] @{ Key = 'Discover'; Label = 'Discover'; Status = 'Pass'
                Detail = '2.1s' }
            [pscustomobject] @{ Key = 'Collect'; Label = 'Collect'; Status = 'Pass'
                Detail = '14s' }
            [pscustomobject] @{ Key = 'Evaluate'; Label = 'Evaluate'; Status = 'Running'
                Detail = '64%' }
        }
        New-ConsoleChart -Type Stacked -Content {
            [pscustomobject] @{ Label = 'Passed'; Value = 412; Role = 'Pass' }
            [pscustomobject] @{ Label = 'Warnings'; Value = 51; Role = 'Warn' }
            [pscustomobject] @{ Label = 'Failed'; Value = 23; Role = 'Fail' }
            [pscustomobject] @{ Label = 'Skipped'; Value = 14; Role = 'Skip' }
        }
        New-ConsoleChart -Type Sparkline -Title 'rules/s' -Unit '/s' -InputObject $rate
    }
} | Show-Console
