Import-Module ConsoleMonster

New-ConsoleRows -Content {
    $progressParameters = @{
        Label = 'Resolving DNS records'
        Detail = 'zone 4 of 9'
        Indeterminate = $true
        WidgetId = 'spinner'
    }
    New-ConsoleProgress @progressParameters
    $progressParameters = @{
        Label = 'Schema snapshot'
        Detail = 'cached'
        Status = 'Pass'
        Indeterminate = $true
        WidgetId = 'spinner'
    }
    New-ConsoleProgress @progressParameters
    $progressParameters = @{
        Label = 'Certificate chain'
        Detail = 'expired 3 days ago'
        Status = 'Fail'
        Indeterminate = $true
        WidgetId = 'spinner'
    }
    New-ConsoleProgress @progressParameters
} | Show-Console
