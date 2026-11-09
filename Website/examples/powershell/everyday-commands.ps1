Import-Module ConsoleMonster
Enable-ConsoleFormatting

# Search text: matches highlighted, -Context lines dimmed.
$log = 'Started', 'Warning: api-02 is slow', 'Checked 12', 'Error: api-03 timed out', 'Done'
$log | Select-String -Pattern 'Warning|Error' -Context 1

# ACLs and certificate stores are Windows features.
$windows = $IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop'

# A folder ACL as Get-Acl returns it, built from fixed SDDL with well-known accounts.
if ($windows) {
    $acl = New-Object System.Security.AccessControl.DirectorySecurity
    $acl.SetSecurityDescriptorSddlForm(
        'O:BAG:SYD:PAI(D;;SD;;;WD)(A;OICIID;FA;;;SY)(A;OICI;FA;;;BA)(A;OICIIO;GA;;;CO)(A;OICINP;0x1200a9;;;BU)')
    $acl
}

# Three invented certificates with fixed dates: expired, expiring and valid.
if ($windows) {
    $certificates = @(
        'MIIBKjCB0KADAgECAggvcTE9RKF/bzAKBggqhkjOPQQDAjAbMRkwFwYDVQQDExB2cG4uZXhhbXBsZS50ZXN0MB4XDTI1MTAyMTEyMDAwMFoXDTI2MTAxODEyMDAwMFowGzEZMBcGA1UEAxMQdnBuLmV4YW1wbGUudGVzdDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABKZisgRy8+IHe8o56IQPyVMExb4wQdMQopSpYo9aBg2yy6NZrQI5Mn1+bconqBWt/Qf/IhoeInAEONSwxgd0iEwwCgYIKoZIzj0EAwIDSQAwRgIhAMi0NVEH3Rdj8I9kAvrd8LAPVjBGStsH2mv89JrrsvZaAiEAlIq2VFwK7pSf09EZRZp2ISVtnE9jyBpNob7H/0CHEsQ='
        'MIIBKjCB0KADAgECAggi8amAzhXvhzAKBggqhkjOPQQDAjAbMRkwFwYDVQQDExBhcGkuZXhhbXBsZS50ZXN0MB4XDTI1MTAyMTEyMDAwMFoXDTI2MTAzMDEyMDAwMFowGzEZMBcGA1UEAxMQYXBpLmV4YW1wbGUudGVzdDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABG5qZumktovjlNi/B8Tg2JAmDN0mOspNzMBl/tXceIpoK35Hj1Yb5g/oJuo8ZG4BDI/AfYkxiyhEsjYh2ZSZ1gIwCgYIKoZIzj0EAwIDSQAwRgIhAJ687lafA0jM9v7hnYcq3695N9WgnwmwKphsjJAqkx8nAiEA+35r9XtUFixSxhUPn8gn0rW/oKYzPfyigkA7eTa9jmc='
        'MIIBLjCB1qADAgECAghUZ3JsL9/HszAKBggqhkjOPQQDAjAeMRwwGgYDVQQDExNwb3J0YWwuZXhhbXBsZS50ZXN0MB4XDTI1MTAyMTEyMDAwMFoXDTI3MDIxNDEyMDAwMFowHjEcMBoGA1UEAxMTcG9ydGFsLmV4YW1wbGUudGVzdDBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABHKVW2PoHLpQq0yTzkqV1M8UGH81HENH6tz9seHhy5eLWTK2FM4a9G1mfcisZEZt60lmSYiM1jzy/7xbDYjAqDIwCgYIKoZIzj0EAwIDRwAwRAIgc0rww4RQjhbtDpcwyj5dC1RRG/cI1z36/n39AM5hWlwCIH9nSfnbTygb4p+mRu/h4b62fXA8cdwCzNigXR36rQWG'
    )
    foreach ($text in $certificates) {
        $bytes = [Convert]::FromBase64String($text)
        New-Object Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList (, $bytes)
    }
}

# Counts, statistics and elapsed time.
'Info', 'Warning', 'Info', 'Error', 'Info' | Group-Object | Sort-Object Count -Descending
12, 48, 7, 30 | Measure-Object -Sum -Average -Minimum -Maximum
New-TimeSpan -Start '2026-10-21 12:00' -End '2026-10-21 13:42:05'

# Windows inventory with fixed, invented rows. These match the provider type names without recording this machine.
$storage = 'Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage'
$cim = 'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2'

# Get-Volume
@(
    [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'Q'; FileSystemLabel = 'Example data'; FileSystemType = 'NTFS'; HealthStatus = 'Healthy'; Size = [uint64] 128GB; SizeRemaining = [uint64] 72GB }
    [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'R'; FileSystemLabel = 'Example build'; FileSystemType = 'NTFS'; HealthStatus = 'Healthy'; Size = [uint64] 256GB; SizeRemaining = [uint64] 32GB }
    [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'S'; FileSystemLabel = 'Example archive'; FileSystemType = 'ReFS'; HealthStatus = 'Warning'; Size = [uint64] 512GB; SizeRemaining = [uint64] 16GB }
)

# Get-Disk
@(
    [pscustomobject] @{ PSTypeName = "$storage/MSFT_Disk"; Number = [uint32] 0; FriendlyName = 'Example system disk'; BusType = 'NVMe'; PartitionStyle = 'GPT'; HealthStatus = 'Healthy'; OperationalStatus = 'Online'; Size = [uint64] 512GB }
    [pscustomobject] @{ PSTypeName = "$storage/MSFT_Disk"; Number = [uint32] 1; FriendlyName = 'Example backup disk'; BusType = 'SATA'; PartitionStyle = 'GPT'; HealthStatus = 'Warning'; OperationalStatus = 'Offline'; Size = [uint64] 1TB }
)

# Get-NetTCPConnection
$states = 'Established', 'Listen', 'TimeWait', 'CloseWait', 'Established'
0..4 | ForEach-Object {
    # The invented remote host keeps these fixture PIDs from resolving against the capture machine.
    [pscustomobject] @{ PSTypeName = "$cim/MSFT_NetTCPConnection"; LocalAddress = '192.0.2.10'; LocalPort = [uint16] (8080 + $_); RemoteAddress = '2001:db8::10'; RemotePort = [uint16] (50000 + $_); State = $states[$_]; OwningProcess = [uint32] (200 + $_); PSComputerName = 'example.test' }
}

# Get-ScheduledTask
$states = 'Ready', 'Running', 'Ready', 'Disabled', 'Ready'
$tasks = 'Build index', 'Refresh sample data', 'Archive logs', 'Legacy report', 'Rotate snapshots'
0..4 | ForEach-Object {
    [pscustomobject] @{ PSTypeName = 'Microsoft.Management.Infrastructure.CimInstance#Root/Microsoft/Windows/TaskScheduler/MSFT_ScheduledTask'; TaskPath = '\Example\'; TaskName = $tasks[$_]; State = $states[$_] }
}

# Get-LocalUser
@(
    [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'; Name = 'example-reader'; Enabled = $true; LastLogon = [datetime] '2026-10-20 12:00'; PasswordExpires = [datetime] '2027-01-15'; Description = 'Read-only demonstration' }
    [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'; Name = 'example-build'; Enabled = $true; LastLogon = [datetime] '2026-10-21 11:30'; PasswordExpires = $null; Description = 'Build demonstration' }
    [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'; Name = 'example-retired'; Enabled = $false; LastLogon = $null; PasswordExpires = $null; Description = 'Retired demonstration' }
)

# Get-InstalledPSResource and Get-InstalledModule
$resourceType = if ($PSEdition -eq 'Desktop') { 'Microsoft.PowerShell.Commands.PSRepositoryItemInfo' } else { 'Microsoft.PowerShell.PSResourceGet.UtilClasses.PSResourceInfo' }
0..4 | ForEach-Object {
    [pscustomobject] @{ PSTypeName = $resourceType; Name = "Example.Module$_"; Version = [version] '1.2.3'; Repository = if ($_ -lt 3) { 'PSGallery' } else { 'ExampleFeed' }; Author = 'Example authors'; PublishedDate = [datetime] '2026-09-15' }
}
