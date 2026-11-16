BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../ConsoleMonster.psd1') -Force
    . (Join-Path $PSScriptRoot 'Support/ConvertTo-TestEverydayText.ps1')

    # These views match the type names their commands return, so synthetic objects exercise them on every platform.
    function New-ViewRow($typeName, $properties) {
        $row = [pscustomobject] $properties
        $row.PSObject.TypeNames.Insert(0, $typeName)
        $row
    }
}

Describe 'Everyday administration command output' {
    It 'lists an environment path variable one folder per line and leaves other values whole' {
        $name = 'CONSOLEMONSTER_TEST_PATH'
        $first = [IO.Path]::GetTempPath().TrimEnd([IO.Path]::DirectorySeparatorChar)
        Set-Item "Env:$name" ($first, 'missing-folder-example', $PSHOME -join [IO.Path]::PathSeparator)
        try {
            $text = Get-ChildItem "Env:$name" | ConvertTo-TestEverydayText -Width 200
        } finally { Remove-Item "Env:$name" }
        $lines = $text -split '\r?\n'
        @($lines | Where-Object { $_ -match "│\s+$([regex]::Escape($first))\s+│" }).Count | Should -Be 1
        @($lines | Where-Object { $_ -match '│\s+missing-folder-example\s+│' }).Count | Should -Be 1
        $text | Should -Not -Match ([regex]::Escape([string] [IO.Path]::PathSeparator) + 'missing-folder-example')

        $plain = @{ Retries = 3 }.GetEnumerator() | ConvertTo-TestEverydayText
        $plain | Should -Match 'Retries\s+│\s+3'
    }

    It 'shows Get-EventLog entries with the Get-WinEvent columns and summary' {
        $start = [datetime] '2026-11-16 08:00'
        $rows = 0..3 | ForEach-Object {
            New-ViewRow 'System.Diagnostics.EventLogEntry' @{
                TimeGenerated = $start.AddMinutes($_ * 10); InstanceId = [long] (1073748860 + $_)
                EntryType = @('Error', 'Warning', 'Information', 'FailureAudit')[$_]; Source = 'Example Source'; Message = "First line $_`nSecond line"
            }
        }
        $text = $rows | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match '7036\s+│\s+\S+ Error'
        $text | Should -Match 'Audit failure'
        $text | Should -Match 'First line 0'
        $text | Should -Not -Match 'Second line'
        $text | Should -Match '1 error · 1 warning · 1 information · 1 audit failure · over 30 m'
    }

    It 'shows logical disks with usage and warns about nearly full drives' {
        $rows = @(
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk' @{ DeviceID = 'C:'; VolumeName = 'System'; DriveType = [uint32] 3; FileSystem = 'NTFS'; Size = [uint64] 500GB; FreeSpace = [uint64] 20GB }
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#Win32_LogicalDisk' @{ DeviceID = 'D:'; VolumeName = 'Data'; DriveType = [uint32] 3; FileSystem = 'NTFS'; Size = [uint64] 1TB; FreeSpace = [uint64] 600GB }
            New-ViewRow 'System.Management.ManagementObject#root\cimv2\Win32_LogicalDisk' @{ DeviceID = 'Z:'; VolumeName = 'Share'; DriveType = [uint32] 4; FileSystem = 'NTFS'; Size = [uint64] 2TB; FreeSpace = [uint64] 1TB }
            # Get-CimInstance -Property can leave FreeSpace out; an unknown drive is not counted as full.
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk' @{ DeviceID = 'Y:'; VolumeName = 'Unknown'; DriveType = [uint32] 3; FileSystem = 'NTFS'; Size = [uint64] 1TB; FreeSpace = $null }
        )
        $text = $rows | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match 'C:\s+│\s+System\s+│\s+\[?Local\]?'
        $text | Should -Match 'Network'
        $text | Should -Match '96%'
        $text | Should -Match '4 drives · 4.49 Ti?B total · 1.61 Ti?B free · 1 over 90% full'
    }

    It 'shows the operating system as one record with uptime and memory in use' {
        $text = New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem' @{
            Caption = 'Example OS '; Version = '10.0.26100'; OSArchitecture = '64-bit'; CSName = 'EXAMPLE-PC'
            InstallDate = [datetime] '2025-01-10'; LastBootUpTime = (Get-Date).AddDays(-2); TotalVisibleMemorySize = [uint64] 33554432; FreePhysicalMemory = [uint64] 8388608
        } | ConvertTo-TestEverydayText
        $text | Should -Match 'Version\s+10\.0\.26100'
        $text | Should -Match 'Last boot\s+2 d ago'
        $text | Should -Match 'Memory\s+32 Gi?B'
        $text | Should -Match 'Memory used\s+.*75%'
    }

    It 'shows hotfixes with relative install dates and the latest one' {
        $rows = 0..2 | ForEach-Object {
            New-ViewRow 'System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering' @{
                HotFixID = "KB50000$_"; Description = @('Update', 'Security Update', 'Security Update')[$_]
                InstalledOn = (Get-Date).Date.AddDays(-10 * (3 - $_)); InstalledBy = 'EXAMPLE\admin'; CSName = 'EXAMPLE-PC'
            }
        }
        $text = $rows | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match 'KB500002.*Security Update.*10 d ago'
        $text | Should -Match '3 updates · 2 security · latest 10 d ago'
    }

    It 'words optional feature states and counts features pending a restart' {
        $rows = 'Enabled', 'Disabled', 'EnablePending', 'DisabledWithPayloadRemoved' | ForEach-Object {
            New-ViewRow 'Microsoft.Dism.Commands.BasicFeatureObject' @{ FeatureName = "Example-$_"; State = $_ }
        }
        $text = $rows | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match 'Example-EnablePending\s+│\s+Enable pending'
        $text | Should -Match 'Disabled with payload removed'
        $text | Should -Match '1 enabled · 2 disabled · 1 pending restart'
    }

    It 'shows firewall rules with their action and direction counts' {
        $rows = 0..4 | ForEach-Object {
            New-ViewRow 'Microsoft.Management.Infrastructure.CimInstance#root/standardcimv2/MSFT_NetFirewallRule' @{
                DisplayName = "Example rule $_"; Enabled = @('True', 'False')[$_ % 2]; Direction = @('Inbound', 'Outbound')[[int] ($_ -gt 2)]
                Action = @('Allow', 'Block')[[int] ($_ -eq 4)]; Profile = 'Domain'; DisplayGroup = 'Example'
            }
        }
        $text = $rows | ConvertTo-TestEverydayText -Width 120
        $text | Should -Match 'Example rule 4\s+│\s+\S+ yes\s+│\s+\[?Outbound\]?\s+│\s+Block'
        $text | Should -Match '5 rules · 3 enabled · 3 inbound · 2 outbound · 1 block'
    }

    It 'shows registry keys under their parent with the first values as name = data' {
        $values = [ordered] @{ '' = 'default text'; Path = 'C:\[Tools]'; Data = [byte[]] (0xde, 0xad); A = 1; B = 2; C = 3; D = 4 }
        $key = New-ViewRow 'Microsoft.Win32.RegistryKey' @{
            PSChildName = 'Example'; PSParentPath = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE'; SubKeyCount = 2
        }
        $key | Add-Member ScriptMethod GetValueNames { [string[]] $values.Keys }.GetNewClosure()
        # The leading comma returns a byte[] whole, as RegistryKey.GetValue does.
        $key | Add-Member ScriptMethod GetValue { param($name, $default, $options) , $values[$name] }.GetNewClosure()
        $text = $key | ConvertTo-TestEverydayText
        $text | Should -Match 'Hive: HKEY_LOCAL_MACHINE\\SOFTWARE'
        $text | Should -Match 'Example\s+│\s+2\s+│\s+\(default\) = default text'
        $text | Should -Match '│\s+Path = C:\\\[Tools\]\s+│'
        $text | Should -Match '│\s+Data = de ad\s+│'
        $text | Should -Match '\+2 more'
    }

    It 'shows a web response as one record with its status and the start of the content' {
        $headers = [Collections.Generic.Dictionary[string, string[]]]::new()
        $headers['Content-Type'] = @('text/html')
        $text = New-ViewRow 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject' @{
            StatusCode = 404; StatusDescription = 'Not Found'; Headers = $headers; RawContentLength = [long] 1536
            Content = "<!doctype html>`n`n  <html>`n<head>`n<title>Example</title>"
        } | ConvertTo-TestEverydayText
        $text | Should -Match 'Status\s+404 Not Found'
        $text | Should -Match 'Content type\s+text/html'
        $text | Should -Match 'Size\s+1\.5 Ki?B'
        $text | Should -Match '<html>'
        $text | Should -Not -Match '<title>'
    }

    It 'keeps the whole hash and shows the path relative to the current folder' {
        $file = Join-Path $TestDrive 'hash-example.txt'
        [IO.File]::WriteAllText($file, 'example')
        Push-Location $TestDrive
        try {
            $text = Get-FileHash -LiteralPath $file | ConvertTo-TestEverydayText -Width 120
        } finally { Pop-Location }
        $text | Should -Match ((Get-FileHash -LiteralPath $file).Hash)
        $text | Should -Match '│\s+hash-example\.txt\s+│'

        # A SHA512 hash wraps in a narrow terminal but stays whole.
        $long = Get-FileHash -LiteralPath $file -Algorithm SHA512
        $narrow = $long | ConvertTo-TestEverydayText -Width 60
        # The Hash column's cells, joined: the second column of each body row.
        $cells = foreach ($line in $narrow -split '\r?\n') { $parts = $line -split '│'; if ($parts.Count -ge 4) { $parts[2].Trim() } }
        (-join $cells) | Should -Match $long.Hash
    }
}
