Import-Module ConsoleMonster

# The build artifacts from above, as a Markdown table for a report or a pull request.
$artifacts = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.nupkg'; Kind = 'NuGet'; Length = 1482113; Time = [timespan]::FromSeconds(48.2); Signed = $true }
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.snupkg'; Kind = 'NuGet'; Length = 402331; Time = [timespan]::FromSeconds(3.1); Signed = $true }
    [pscustomobject] @{ Name = 'ConsoleMonster-2.0.0.zip'; Kind = 'Module'; Length = 2904576; Time = [timespan]::FromSeconds(11.7); Signed = $true }
    [pscustomobject] @{ Name = 'Docs.zip'; Kind = 'Module'; Length = 51302; Time = [timespan]::FromSeconds(1.2); Signed = $false }
)

$artifacts | Format-ConsoleTable -As Markdown -Property @(
    @{ Name = 'Name'; Key = $true }
    @{ Name = 'Kind' }
    @{ Name = 'Length'; Header = 'Size'; Presenter = 'Bytes'; Aggregate = 'Sum' }
    @{ Name = 'Time'; Header = 'Built in'; Presenter = 'Duration'; Aggregate = 'Sum' }
    @{ Name = 'Signed'; Presenter = 'Boolean' }
) -Total