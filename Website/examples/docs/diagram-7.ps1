Import-Module ConsoleMonster

# A use case walks a request through the diagram; this shows its third step.
Show-ConsoleDiagram -Title 'Checkout' -Step 3 -StaticSnapshot {
    New-ConsoleDiagramNode 'Shopper' -Kind User -To 'Web'
    New-ConsoleDiagramNode 'Web' -Kind Service -To 'Orders API'
    New-ConsoleDiagramNode 'Orders API' -Kind Service -To 'Payments', 'SQL'
    New-ConsoleDiagramNode 'Payments' -Kind External -Subtitle 'Stripe'
    New-ConsoleDiagramNode 'SQL' -Kind Database
    New-ConsoleDiagramUseCase 'Place an order' {
        New-ConsoleDiagramStep 'Shopper' 'Web' 'POST /checkout'
        New-ConsoleDiagramStep 'Web' 'Orders API' 'create order'
        New-ConsoleDiagramStep 'Orders API' 'Payments' 'charge 24.00 EUR'
        New-ConsoleDiagramStep 'Payments' 'Orders API' 'approved' -Reply
        New-ConsoleDiagramStep 'Orders API' 'SQL' 'INSERT order'
        New-ConsoleDiagramStep 'Orders API' 'Web' '201 order 1042' -Reply
        New-ConsoleDiagramStep 'Web' 'Shopper' 'Thank you page' -Reply
    }
}
