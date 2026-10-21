# Build artifacts as a module build reports them.
$artifacts = @(
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.nupkg'; Kind = 'NuGet'; Length = 1482113; Time = [timespan]::FromSeconds(48.2); Signed = $true; Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster.2.0.0.nupkg' }
#region more
    [pscustomobject] @{ Name = 'ConsoleMonster.2.0.0.snupkg'; Kind = 'NuGet'; Length = 402331; Time = [timespan]::FromSeconds(3.1); Signed = $true; Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster.2.0.0.snupkg' }
    [pscustomobject] @{ Name = 'ConsoleMonster-2.0.0.zip'; Kind = 'Module'; Length = 2904576; Time = [timespan]::FromSeconds(11.7); Signed = $true; Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\ConsoleMonster-2.0.0.zip' }
    [pscustomobject] @{ Name = 'Docs.zip'; Kind = 'Module'; Length = 51302; Time = [timespan]::FromSeconds(1.2); Signed = $false; Path = 'C:\Support\GitHub\ConsoleMonster\Artefacts\Packed\Docs.zip' }
#endregion
)

$artifacts | Format-Table -AutoSize
