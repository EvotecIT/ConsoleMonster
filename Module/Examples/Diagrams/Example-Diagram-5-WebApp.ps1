<#
.SYNOPSIS
Diagram ladder level 5: WebApp.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-5-WebApp.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Shop · production' -Direction Right {
    New-ConsoleDiagramGroup 'App service' {
        New-ConsoleDiagramNode 'Web' -Kind Service -Subtitle '3 instances'
        New-ConsoleDiagramNode 'Orders API' -Kind Service -Subtitle '2 instances'
        New-ConsoleDiagramNode 'Worker' -Kind Process -Subtitle 'emails, invoices'
    }
    New-ConsoleDiagramGroup 'Data' {
        New-ConsoleDiagramNode 'SQL' -Kind Database -Subtitle 'orders'
        New-ConsoleDiagramNode 'Redis' -Kind Database -Subtitle 'sessions'
        New-ConsoleDiagramNode 'Queue' -Kind Queue -Subtitle 'orders.created'
    }
    New-ConsoleDiagramNode 'Browser' -Kind User
    New-ConsoleDiagramNode 'CDN' -Kind Gateway -Subtitle 'Cloudflare'
    New-ConsoleDiagramLink 'Browser' 'CDN'
    New-ConsoleDiagramLink 'CDN' 'Web'
    New-ConsoleDiagramLink 'Web' 'Orders API'
    New-ConsoleDiagramLink 'Orders API' 'SQL'
    New-ConsoleDiagramLink 'Web' 'Redis'
    New-ConsoleDiagramLink 'Orders API' 'Queue'
    New-ConsoleDiagramLink 'Queue' 'Worker'
}
