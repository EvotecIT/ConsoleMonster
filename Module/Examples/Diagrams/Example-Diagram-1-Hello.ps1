<#
.SYNOPSIS
Diagram ladder level 1: Hello.
.DESCRIPTION
Builds a real Diagram widget; pipe it to Show-ConsoleDiagram for keys or Show-Console for a static view.
.EXAMPLE
./Example-Diagram-1-Hello.ps1 | Show-ConsoleDiagram -AsString -Width 120
#>
New-ConsoleDiagram -Title '' -Direction Right {
    New-ConsoleDiagramNode 'Laptop' -Kind Workstation
    New-ConsoleDiagramNode 'Router' -Kind Gateway
    New-ConsoleDiagramNode 'Internet' -Kind Cloud
    New-ConsoleDiagramLink 'Laptop' 'Router'
    New-ConsoleDiagramLink 'Router' 'Internet'
}
