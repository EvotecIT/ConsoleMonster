function Get-EverydayViewSample {
    <# Synthetic, deterministic inputs for the default-host contracts and their review captures. #>
    param([string] $View)

    switch ($View) {
        'json-flat' {
            '[{"Name":"api-01","Count":12,"Ready":true},{"Name":"api-02","Count":2048,"Ready":false}]' | ConvertFrom-Json
        }
        'json-nested' {
            '{"Name":"api-01","Settings":{"Region":"west","Retry":{"Count":3}},"Tags":["blue","ready"],"Checks":[{"Name":"dns"},{"Name":"http"}],"Empty":[]}' | ConvertFrom-Json
        }
        'json-envelope' {
            '{"Total":3,"Items":[{"Name":"api-01","Region":"west","Ready":true},{"Name":"api-02","Region":"east","Ready":false},{"Name":"worker-01","Region":"west","Ready":true}],"Next":null}' | ConvertFrom-Json
        }
        'json-envelopes' {
            '{"Total":4,"Region":"west","Users":[{"Name":"Alex","Role":"Reader"},{"Name":"Sam","Role":"Editor"}],"Jobs":[{"Name":"backup","State":"Running"},{"Name":"inventory","State":"Ready"}]}' | ConvertFrom-Json
        }
        'json-budgets' {
            $items = 1..8 | ForEach-Object { [pscustomobject]@{ Name = "check-$_"; State = 'Ready' } }
            [pscustomobject]@{
                Name = 'payload'
                Tags = 1..10 | ForEach-Object { "tag-$_" }
                Settings = [pscustomobject]@{ Region = 'west'; Retry = $null }
                Empty = @()
                EmptyObject = [pscustomobject]@{}
                Deep = [pscustomobject]@{ Level2 = [pscustomobject]@{ Level3 = [pscustomobject]@{ A = 1; B = 2; C = 3; D = 4; E = 5; F = 6 } } }
                Message = 'Processing inventory for west region. ' * 8
                Checks = @($items)
            }
        }
        'json-records' {
            '[{"Name":"api-01","Checks":[{"Name":"dns","Ready":true},{"Name":"http","Ready":false}]},{"Name":"api-02","Checks":[{"Name":"dns","Ready":true}]}]' | ConvertFrom-Json
        }
        'json-empty-records' {
            '{"Total":2,"Items":[{},{}],"Mixed":[{"Name":"api-01"},{}]}' | ConvertFrom-Json
        }
        'json-large' {
            1..53 | ForEach-Object { [pscustomobject]@{ Name = "api-$_"; Count = $_ } }
        }
        'csv' {
            'Name,Count,Cost,When,Notes', 'api-01,0012,12.50,2026-10-24,ready', 'api-02,123456,-2.75,2026-10-25,waiting' | ConvertFrom-Csv
        }
        'csv-wide' {
            'Name,Count,Date,Owner,Region,Description,Ticket,Queue,Notes',
                'api-01,12,2026-10-24,Alex,west,Application deployment,12345,release,Ready for review',
                'api-02,2048,2026-10-25,Sam,east,Database maintenance,12346,backup,Waiting for backup' | ConvertFrom-Csv
        }
        'command' {
            foreach ($row in @(
                @{ Name = 'Get-Inventory'; CommandType = 'Function'; Version = [version]'1.2.0'; Source = 'Demo.Inventory'; ModuleName = 'Demo.Inventory' }
                @{ Name = 'Get-Date'; CommandType = 'Cmdlet'; Version = [version]'7.0.0.0'; Source = 'Microsoft.PowerShell.Utility'; ModuleName = 'Microsoft.PowerShell.Utility' }
                @{ Name = 'gi'; Definition = 'Get-Inventory'; CommandType = 'Alias'; Source = ''; ModuleName = '' }
                @{ Name = 'tool'; CommandType = 'Application'; Version = [version]'0.0.0.0'; Source = '/usr/bin/tool'; ModuleName = '' }
            )) {
                $item = [pscustomobject] $row
                $item.PSObject.TypeNames.Insert(0, "System.Management.Automation.$($row.CommandType)Info")
                $item
            }
        }
        'command-grouped' {
            1..12 | ForEach-Object {
                $item = [pscustomobject]@{
                    Name = "Get-Item$_"
                    CommandType = 'Function'
                    Version = [version]'1.2.0'
                    Source = if ($_ -le 6) { 'Demo.Inventory' } else { 'Demo.Backup' }
                    ModuleName = if ($_ -le 6) { 'Demo.Inventory' } else { 'Demo.Backup' }
                }
                $item.PSObject.TypeNames.Insert(0, 'System.Management.Automation.FunctionInfo')
                $item
            }
        }
        'module' {
            foreach ($version in '1.2.0', '2.0.0', '2.1.0') {
                $item = [pscustomobject]@{
                    Name = 'Demo.Inventory'
                    Version = [version] $version
                    ModuleType = 'Script'
                    ExportedCommands = @{ 'Get-Inventory' = $null; 'Test-Inventory' = $null }
                    Path = if ($version -eq '2.1.0') { "~/Documents/PowerShell/Modules/Demo.Inventory/$version/Demo.Inventory.psm1" }
                        else { "/opt/powershell/Modules/Demo.Inventory/$version/Demo.Inventory.psm1" }
                }
                $item.PSObject.TypeNames.Insert(0, 'System.Management.Automation.PSModuleInfo')
                $item
            }
        }
        'history' {
            foreach ($id in 101, 102) {
                $item = [pscustomobject]@{
                    Id = $id
                    StartExecutionTime = [datetime]'2026-10-24T10:20:30'
                    EndExecutionTime = [datetime]'2026-10-24T10:20:32'
                    ExecutionStatus = if ($id -eq 101) { 'Completed' } else { 'Failed' }
                    CommandLine = if ($id -eq 101) { 'Get-Inventory -Region "west" | Select-Object Name, Count' }
                        else { 'Invoke-Inventory -ComputerName "api-02" -OutputPath "/reports/very-long-report-path/inventory.json" -ErrorAction Stop' }
                }
                $item.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.HistoryInfo')
                $item
            }
        }
        'ping' {
            foreach ($status in 'Success', 'TimedOut', 'DestinationHostUnreachable') {
                $item = [pscustomobject]@{ Destination = 'api-01'; Ping = 1; Address = '192.0.2.10'; Latency = 12; Status = $status }
                $item.PSObject.TypeNames.Insert(0, 'Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus')
                $item
            }
        }
        'ip' { [Net.IPAddress]::Parse('192.0.2.10'); [Net.IPAddress]::Parse('2001:db8::10') }
        'dns-host' {
            $hostEntry = [Net.IPHostEntry]::new()
            $hostEntry.HostName = 'api.example.test'
            $hostEntry.AddressList = @([Net.IPAddress]::Parse('192.0.2.10'), [Net.IPAddress]::Parse('2001:db8::10'))
            $hostEntry.Aliases = @('api-01')
            $hostEntry
        }
        'dns-record' {
            foreach ($kind in 'A', 'MX', 'TXT', 'SRV') {
                $item = [pscustomobject]@{
                    Name = 'example.test'
                    Type = $kind
                    TTL = 300
                    IPAddress = if ($kind -eq 'A') { '192.0.2.10' }
                    NameExchange = if ($kind -eq 'MX') { 'mail.example.test' }
                    Preference = 10
                    Strings = if ($kind -eq 'TXT') { @('v=spf1', '-all') }
                    NameTarget = if ($kind -eq 'SRV') { 'ldap.example.test' }
                    Port = 389
                }
                $item.PSObject.TypeNames.Insert(0, "Microsoft.DnsClient.Commands.DnsRecord_$kind")
                $item
            }
        }
        'net-ip' {
            $item = [pscustomobject]@{ InterfaceAlias = 'Ethernet'; IPAddress = '192.0.2.10'; PrefixLength = 24; AddressFamily = 'IPv4'; AddressState = 'Preferred' }
            $item.PSObject.TypeNames.Insert(0, 'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetIPAddress')
            $item
        }
        'net-adapter' {
            foreach ($status in 'Up', 'Disconnected') {
                $item = [pscustomobject]@{ Name = 'Ethernet'; Status = $status; MacAddress = '00-00-5E-00-53-01'; LinkSpeed = '1 Gbps'; InterfaceDescription = 'Synthetic network adapter' }
                $item.PSObject.TypeNames.Insert(0, 'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetAdapter')
                $item
            }
        }
        'net-connection' {
            foreach ($success in $true, $false) {
                $item = [pscustomobject]@{ ComputerName = 'api.example.test'; RemoteAddress = [Net.IPAddress]::Parse('192.0.2.10'); RemotePort = 443; InterfaceAlias = 'Ethernet'; PingSucceeded = $success; TcpTestSucceeded = $success }
                $item.PSObject.TypeNames.Insert(0, 'NetConnectionResults')
                $item
            }
        }
    }
}
