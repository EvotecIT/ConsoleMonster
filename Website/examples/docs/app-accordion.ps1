Import-Module ConsoleMonster

Show-ConsoleApp -Content {
    New-ConsoleAccordion -Content {
        New-ConsoleCollapsible -Key Synopsis -Expanded -Content {
            'Lists servers and their roles.'
        }
        New-ConsoleCollapsible -Key Parameters -Summary '(2)' -Content {
            '-Name    server name or wildcard'
            '-Role    web, db or cache'
        }
        New-ConsoleCollapsible -Key Examples -Summary '(1)' -Content {
            'Get-Inventory -Role web'
        }
    }
}