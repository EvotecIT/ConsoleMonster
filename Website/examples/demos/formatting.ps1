Import-Module ConsoleMonster

# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ Name = 'WebApi'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Worker'; Status = 'Running'; StartType = 'Automatic' }
    [pscustomobject] @{ Name = 'Archive'; Status = 'Stopped'; StartType = 'Manual' }
)

# PowerShell's own output
$services
[version] '2.0.0'
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }

Enable-ConsoleFormatting

# The same objects now
$services
[version] '2.0.0'
@{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }
$services | Show-ConsoleTable -GroupBy Status -MaxRows 6
