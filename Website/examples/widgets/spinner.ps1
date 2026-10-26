Import-Module ConsoleMonster

New-ConsoleRows -Content {
    New-ConsoleProgress -Label 'Resolving DNS records' -Detail 'zone 4 of 9' -Indeterminate -WidgetId 'spinner'
    New-ConsoleProgress -Label 'Schema snapshot' -Detail 'cached' -Status Pass -Indeterminate -WidgetId 'spinner'
    New-ConsoleProgress -Label 'Certificate chain' -Detail 'expired 3 days ago' -Status Fail -Indeterminate -WidgetId 'spinner'
} | Show-Console
