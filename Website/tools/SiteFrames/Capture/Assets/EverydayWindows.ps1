# Invented Windows inventory for the typed everyday-commands capture. No host data is recorded.
function global:Get-Volume {
    $storage = 'Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage'
    @(
        [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'Q'
            FileSystemLabel = 'Example data'; FileSystemType = 'NTFS'; HealthStatus = 'Healthy'
            Size = [uint64] 128GB; SizeRemaining = [uint64] 72GB }
        [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'R'
            FileSystemLabel = 'Example build'; FileSystemType = 'NTFS'; HealthStatus = 'Healthy'
            Size = [uint64] 256GB; SizeRemaining = [uint64] 32GB }
        [pscustomobject] @{ PSTypeName = "$storage/MSFT_Volume"; DriveLetter = [char] 'S'
            FileSystemLabel = 'Example archive'; FileSystemType = 'ReFS'; HealthStatus = 'Warning'
            Size = [uint64] 512GB; SizeRemaining = [uint64] 16GB }
    )
}

function global:Get-Disk {
    $storage = 'Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage'
    @(
        [pscustomobject] @{ PSTypeName = "$storage/MSFT_Disk"; Number = [uint32] 0
            FriendlyName = 'Example system disk'; BusType = 'NVMe'; PartitionStyle = 'GPT'
            HealthStatus = 'Healthy'; OperationalStatus = 'Online'; Size = [uint64] 512GB }
        [pscustomobject] @{ PSTypeName = "$storage/MSFT_Disk"; Number = [uint32] 1
            FriendlyName = 'Example backup disk'; BusType = 'SATA'; PartitionStyle = 'GPT'
            HealthStatus = 'Warning'; OperationalStatus = 'Offline'; Size = [uint64] 1TB }
    )
}

function global:Get-NetTCPConnection {
    $cim = 'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2'
    $states = 'Established', 'Listen', 'TimeWait', 'CloseWait', 'Established'
    0..4 | ForEach-Object {
        # The invented remote host keeps these fixture PIDs from resolving against the capture machine.
        [pscustomobject] @{ PSTypeName = "$cim/MSFT_NetTCPConnection"; LocalAddress = '192.0.2.10'
            LocalPort = [uint16] (8080 + $_); RemoteAddress = '2001:db8::10'
            RemotePort = [uint16] (50000 + $_); State = $states[$_]
            OwningProcess = [uint32] (200 + $_); PSComputerName = 'example.test' }
    }
}

function global:Get-ScheduledTask {
    $states = 'Ready', 'Running', 'Ready', 'Disabled', 'Ready'
    $tasks = 'Build index', 'Refresh sample data', 'Archive logs', 'Legacy report',
        'Rotate snapshots'
    0..4 | ForEach-Object {
        [pscustomobject] @{ PSTypeName = 'Microsoft.Management.Infrastructure.CimInstance#Root/Microsoft/Windows/TaskScheduler/MSFT_ScheduledTask'
            TaskPath = '\Example\'; TaskName = $tasks[$_]; State = $states[$_] }
    }
}

function global:Get-LocalUser {
    @(
        [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'
            Name = 'example-reader'; Enabled = $true; LastLogon = [datetime] '2026-10-20 12:00'
            PasswordExpires = [datetime] '2027-01-15'; Description = 'Read-only demonstration' }
        [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'
            Name = 'example-build'; Enabled = $true; LastLogon = [datetime] '2026-10-21 11:30'
            PasswordExpires = $null; Description = 'Build demonstration' }
        [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.LocalUser'
            Name = 'example-retired'; Enabled = $false; LastLogon = $null; PasswordExpires = $null
            Description = 'Retired demonstration' }
    )
}

function global:Get-InstalledModule {
    $resourceType = if ($PSEdition -eq 'Desktop') {
        'Microsoft.PowerShell.Commands.PSRepositoryItemInfo'
    }
    else { 'Microsoft.PowerShell.PSResourceGet.UtilClasses.PSResourceInfo' }
    0..4 | ForEach-Object {
        [pscustomobject] @{ PSTypeName = $resourceType; Name = "Example.Module$_"
            Version = [version] '1.2.3'
            Repository = if ($_ -lt 3) { 'PSGallery' } else { 'ExampleFeed' }
            Author = 'Example authors'; PublishedDate = [datetime] '2026-09-15' }
    }
}
