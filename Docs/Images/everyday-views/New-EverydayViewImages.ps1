<#
.SYNOPSIS
Draws the everyday view images in this folder: the summary lines and the recursive file tree.

.DESCRIPTION
Each view runs on a headless terminal through the console view that Enable-ConsoleFormatting uses, so the images are
ConsoleMonster's own output. The records are invented and fixed, so the images only change when the views do.

.EXAMPLE
pwsh Docs/Images/everyday-views/New-EverydayViewImages.ps1
#>
param([string] $OutputPath = $PSScriptRoot)

Import-Module (Join-Path $PSScriptRoot '../../../Module/ConsoleMonster.psd1') -Force

function Save-View {
    param([string] $Name, [string] $Title, [scriptblock] $Objects, [int] $Width = 96, [int] $Height = 60)
    $screen = Invoke-ConsoleHeadless -Width $Width -Height $Height -ArgumentList $Objects -ScriptBlock {
        param($objects)
        $type = (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault')
        $console = [Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', $type)
        & $objects | & $console -Force
    }
    $path = Join-Path $OutputPath ($Name + '.png')
    $screen.Cells | Export-ConsoleFrame -Path $path -Format Png -Preset Social -Fit Content -Theme Dark -Title $Title -ChromeStyle MacOS
    $path
}

function New-Record([string] $Type, [hashtable] $Data) {
    $record = [pscustomobject] $Data
    $record.PSObject.TypeNames.Insert(0, $Type)
    $record
}

# A release folder with fixed sizes and times.
$temp = if ($IsMacOS -or $IsLinux) { '/tmp' } else { $env:TEMP }
$root = Join-Path $temp 'release'
# The fixed path shows in the image; an existing folder is never deleted, so the script stops instead.
if (Test-Path -LiteralPath $root) { throw "Remove '$root' before drawing the images. The script only deletes folders it created." }
$files = [ordered] @{
    'bin/ConsoleMonster.dll' = 1048576; 'bin/ConsoleMonster.xml' = 262144; 'bin/runtimes/libcm.so' = 393216
    'docs/README.md' = 15360; 'docs/CHANGELOG.md' = 63488; 'install.ps1' = 2048
}
foreach ($name in $files.Keys) {
    $path = Join-Path $root $name
    $null = New-Item -ItemType Directory -Path (Split-Path $path) -Force
    [System.IO.File]::WriteAllBytes($path, (New-Object byte[] $files[$name]))
}
$stamp = [datetime] '2026-10-21 12:00'
Get-ChildItem $root -Recurse -Force | ForEach-Object { $_.LastWriteTime = $stamp }
(Get-Item -LiteralPath $root).LastWriteTime = $stamp
Save-View 'files-tree' 'Get-ChildItem -Recurse' { Get-ChildItem -LiteralPath $root -Recurse } -Width 72
Remove-Item -LiteralPath $root -Recurse -Force

# A project folder with one file of each kind, so every name colour shows.
$root = Join-Path $temp 'project'
# The fixed path shows in the image; an existing folder is never deleted, so the script stops instead.
if (Test-Path -LiteralPath $root) { throw "Remove '$root' before drawing the images. The script only deletes folders it created." }
$null = New-Item -ItemType Directory -Path (Join-Path $root 'src'), (Join-Path $root 'assets') -Force
$files = [ordered] @{
    '.gitignore' = 120; 'build.ps1' = 4096; 'Program.cs' = 2210; 'release.zip' = 5242880; 'logo.png' = 48211
    'debug.log' = 91234; 'README.md' = 15360; 'settings.json' = 512; 'setup.exe' = 3145728
}
foreach ($name in $files.Keys) { [System.IO.File]::WriteAllBytes((Join-Path $root $name), (New-Object byte[] $files[$name])) }
if ($IsMacOS -or $IsLinux) { & chmod +x (Join-Path $root 'setup.exe') }
Get-ChildItem $root -Force | ForEach-Object { $_.LastWriteTime = $stamp }
# On macOS and Linux the listing shows the owner; the image shows an example name instead of whoever draws it.
Save-View 'files' 'Get-ChildItem -Force' {
    foreach ($type in 'System.IO.FileInfo', 'System.IO.DirectoryInfo') {
        Update-TypeData -TypeName $type -MemberType ScriptProperty -MemberName User -Value { 'example' } -Force
    }
    Get-ChildItem -LiteralPath $root -Force
} -Width 90
Remove-Item -LiteralPath $root -Recurse -Force

Save-View 'processes' 'Get-Process' {
    $processes = @(('pwsh', 412MB, 61.2), ('dotnet', 1.9GB, 812.4), ('Code', 980MB, 344.9), ('sqlservr', 2.6GB, 1290.5),
        ('w3wp', 640MB, 220.1), ('explorer', 210MB, 95.3), ('OneDrive', 160MB, 40.8))
    $id = 4100
    foreach ($p in $processes) {
        $id += 317
        New-Record 'System.Diagnostics.Process' @{ Id = $id; ProcessName = $p[0]; WorkingSet64 = [long] $p[1]; CPU = $p[2]
            SessionId = 1; Name = $p[0]; VirtualMemorySize64 = [long] $p[1] * 3; PagedMemorySize64 = [long] $p[1]
            PrivateMemorySize64 = [long] $p[1]; NonpagedSystemMemorySize64 = 65536; PagedSystemMemorySize64 = 262144; Handles = 900 + $id % 700; Threads = @(1..(8 + $id % 20)) }
    }
}

Save-View 'services' 'Get-Service' {
    foreach ($s in @(('Running', 'Automatic', 'Dnscache', 'DNS Client'), ('Running', 'Automatic', 'EventLog', 'Windows Event Log'),
            ('Stopped', 'Automatic', 'Spooler', 'Print Spooler'), ('Running', 'Automatic', 'W32Time', 'Windows Time'),
            ('Stopped', 'Manual', 'wuauserv', 'Windows Update'), ('Running', 'Automatic', 'WinRM', 'Windows Remote Management'),
            ('Stopped', 'Automatic', 'MSSQLSERVER', 'SQL Server (MSSQLSERVER)'), ('Stopped', 'Disabled', 'Fax', 'Fax'))) {
        New-Record 'System.ServiceProcess.ServiceController' @{ Status = $s[0]; StartType = $s[1]; Name = $s[2]; DisplayName = $s[3]
            ServicesDependedOn = @(); DependentServices = @(); CanPauseAndContinue = $false }
    }
}

Save-View 'events' 'Get-WinEvent -LogName System' {
    $now = [datetime] '2026-11-08 21:00'
    $messages = @{ Error = 'The SQL Server (MSSQLSERVER) service terminated unexpectedly.'
        Warning = 'The time service has not synchronized the system time for 86400 seconds.'
        Information = 'The Windows Update service entered the running state.' }
    foreach ($i in 0..13) {
        $level = if ($i -in 1, 2) { 'Error' } elseif ($i % 4 -eq 0) { 'Warning' } else { 'Information' }
        New-Record 'System.Diagnostics.Eventing.Reader.EventLogRecord' @{ TimeCreated = $now.AddMinutes(-[math]::Pow($i, 2) * 2)
            Id = @{ Error = 7034; Warning = 129; Information = 7036 }[$level]; LevelDisplayName = $level
            ProviderName = 'Service Control Manager'; Message = $messages[$level] }
    }
} -Width 140

Save-View 'ping' 'Test-Connection' {
    foreach ($target in @(('example.com', '93.184.215.14', @(18, 21, 19, 34, 20, 18)), ('fileserver.contoso.test', '10.0.4.20', @(2, 3, 0, 2, 9, 2)))) {
        $n = 0
        foreach ($latency in $target[2]) {
            $n++
            New-Record 'Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus' @{ Destination = $target[0]; Ping = $n
                Address = $target[1]; Latency = [long] $latency; Status = $(if ($latency) { 'Success' } else { 'TimedOut' }) }
        }
    }
}

Save-View 'history' 'Get-History' {
    $start = [datetime] '2026-11-08 09:00'
    $commands = @(('Get-ChildItem C:\Logs -Recurse -Filter *.log', 4.2), ('Import-Csv .\users.csv | Measure-Object', 0.4),
        ('Invoke-Pester .\Tests -Output Detailed', 71.5), ('Get-ADUser -Filter * -Properties LastLogonDate', 12.8),
        ('Restart-Service Spooler', 3.1), ('Test-Connection fileserver -Count 6', 5.4))
    $id = 0
    foreach ($c in $commands) {
        $id++
        New-Record 'Microsoft.PowerShell.Commands.HistoryInfo' @{ Id = $id; CommandLine = $c[0]
            StartExecutionTime = $start.AddMinutes($id * 3); EndExecutionTime = $start.AddMinutes($id * 3).AddSeconds($c[1])
            ExecutionStatus = $(if ($id -eq 4) { 'Failed' } else { 'Completed' }) }
    }
}

Register-ConsoleView -TypeName Contoso.Server -Property Name,
    @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' },
    @{ Name = 'Disk'; Expression = { $_.Disk }; Presenter = 'BytesBar' } -Summary {
    param($servers)
    @{ Value = $servers.Count; Label = 'servers' }
    $down = @($servers | Where-Object State -ne 'Running').Count
    if ($down) { @{ Value = $down; Label = 'stopped'; Role = 'Warn' } }
    @{ Caption = 'disk'; Value = ($servers | Measure-Object Disk -Sum).Sum; Presenter = 'Bytes' }
}
Save-View 'custom-summary' 'Register-ConsoleView -Summary' {
    foreach ($s in @(('web-01', 'Running', 120GB), ('web-02', 'Running', 96GB), ('db-01', 'Stopped', 410GB))) {
        New-Record 'Contoso.Server' @{ Name = $s[0]; State = $s[1]; Disk = $s[2] }
    }
} -Width 60

# Everyday administration commands.
Save-View 'env-path' 'Get-ChildItem Env:PATH, Env:HOME' {
    $separator = [IO.Path]::PathSeparator
    # Two folders that exist on the machine drawing the image and two that do not.
    $system = if ($IsMacOS -or $IsLinux) { '/usr/bin' } else { $env:SystemRoot }
    $missing = if ($IsMacOS -or $IsLinux) { '/opt/old-tool/bin', '/usr/local/retired/scripts' } else { 'C:\Tools\OldTool\bin', 'C:\Retired\Scripts' }
    $path = [Collections.DictionaryEntry]::new('PATH', (@($PSHOME, $missing[0], $system, $missing[1]) -join $separator))
    $homeEntry = [Collections.DictionaryEntry]::new('HOME', '/home/example')
    foreach ($entry in $path, $homeEntry) { $item = [psobject] $entry; $item | Add-Member PSDrive 'Env'; $item }
} -Width 80

Save-View 'event-log' 'Get-EventLog -LogName System -Newest 6' {
    $now = [datetime] '2026-11-08 21:00'
    $entries = @(('Error', 7034, 'The SQL Server (MSSQLSERVER) service terminated unexpectedly.'), ('Warning', 129, 'The time service has not synchronized the system time.'),
        ('Information', 7036, 'The Windows Update service entered the running state.'), ('Information', 7040, 'The start type of the BITS service was changed.'),
        ('Error', 7000, 'The Example Agent service failed to start.'), ('Information', 7036, 'The Print Spooler service entered the stopped state.'))
    $i = 0
    foreach ($e in $entries) {
        $i++
        New-Record 'System.Diagnostics.EventLogEntry' @{ TimeGenerated = $now.AddMinutes(-$i * $i * 7); InstanceId = [long] $e[1]; EntryType = $e[0]
            Source = 'Service Control Manager'; Message = $e[2] }
    }
} -Width 120

Save-View 'logical-disks' 'Get-CimInstance Win32_LogicalDisk' {
    foreach ($d in @(('C:', 'System', 3, 512GB, 31GB), ('D:', 'Data', 3, 2TB, 1.2TB), ('E:', 'USB', 2, 64GB, 50GB), ('Z:', 'Team share', 4, 4TB, 0.9TB))) {
        New-Record 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_LogicalDisk' @{ DeviceID = $d[0]; VolumeName = $d[1]
            DriveType = [uint32] $d[2]; FileSystem = 'NTFS'; Size = [uint64] $d[3]; FreeSpace = [uint64] $d[4] }
    }
} -Width 96

Save-View 'operating-system' 'Get-CimInstance Win32_OperatingSystem' {
    New-Record 'Microsoft.Management.Infrastructure.CimInstance#root/cimv2/Win32_OperatingSystem' @{ Caption = 'Microsoft Windows 11 Pro'
        Version = '10.0.26100'; OSArchitecture = '64-bit'; CSName = 'EXAMPLE-PC'; InstallDate = [datetime] '2025-03-14 10:12'
        LastBootUpTime = (Get-Date).AddDays(-3).AddHours(-4); TotalVisibleMemorySize = [uint64] 33554432; FreePhysicalMemory = [uint64] 6291456 }
} -Width 72

Save-View 'hotfixes' 'Get-HotFix' {
    $today = (Get-Date).Date
    foreach ($h in @(('KB5048667', 'Security Update', 6), ('KB5046617', 'Update', 34), ('KB5044284', 'Security Update', 62), ('KB5043080', 'Security Update', 90))) {
        New-Record 'System.Management.ManagementObject#root\cimv2\Win32_QuickFixEngineering' @{ HotFixID = $h[0]; Description = $h[1]
            InstalledOn = $today.AddDays(-$h[2]); InstalledBy = 'NT AUTHORITY\SYSTEM'; CSName = 'EXAMPLE-PC' }
    }
} -Width 96

Save-View 'optional-features' 'Get-WindowsOptionalFeature -Online' {
    foreach ($f in @(('Microsoft-Windows-Subsystem-Linux', 'Enabled'), ('VirtualMachinePlatform', 'EnablePending'), ('TelnetClient', 'Disabled'),
            ('Microsoft-Hyper-V-All', 'Enabled'), ('SMB1Protocol', 'DisabledWithPayloadRemoved'))) {
        New-Record 'Microsoft.Dism.Commands.BasicFeatureObject' @{ FeatureName = $f[0]; State = $f[1] }
    }
} -Width 80

Save-View 'firewall-rules' 'Get-NetFirewallRule' {
    foreach ($r in @(('Remote Desktop - User Mode (TCP-In)', 'True', 'Inbound', 'Allow', 'Domain, Private', 'Remote Desktop'),
            ('File and Printer Sharing (SMB-In)', 'False', 'Inbound', 'Allow', 'Private', 'File and Printer Sharing'),
            ('Block legacy SMB', 'True', 'Inbound', 'Block', 'Any', ''), ('Example Agent (TCP-Out)', 'True', 'Outbound', 'Allow', 'Domain', 'Example Agent'),
            ('Windows Remote Management (HTTP-In)', 'True', 'Inbound', 'Allow', 'Domain', 'Windows Remote Management'))) {
        New-Record 'Microsoft.Management.Infrastructure.CimInstance#root/standardcimv2/MSFT_NetFirewallRule' @{ DisplayName = $r[0]; Enabled = $r[1]
            Direction = $r[2]; Action = $r[3]; Profile = $r[4]; DisplayGroup = $r[5] }
    }
} -Width 120

Save-View 'registry' 'Get-ChildItem HKLM:\SOFTWARE\Example' {
    foreach ($k in @(('Agent', 2, [ordered] @{ '' = 'Example Agent'; InstallPath = 'C:\Program Files\Example\Agent'; Version = '4.2.1'; Port = 8443 }),
            ('Updater', 0, [ordered] @{ Channel = 'stable'; LastCheck = '2026-11-08T06:00:00Z'; Enabled = 1; Proxy = ''; Retries = 3; Timeout = 30; Mirror = 'https://updates.example.com' }))) {
        $values = $k[2]
        $key = New-Record 'Microsoft.Win32.RegistryKey' @{ PSChildName = $k[0]; SubKeyCount = $k[1]
            PSParentPath = 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Example' }
        $key | Add-Member ScriptMethod GetValueNames { [string[]] $values.Keys }.GetNewClosure()
        $key | Add-Member ScriptMethod GetValue { param($name, $default, $options) $values[$name] }.GetNewClosure()
        $key
    }
} -Width 90

Save-View 'web-request' 'Invoke-WebRequest https://example.com/status' {
    $headers = [Collections.Generic.Dictionary[string, string[]]]::new()
    $headers['Content-Type'] = @('application/json; charset=utf-8'); $headers['Server'] = @('nginx')
    New-Record 'Microsoft.PowerShell.Commands.BasicHtmlWebResponseObject' @{ StatusCode = 503; StatusDescription = 'Service Unavailable'
        Headers = $headers; RawContentLength = [long] 214; Content = "{`n  `"status`": `"degraded`",`n  `"failing`": [`"database`"],`n  `"retryAfter`": 30`n}" }
} -Width 80

Save-View 'file-hash' 'Get-FileHash .\release.zip, .\install.ps1' {
    foreach ($f in @(('release.zip', '9F2C4A1E7B3D5068C1A4E2F9B7D30C58A6E1F4B2D9C7A3E5F1B8D2C4A6E9F0B3'), ('install.ps1', '1B7E9C3A5D2F4086B1C3E5A7D9F2B4C6E8A1D3F5B7C9E2A4D6F8B1C3E5A7D9F2'))) {
        New-Record 'Microsoft.PowerShell.Commands.FileHashInfo' @{ Algorithm = 'SHA256'; Hash = $f[1]; Path = Join-Path $PWD.ProviderPath $f[0] }
    }
} -Width 110
