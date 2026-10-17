. .\artifacts.ps1
$artifacts | Format-ConsoleTable -Title '[bold]PSPublishModule[/] [muted]artifacts[/]' -GroupBy Kind -Total -Property @(
    @{ Name = 'Name'; Key = $true; Overflow = 'Never' }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Priority = 9; Aggregate = 'Sum' }
    @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration'; Priority = 6; Aggregate = 'Sum' }
    @{ Name = 'Signed'; Presenter = 'Boolean'; Priority = 5 }
    @{ Name = 'Path'; Overflow = 'TruncateMiddle'; Priority = 1; MinWidth = 20 }
)
