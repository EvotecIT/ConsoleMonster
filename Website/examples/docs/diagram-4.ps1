Import-Module ConsoleMonster

# Status uses the same words as every widget; -Value says what is wrong.
New-ConsoleDiagram -Title 'Small office · Monday 08:10' {
    New-ConsoleDiagramGroup 'Office LAN' {
        New-ConsoleDiagramNode '12 PCs' -Kind Workstation -To 'Firewall'
        New-ConsoleDiagramNode 'File server' -Kind Storage -Status Warn -Value 'disk 91%'
        New-ConsoleDiagramNode 'Printer' -Kind Device -Status Fail -Value 'offline'
    }
    New-ConsoleDiagramGroup 'Cloud' {
        New-ConsoleDiagramNode 'Microsoft 365' -Kind Cloud
        New-ConsoleDiagramNode 'Backup' -Kind Storage -Status Fail -Value 'last success 2 days ago'
    }
    New-ConsoleDiagramNode 'Firewall' -Kind Gateway -To 'Microsoft 365'
    New-ConsoleDiagramLink 'File server' 'Backup' -Label 'failed 2 nights' -Status Fail
} | Show-Console
