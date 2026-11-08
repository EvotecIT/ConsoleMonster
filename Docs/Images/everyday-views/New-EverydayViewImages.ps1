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
if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
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
