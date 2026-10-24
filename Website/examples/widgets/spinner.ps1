Import-Module ConsoleMonster

New-ConsoleRows -Content {
    New-ConsoleSpinner -Label 'Resolving DNS records' -Detail 'zone 4 of 9'
    New-ConsoleSpinner -Label 'Schema snapshot' -Detail 'cached' -Status Pass
    New-ConsoleSpinner -Label 'Certificate chain' -Detail 'expired 3 days ago' -Status Fail
} | Show-Console
