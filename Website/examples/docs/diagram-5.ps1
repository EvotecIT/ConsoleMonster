Import-Module ConsoleMonster

# Rows you already have; nodes are created from the links.
$links = @(
    @{ From = 'Browser'; To = 'CDN' }
    @{ From = 'CDN'; To = 'Web' }
    @{ From = 'Web'; To = 'Orders API' }
    @{ From = 'Orders API'; To = 'SQL' }
    @{ From = 'Web'; To = 'Redis' }
    @{ From = 'Orders API'; To = 'Queue' }
    @{ From = 'Queue'; To = 'Worker' }
)
New-ConsoleDiagram -Title 'Shop · production' {
    foreach ($link in $links) { New-ConsoleDiagramLink @link }
} | Show-Console
