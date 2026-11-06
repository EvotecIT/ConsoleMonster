Import-Module ConsoleMonster

Show-ConsoleApp -Content {
    New-ConsoleSplitter -Key Panes -Ratio 0.3 -First {
        New-ConsolePanel -Title Servers -Content {
            'WEB01  Web'
            'SQL01  Database'
            'FS01   File'
        }
    } -Second {
        New-ConsolePanel -Title Notes -Content {
            'WEB01 serves the public site.'
            'SQL01 holds the orders database.'
            'FS01 keeps the shared folders.'
        }
    }
}