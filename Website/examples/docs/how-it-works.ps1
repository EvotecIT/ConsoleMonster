Import-Module ConsoleMonster

# Invented services; nothing is queried.
$services = @(
    [pscustomobject] @{ Name = 'Web'; Server = 'web01.example.com'; Status = 'Pass' }
    [pscustomobject] @{ Name = 'Mail'; Server = 'mail01.example.com'; Status = 'Warn' }
    [pscustomobject] @{ Name = 'Files'; Server = 'fs01.example.com'; Status = 'Pass' }
)

# Build: each New-Console* command returns a widget; containers take their children in -Content.
$report = New-ConsoleRows -Gap 1 -Content {
    New-ConsoleRule -Title '[accent bold]Service check[/]'
    New-ConsoleFacts -Content {
        @{ Value = 2; Label = 'healthy'; Role = 'Pass' }
        @{ Value = 1; Label = 'degraded'; Role = 'Warn' }
    }
    New-ConsolePanel -Title 'Services' -Content {
        $services | New-ConsoleTable -Property Name, Server, Status -StatusProperty Status
    }
}

# Render: print the finished widget once.
$report | Show-Console
