Import-Module ConsoleMonster

# Invented services: the same inventory before and after formatting is enabled.
$services = @(
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'
        ServiceName = 'WebApi'; Status = 'Running'; ServicesDependedOn = @()
        DisplayName = 'Web API' }
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'
        ServiceName = 'Worker'; Status = 'Running'; ServicesDependedOn = @()
        DisplayName = 'Background worker' }
    [pscustomobject] @{ PSTypeName = 'System.ServiceProcess.ServiceController'
        ServiceName = 'Archive'; Status = 'Stopped'; ServicesDependedOn = @()
        DisplayName = 'Archive' }
)

# PowerShell's own output
$services
[version] '2.0.0'
[ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }

Clear-Host
Enable-ConsoleFormatting

# The same objects now
$services
[version] '2.0.0'
[ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = $false }
