Import-Module ConsoleMonster

New-ConsolePhases -Content {
    [pscustomobject]@{ Label = 'Discover'; Status = 'Pass'; Detail = '2.1s' }
    [pscustomobject]@{ Label = 'Collect'; Status = 'Pass'; Detail = '14s' }
    [pscustomobject]@{ Label = 'Evaluate'; Status = 'Running'; Detail = '64%' }
    [pscustomobject]@{ Label = 'Report' }
} | Show-Console
