BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    . (Join-Path $PSScriptRoot 'Support/ConvertTo-TestEverydayText.ps1')

    function New-ViewRow($typeName, $properties) {
        $row = [pscustomobject] $properties
        $row.PSObject.TypeNames.Insert(0, $typeName)
        $row
    }
}

Describe 'Windows everyday command output' -Skip:([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    It 'renders volume labels and summarizes capacity, free space and unhealthy volumes' {
        $rows = @(
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage/MSFT_Volume' @{
                DriveLetter = [char] 'Q'; FileSystemLabel = 'ExampleData'; FileSystemType = 'NTFS'
                Size = [uint64] 100GB; SizeRemaining = [uint64] 10GB; HealthStatus = 'Healthy'
            }
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#MSFT_Volume' @{
                DriveLetter = [char] 0; FileSystemLabel = 'ExampleBackup'; FileSystemType = 'ReFS'
                Size = [uint64] 50GB; SizeRemaining = [uint64] 2GB; HealthStatus = 'Warning'
            }
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#MSFT_Volume' @{
                DriveLetter = [char] 'R'; FileSystemLabel = 'ExampleWork'; FileSystemType = 'NTFS'
                Size = [uint64] 50GB; SizeRemaining = [uint64] 25GB; HealthStatus = 'Healthy'
            }
        )
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match 'ExampleData'
        $text | Should -Match 'Used'
        $text | Should -Match '3 volumes.*200 Gi?B total.*37 Gi?B free.*1 not healthy'
        ($rows[0] | ConvertTo-TestEverydayText) | Should -Not -Match 'volumes'
    }

    It 'warns once for a disk that is both offline and unhealthy' {
        $rows = @(0, 1 | ForEach-Object {
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#ROOT/Microsoft/Windows/Storage/MSFT_Disk' @{
                Number = [uint32] $_; FriendlyName = "Example disk $_"; BusType = 'SATA'; PartitionStyle = 'GPT'
                HealthStatus = if ($_ -eq 0) { 'Healthy' } else { 'Warning' }
                OperationalStatus = if ($_ -eq 0) { 'Online' } else { 'Offline' }
                Size = [uint64] 128GB
            }
        })
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match 'Example disk 1.*Offline'
        $text | Should -Match '2 disks.*256 Gi?B total.*1 need attention'
    }

    It 'brackets IPv6 endpoints and counts a dual-stack listening port once' {
        $rows = @(0..4 | ForEach-Object {
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetTCPConnection' @{
                LocalAddress = if ($_ % 2) { '2001:db8::1' } else { '192.0.2.1' }
                LocalPort = [uint16] 8443; RemoteAddress = '2001:db8::2'; RemotePort = [uint16] 40000
                State = if ($_ -lt 2) { 'Listen' } else { 'Established' }
                OwningProcess = [uint32] $PID
            }
        })
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match '\[2001:db8::1\]:8443'
        $text | Should -Match '5 connections.*2 Listen.*3 Established.*1 listening port'
        $text | Should -Match ([regex]::Escape([Diagnostics.Process]::GetCurrentProcess().ProcessName))
    }

    It 'summarizes scheduled task states without invoking per-task queries' {
        $states = 'Ready', 'Ready', 'Running', 'Disabled', 'Ready'
        $rows = @(0..4 | ForEach-Object {
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#Root/Microsoft/Windows/TaskScheduler/MSFT_ScheduledTask' @{
                TaskPath = '\Example\'; TaskName = "Example task $_"; State = $states[$_]
            }
        })
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match 'Example task 4'
        $text | Should -Match '5 tasks.*3 Ready.*1 Running.*1 Disabled'
    }

    It 'shows never for an absent last logon and counts enabled and disabled local users' {
        $rows = @(0..2 | ForEach-Object {
            New-ViewRow 'Microsoft.PowerShell.Commands.LocalUser' @{
                Name = "ExampleUser$_"; Enabled = $_ -lt 2
                LastLogon = $(if ($_ -eq 0) { [datetime] '2026-10-21' })
                PasswordExpires = [datetime] '2027-01-15'; Description = '  Example user  '
            }
        })
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match 'ExampleUser2.*never'
        $text | Should -Match '2 enabled.*1 disabled.*2 never logged on'
    }
}

Describe 'Installed PowerShell module output' {
    It 'renders and groups <Command> by repository while allowing missing publication dates' -ForEach @(
        @{ Command = 'Get-InstalledPSResource'; TypeName = 'Microsoft.PowerShell.PSResourceGet.UtilClasses.PSResourceInfo'; Version = [version] '1.2.3' }
        @{ Command = 'Get-InstalledModule'; TypeName = 'Microsoft.PowerShell.Commands.PSRepositoryItemInfo'; Version = '1.2.3' }
    ) {
        $rows = @(0..4 | ForEach-Object {
            New-ViewRow $TypeName @{
                Name = "Example.Module$_"; Version = $Version; Author = 'Example authors'
                Repository = if ($_ -lt 3) { 'PSGallery' } else { 'ExampleFeed' }
                PublishedDate = $(if ($_ -ne 0) { [datetime] '2026-10-21' })
            }
        })
        $text = $rows | ConvertTo-TestEverydayText
        $text | Should -Match 'Example.Module0.*1\.2\.3.*PSGallery.*Example authors'
        $text | Should -Match '5 modules.*3 PSGallery.*2 ExampleFeed'
    }
}
