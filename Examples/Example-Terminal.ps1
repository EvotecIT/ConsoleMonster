Import-Module .\ConsoleMonster.psd1 -Force

New-Terminal {
    New-TerminalWindow -Title 'Test' {
        New-TerminalText -Text 'Special Text' -Width 20
    }
}