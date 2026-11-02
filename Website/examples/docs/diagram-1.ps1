Import-Module ConsoleMonster

# A node can point at other nodes with -To.
New-ConsoleDiagram {
    New-ConsoleDiagramNode 'Laptop' -To 'Router'
    New-ConsoleDiagramNode 'Router' -To 'Internet'
    New-ConsoleDiagramNode 'Internet'
} | Show-Console
