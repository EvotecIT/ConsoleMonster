Import-Module ConsoleMonster

# The values a live session would keep updating; the layout never moves.
New-ConsoleDiagram -Title 'Ping monitor · every 5 s' {
    New-ConsoleDiagramNode 'This PC' -Kind Workstation -To 'Router'
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Value '1 ms' -To 'DNS', 'VPN', 'contoso.com'
    New-ConsoleDiagramNode 'DNS' -Kind Server -Subtitle '1.1.1.1' -Value '12 ms'
    New-ConsoleDiagramNode 'VPN' -Kind Server -Subtitle '10.8.0.1' -Status Warn -Value '180 ms'
    New-ConsoleDiagramNode 'contoso.com' -Kind Cloud -Status Fail -Value 'timeout'
} | Show-Console
