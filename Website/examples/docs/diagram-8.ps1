Import-Module ConsoleMonster

# Links that form a tree become a hierarchy, laid out top to bottom.
New-ConsoleDiagram -Title 'IT team' -Direction Down {
    New-ConsoleDiagramNode 'Anna' -Kind Person -Subtitle 'CIO' -To 'Marek', 'Julia'
    New-ConsoleDiagramNode 'Marek' -Kind Person -Subtitle 'Operations' -To 'Tom', 'Ola'
    New-ConsoleDiagramNode 'Julia' -Kind Person -Subtitle 'Development' -To 'Ben'
    New-ConsoleDiagramNode 'Tom' -Kind Person -Subtitle 'Service desk'
    New-ConsoleDiagramNode 'Ola' -Kind Person -Subtitle 'Network'
    New-ConsoleDiagramNode 'Ben' -Kind Person -Subtitle 'Apps'
} | Show-Console
