# Invented administration records for the typed everyday-admin capture. No host data is recorded.

# A working folder with one file to hash, at a fixed path so the capture does not depend on the machine.
$root = if ($IsWindows -or $PSVersionTable.PSEdition -eq 'Desktop') { Join-Path $env:TEMP 'everyday-admin' } else { '/tmp/everyday-admin' }
$null = New-Item -ItemType Directory -Path $root -Force
[IO.File]::WriteAllText((Join-Path $root 'release.zip'), 'ConsoleMonster example package')
Set-Location -LiteralPath $root

# Two folders that exist on every machine of this kind and two that do not.
$env:PATH = if ($IsWindows) {
    @("$env:SystemRoot\System32", 'C:\Tools\OldTool\bin', $env:SystemRoot, 'C:\Retired\Scripts') -join ';'
}
else { @('/usr/bin', '/opt/old-tool/bin', '/bin', '/usr/local/retired/bin') -join ':' }

function global:Get-CimInstance {
    param([Parameter(Position = 0)][string] $ClassName)
    $cim = 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2'
    switch ($ClassName) {
        'Win32_LogicalDisk' {
            foreach ($d in @(('C:', 'System', 3, 512GB, 31GB), ('D:', 'Data', 3, 2TB, 1.2TB), ('E:', 'Example USB', 2, 64GB, 50GB),
                    ('Z:', 'Team share', 4, 4TB, 0.9TB))) {
                [pscustomobject] @{ PSTypeName = "$cim/Win32_LogicalDisk"; DeviceID = $d[0]; VolumeName = $d[1]
                    DriveType = [uint32] $d[2]; FileSystem = 'NTFS'; Size = [uint64] $d[3]; FreeSpace = [uint64] $d[4] }
            }
        }
        'Win32_OperatingSystem' {
            [pscustomobject] @{ PSTypeName = "$cim/Win32_OperatingSystem"; Caption = 'Microsoft Windows 11 Pro'
                Version = '10.0.26100'; OSArchitecture = '64-bit'; CSName = 'EXAMPLE-PC'
                InstallDate = [datetime] '2025-03-14 10:12'; LastBootUpTime = [datetime] '2026-10-18 08:00'
                TotalVisibleMemorySize = [uint64] 33554432; FreePhysicalMemory = [uint64] 6291456 }
        }
    }
}

function global:Get-HotFix {
    foreach ($h in @(('KB5048667', 'Security Update', '2026-10-15'), ('KB5046617', 'Update', '2026-09-17'),
            ('KB5044284', 'Security Update', '2026-08-20'), ('KB5043080', 'Security Update', '2026-07-23'))) {
        # The type name goes on after the object exists: PowerShell's own type data makes InstalledOn read-only.
        $fix = [pscustomobject] @{ HotFixID = $h[0]; Description = $h[1]; InstalledOn = [datetime] $h[2]
            InstalledBy = 'NT AUTHORITY\SYSTEM'; CSName = 'EXAMPLE-PC' }
        $fix.PSObject.TypeNames.Insert(0, 'System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering')
        $fix
    }
}

function global:Get-WindowsOptionalFeature {
    param([switch] $Online)
    foreach ($f in @(('Microsoft-Windows-Subsystem-Linux', 'Enabled'), ('VirtualMachinePlatform', 'EnablePending'),
            ('TelnetClient', 'Disabled'), ('Microsoft-Hyper-V-All', 'Enabled'), ('SMB1Protocol', 'DisabledWithPayloadRemoved'))) {
        [pscustomobject] @{ PSTypeName = 'Microsoft.Dism.Commands.BasicFeatureObject'; FeatureName = $f[0]; State = $f[1] }
    }
}

function global:Get-NetFirewallRule {
    foreach ($r in @(('Remote Desktop - User Mode (TCP-In)', 'True', 'Inbound', 'Allow', 'Domain, Private', 'Remote Desktop'),
            ('File and Printer Sharing (SMB-In)', 'False', 'Inbound', 'Allow', 'Private', 'File and Printer Sharing'),
            ('Block legacy SMB', 'True', 'Inbound', 'Block', 'Any', ''),
            ('Example Agent (TCP-Out)', 'True', 'Outbound', 'Allow', 'Domain', 'Example Agent'),
            ('Windows Remote Management (HTTP-In)', 'True', 'Inbound', 'Allow', 'Domain', 'Windows Remote Management'))) {
        [pscustomobject] @{ PSTypeName = 'Microsoft.Management.Infrastructure.CimInstance#root/standardcimv2/MSFT_NetFirewallRule'
            DisplayName = $r[0]; Enabled = $r[1]; Direction = $r[2]; Action = $r[3]; Profile = $r[4]; DisplayGroup = $r[5] }
    }
}

function global:Invoke-WebRequest {
    param([Parameter(Position = 0)][string] $Uri)
    $headers = [Collections.Generic.Dictionary[string, string[]]]::new()
    $headers['Content-Type'] = @('application/json; charset=utf-8')
    $headers['Server'] = @('nginx')
    [pscustomobject] @{ PSTypeName = 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject'; StatusCode = 503
        StatusDescription = 'Service Unavailable'; Headers = $headers; RawContentLength = [long] 214
        Content = "{`n  `"status`": `"degraded`",`n  `"failing`": [`"database`"],`n  `"retryAfter`": 30`n}" }
}
