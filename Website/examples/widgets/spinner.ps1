New-ConsoleRows {
    New-ConsoleSpinner 'Resolving DNS records' -Detail 'zone 4 of 9'
    New-ConsoleSpinner 'Schema snapshot' -Detail 'cached' -Status Pass
    New-ConsoleSpinner 'Certificate chain' -Detail 'expired 3 days ago' -Status Fail
} | Write-ConsoleWidget
