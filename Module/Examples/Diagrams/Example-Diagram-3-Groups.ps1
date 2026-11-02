<#
.SYNOPSIS
Diagram ladder level 3: Groups.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-3-Groups.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Small office' -Direction Right {
    New-ConsoleDiagramGroup 'Office LAN' {
        New-ConsoleDiagramNode '12 PCs' -Kind Workstation
        New-ConsoleDiagramNode 'File server' -Kind Storage -Subtitle 'FS01'
        New-ConsoleDiagramNode 'Printer' -Kind Device
    }
    New-ConsoleDiagramGroup 'DMZ' {
        New-ConsoleDiagramNode 'Web server' -Kind Server -Subtitle 'WEB01'
    }
    New-ConsoleDiagramGroup 'Cloud' {
        New-ConsoleDiagramNode 'Microsoft 365' -Kind Cloud
        New-ConsoleDiagramNode 'Backup' -Kind Storage -Subtitle 'Offsite'
    }
    New-ConsoleDiagramNode 'Firewall' -Kind Gateway -Subtitle 'FW01'
    New-ConsoleDiagramLink '12 PCs' 'Firewall' -Label 'HTTPS'
    New-ConsoleDiagramLink 'File server' 'Backup' -Label 'nightly'
    New-ConsoleDiagramLink 'Firewall' 'Web server' -Label '443'
    New-ConsoleDiagramLink 'Firewall' 'Microsoft 365' -Label '443'
}
