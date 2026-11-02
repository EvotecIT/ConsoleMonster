<#
.SYNOPSIS
Diagram ladder level 4: Status.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-4-Status.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Small office · Monday 08:10' -Direction Right {
    New-ConsoleDiagramGroup 'Office LAN' {
        New-ConsoleDiagramNode '12 PCs' -Kind Workstation
        New-ConsoleDiagramNode 'File server' -Kind Storage -Subtitle 'disk 91%' -Status Warn
        New-ConsoleDiagramNode 'Printer' -Kind Device -Subtitle 'offline' -Status Fail
    }
    New-ConsoleDiagramGroup 'Cloud' {
        New-ConsoleDiagramNode 'Microsoft 365' -Kind Cloud
        New-ConsoleDiagramNode 'Backup' -Kind Storage -Subtitle 'Offsite' -Status Fail
    }
    New-ConsoleDiagramNode 'Firewall' -Kind Gateway
    New-ConsoleDiagramLink '12 PCs' 'Firewall' -Label 'HTTPS'
    New-ConsoleDiagramLink 'File server' 'Backup' -Label 'failed 2 nights' -Status Fail
    New-ConsoleDiagramLink 'Firewall' 'Microsoft 365' -Label '443'
}
