<#
.SYNOPSIS
Diagram ladder level 6: LivePing.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-6-LivePing.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Ping monitor · every 5 s' -Direction Right {
    New-ConsoleDiagramNode 'This PC' -Kind Workstation
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Value '1 ms'
    New-ConsoleDiagramNode 'DNS' -Kind Server -Subtitle '1.1.1.1' -Value '12 ms'
    New-ConsoleDiagramNode 'VPN' -Kind Server -Subtitle 'vpn.contoso.com' -Status Warn -Value '180 ms'
    New-ConsoleDiagramNode 'contoso.com' -Kind Cloud -Status Fail -Value 'timeout'
    New-ConsoleDiagramLink 'This PC' 'Router'
    New-ConsoleDiagramLink 'Router' 'DNS'
    New-ConsoleDiagramLink 'Router' 'VPN'
    New-ConsoleDiagramLink 'Router' 'contoso.com'
}
