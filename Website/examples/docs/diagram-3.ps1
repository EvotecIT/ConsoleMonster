Import-Module ConsoleMonster

# A group is a block around its nodes.
New-ConsoleDiagram -Title 'Small office' {
    New-ConsoleDiagramGroup 'Office LAN' {
        New-ConsoleDiagramNode '12 PCs' -Kind Workstation -To 'Firewall'
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
    New-ConsoleDiagramNode 'Firewall' -Kind Gateway -To 'Web server', 'Microsoft 365'
    New-ConsoleDiagramLink 'File server' 'Backup' -Label 'nightly'
} | Show-Console
