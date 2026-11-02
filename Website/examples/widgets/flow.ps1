Import-Module ConsoleMonster

# Steps joined by labelled connections; a cut connection shows where the path breaks.
New-ConsoleFlow -Content {
    [pscustomobject] @{ Kind = 'Node'; Key = 'client'; Label = 'Client'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'proxy'; Label = 'Proxy'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'api'; Label = 'API'; Status = 'Warn' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'db'; Label = 'Database'; Status = 'Fail' }
    [pscustomobject] @{ Kind = 'Edge'; From = 'client'; To = 'proxy'; Label = ':443' }
    [pscustomobject] @{ Kind = 'Edge'; From = 'proxy'; To = 'api'; Label = 'HTTP' }
    [pscustomobject] @{ Kind = 'Edge'; From = 'api'; To = 'db'; Label = 'timeout'; Status = 'Fail'
        Cut = $true }
    [pscustomobject] @{ Kind = 'Branch'; From = 'db'; Label = 'listener stopped'; Status = 'Fail' }
} | Show-Console
