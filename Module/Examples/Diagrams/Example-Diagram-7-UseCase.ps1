<#
.SYNOPSIS
Diagram ladder level 7: UseCase.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-7-UseCase.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'Checkout' -Direction Right {
    New-ConsoleDiagramNode 'Shopper' -Kind User
    New-ConsoleDiagramNode 'Web' -Kind Service
    New-ConsoleDiagramNode 'Orders API' -Kind Service
    New-ConsoleDiagramNode 'Payments' -Kind External -Subtitle 'Stripe'
    New-ConsoleDiagramNode 'SQL' -Kind Database
    New-ConsoleDiagramLink 'Shopper' 'Web'
    New-ConsoleDiagramLink 'Web' 'Orders API'
    New-ConsoleDiagramLink 'Orders API' 'Payments'
    New-ConsoleDiagramLink 'Orders API' 'SQL'
    New-ConsoleDiagramUseCase 'Place an order' {
        New-ConsoleDiagramStep 'Shopper' 'Web' 'POST /checkout'
        New-ConsoleDiagramStep 'Web' 'Orders API' 'create order' -Payload @{ sku = 'MUG-01'; qty = 2 }
        New-ConsoleDiagramStep 'Orders API' 'Payments' 'charge 24.00 EUR'
        New-ConsoleDiagramStep 'Payments' 'Orders API' 'approved' -Reply
        New-ConsoleDiagramStep 'Orders API' 'SQL' 'INSERT order'
        New-ConsoleDiagramStep 'Orders API' 'Web' '201 order 1042' -Reply
        New-ConsoleDiagramStep 'Web' 'Shopper' 'Thank you page' -Reply
    }
}
