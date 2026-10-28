Import-Module ConsoleMonster


# Relative dates use a fixed clock, matching both C# styles.
$now = [datetimeoffset] '2026-10-16T14:30:00+02:00'
$root = 'D:\Builds\ConsoleMonster\Packed\'

$rows = @(
    [pscustomobject] @{
        Artifact = 'ConsoleMonster.2.0.0.nupkg'
        Kind = 'NuGet'
        Size = 1482113
        Built = [timespan]::FromSeconds(48.2)
        Created = $now.AddMinutes(-12)
        Path = ($root + 'ConsoleMonster.2.0.0.nupkg')
        SHA256 = '9f2c4e81a7d03b6e'
    }
    [pscustomobject] @{
        Artifact = 'ConsoleMonster.2.0.0.snupkg'
        Kind = 'Symbols'
        Size = 402331
        Built = [timespan]::FromSeconds(3.1)
        Created = $now.AddMinutes(-12)
        Path = ($root + 'ConsoleMonster.2.0.0.snupkg')
        SHA256 = '41ab09cd77e2f310'
    }
    [pscustomobject] @{
        Artifact = 'ConsoleMonster-2.0.0.zip'
        Kind = 'Module'
        Size = 2904576
        Built = [timespan]::FromSeconds(11.7)
        Created = $now.AddMinutes(-11)
        Path = ($root + 'ConsoleMonster-2.0.0.zip')
        SHA256 = 'c0ffee4251de9a07'
    }
    [pscustomobject] @{
        Artifact = 'ConsoleMonster.PowerShell.dll'
        Kind = 'Binary'
        Size = 188416
        Built = [timespan]::FromSeconds(22.9)
        Created = $now.AddHours(-3)
        Path = 'D:\Builds\ConsoleMonster\Release\net8.0\ConsoleMonster.PowerShell.dll'
        SHA256 = '77aa13f9b2c46e5d'
    }
    [pscustomobject] @{
        Artifact = 'en-US\ConsoleMonster-help.xml'
        Kind = 'Help'
        Size = 96210
        Built = [timespan]::FromSeconds(6.4)
        Created = $now.AddDays(-2)
        Path = ($root + 'en-US\ConsoleMonster-help.xml')
        SHA256 = '0be4d29c6a1f8873'
    }
    [pscustomobject] @{
        Artifact = 'Docs.zip'
        Kind = 'Docs'
        Size = 51302
        Built = [timespan]::FromSeconds(1.2)
        Created = $now.AddDays(-9)
        Path = ($root + 'Docs.zip')
        SHA256 = '5d6e7f8091a2b3c4'
    }
)

$tableParameters = @{
    Title = '[bold]Build artifacts[/] [muted]· ConsoleMonster 2.0.0[/]'
    ShowTotals = $true
    MaxRows = 5
    TotalRowCount = 1234
    Presentation = (@{ Now = $now })
}
$rows |
    Show-ConsoleTable @tableParameters -Property @(
        @{ Name = 'Artifact'; Key = $true; Overflow = 'Never' }
        @{ Name = 'Kind'; Priority = 3 }
        @{ Name = 'Size'; Priority = 9; Presenter = 'Bytes'; Aggregate = 'Sum' }
        @{ Name = 'Built'; Header = 'Built in'; Priority = 6; Presenter = 'Duration'
            Aggregate = 'Sum' }
        @{ Name = 'Created'; Priority = 4; Presenter = 'DateRelative' }
        @{ Name = 'Path'; Priority = 1; Overflow = 'TruncateMiddle'; MinWidth = 20; MaxWidth = 40 }
        @{ Name = 'SHA256'; Priority = 0; Overflow = 'Truncate'; MaxWidth = 14 }
    )
