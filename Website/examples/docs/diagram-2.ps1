Import-Module ConsoleMonster

# -Kind says what a node is; a link of its own can carry a label.
New-ConsoleDiagram -Title 'Home network' {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation -Subtitle 'Wi-Fi'
    New-ConsoleDiagramNode 'TV' -Kind Workstation -Subtitle 'Ethernet'
    New-ConsoleDiagramNode 'NAS' -Kind Storage -Subtitle 'Photos, backups'
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Subtitle '192.168.1.1'
    New-ConsoleDiagramNode 'Internet' -Kind Cloud -Subtitle 'Fibre 1 Gb'
    New-ConsoleDiagramLink 'Laptop' 'Router' -Label 'Wi-Fi'
    New-ConsoleDiagramLink 'TV' 'Router' -Label '1 Gb'
    New-ConsoleDiagramLink 'NAS' 'Router' -Label '1 Gb'
    New-ConsoleDiagramLink 'Router' 'Internet' -Label 'WAN'
} | Show-Console
