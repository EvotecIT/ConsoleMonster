. .\artifacts.ps1
$artifacts[0] | Format-ConsoleList -Property Name, Kind, @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes' }, @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration' }, @{ Name = 'Signed'; Presenter = 'Boolean' }, Path
