<#
.SYNOPSIS
Diagram ladder level 2: Labels.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-2-Labels.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Home network' -Direction Right {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation -Subtitle 'Wi-Fi'
    New-ConsoleDiagramNode 'TV' -Kind Workstation -Subtitle 'Ethernet'
    New-ConsoleDiagramNode 'NAS' -Kind Storage -Subtitle 'Photos, backups'
    New-ConsoleDiagramNode 'Router' -Kind Gateway -Subtitle '192.168.1.1'
    New-ConsoleDiagramNode 'Internet' -Kind Cloud -Subtitle 'Fibre 1 Gb'
    New-ConsoleDiagramLink 'Laptop' 'Router' -Label 'Wi-Fi'
    New-ConsoleDiagramLink 'TV' 'Router' -Label '1 Gb'
    New-ConsoleDiagramLink 'NAS' 'Router' -Label '1 Gb'
    New-ConsoleDiagramLink 'Router' 'Internet' -Label 'WAN'
}
