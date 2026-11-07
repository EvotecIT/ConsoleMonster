Import-Module ConsoleMonster

Show-ConsoleApp -Content {
    New-ConsoleSplitter -Key Panes -Ratio 0.3 -First {
        New-ConsolePanel -Title Servers -Content {
            New-ConsoleSelect -Key Server -PageSize 3 -Choices @(
                [pscustomobject] @{ Value = 'WEB01'; Label = 'WEB01  Web' }
                [pscustomobject] @{ Value = 'SQL01'; Label = 'SQL01  Database' }
                [pscustomobject] @{ Value = 'FS01'; Label = 'FS01   File' }
            )
        }
    } -Second {
        New-ConsolePanel -Title Notes -Content {
            'WEB01 serves the public site.'
            'SQL01 holds the orders database.'
            'FS01 keeps the shared folders.'
        }
    }
}
