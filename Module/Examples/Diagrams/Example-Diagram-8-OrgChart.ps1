<#
.SYNOPSIS
Diagram ladder level 8: OrgChart.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-8-OrgChart.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title 'IT team' -Direction Down {
    New-ConsoleDiagramNode 'Anna' -Kind Person -Subtitle 'CIO'
    New-ConsoleDiagramNode 'Marek' -Kind Person -Subtitle 'Operations'
    New-ConsoleDiagramNode 'Julia' -Kind Person -Subtitle 'Development'
    New-ConsoleDiagramNode 'Tom' -Kind Person -Subtitle 'Service desk'
    New-ConsoleDiagramNode 'Ola' -Kind Person -Subtitle 'Network'
    New-ConsoleDiagramNode 'Ben' -Kind Person -Subtitle 'Apps'
    New-ConsoleDiagramLink 'Anna' 'Marek'
    New-ConsoleDiagramLink 'Anna' 'Julia'
    New-ConsoleDiagramLink 'Marek' 'Tom'
    New-ConsoleDiagramLink 'Marek' 'Ola'
    New-ConsoleDiagramLink 'Julia' 'Ben'
}
