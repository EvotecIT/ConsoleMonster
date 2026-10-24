# Trusted built-in definitions. Native Format-* still owns its selected columns and expressions.
@{
    TypeName = 'Microsoft.PowerShell.Commands.MemberDefinition'
    Group = 'Member'
    GroupBy = 'MemberType'
    Title = { "TypeName: $($_.TypeName)" }
    GroupHeadersAsRows = $true
    Property = @(
        'Name'
        @{ Name = 'MemberType'; Expression = { [string] $_.MemberType } }
        @{ Name = 'Definition'; Expression = 'Definition'; Presenter = 'MemberSignature'; MaxLines = 0 }
    )
}
@{
    TypeName = @(
        'System.Management.Automation.CmdletInfo'
        'System.Management.Automation.FunctionInfo'
        'System.Management.Automation.FilterInfo'
        'System.Management.Automation.AliasInfo'
        'System.Management.Automation.ApplicationInfo'
        'System.Management.Automation.ExternalScriptInfo'
        'System.Management.Automation.ConfigurationInfo'
    )
    Group = 'Command'
    GroupBy = 'ModuleName'
    GroupMinimumRows = 12
    Property = @(
        @{ Label = 'Name'; Expression = { if ($_.CommandType -eq 'Alias') { $_.Name + ' -> ' + $_.Definition } else { $_.Name } }; Priority = 10 }
        @{ Label = 'CommandType'; Expression = { [string] $_.CommandType }; Presenter = 'Chip'; Priority = 8 }
        @{ Name = 'Version'; Expression = { if ($_.Version -and $_.Version -ne [version] '0.0' -and $_.Version -ne [version] '0.0.0.0') { [string] $_.Version } } }
        'Source'
    )
}
@{
    TypeName = 'System.Management.Automation.PSModuleInfo'
    Group = 'Module'
    GroupBy = 'Name'
    GroupRepeatedOnly = $true
    Property = @(
        @{ Label = 'Name'; Expression = 'Name'; Priority = 10 }
        @{ Label = 'Version'; Expression = { [string] $_.Version }; Priority = 8 }
        @{ Label = 'ModuleType'; Expression = { [string] $_.ModuleType } }
        @{ Label = 'Commands'; Expression = { $_.ExportedCommands.Count }; Presenter = 'Identifier' }
        @{ Label = 'Path'; Expression = { [ConsoleMonster.Text.DisplayPath]::Short([string] $_.Path, $HOME) }; Overflow = 'TruncateMiddle' }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.HistoryInfo'
    Group = 'History'
    Property = @(
        @{ Name = 'Id'; Expression = 'Id'; Presenter = 'Identifier' }
        @{ Label = 'Started'; Expression = { $_.StartExecutionTime.ToString('HH:mm:ss') }; Alignment = 'Right' }
        @{ Name = 'Duration'; Expression = { $_.EndExecutionTime - $_.StartExecutionTime }; Presenter = 'Duration' }
        @{ Name = 'Status'; Expression = { [string] $_.ExecutionStatus }; Presenter = 'StatusWord' }
        @{ Label = 'CommandLine'; Expression = { $_.CommandLine -replace '[\r\n]+', ' ' }; Highlight = 'PowerShell'; MaxLines = 1; Overflow = 'Truncate'; Priority = 10 }
    )
}

# JSON, REST and CSV all arrive as PSCustomObject; keep the originals and infer only their displayed alignment.
@{
    TypeName = 'System.Management.Automation.PSCustomObject'
    Group = 'StructuredData'
    StructuredData = $true
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.TestConnectionCommand+PingStatus'
    Group = 'Ping'
    Property = @(
        @{ Label = 'Destination'; Expression = 'Destination'; Priority = 10 }
        @{ Label = 'Reply'; Expression = 'Ping'; Presenter = 'Identifier' }
        @{ Label = 'Address'; Expression = { [string] $_.Address } }
        @{ Label = 'Latency (ms)'; Expression = { if ([string] $_.Status -eq 'Success') { $_.Latency } }; Alignment = 'Right' }
        @{ Label = 'Status'; Expression = { [string] $_.Status }; Presenter = 'Text'; Priority = 8
            Tone = @(
                @{ Equals = 'Success'; Tone = 'Pass' }
                foreach ($status in 'Unknown', 'DestinationNetworkUnreachable', 'DestinationHostUnreachable', 'DestinationProtocolUnreachable',
                    'DestinationPortUnreachable', 'NoResources', 'BadOption', 'HardwareError', 'PacketTooBig', 'TimedOut',
                    'BadRoute', 'TtlExpired', 'TtlReassemblyTimeExceeded', 'ParameterProblem', 'SourceQuench', 'BadDestination',
                    'DestinationUnreachable', 'TimeExceeded', 'BadHeader', 'UnrecognizedNextHeader', 'IcmpError',
                    'DestinationScopeMismatch') {
                    @{ Equals = $status; Tone = 'Fail' }
                }
            )
        }
    )
}
@{
    TypeName = 'System.Net.IPAddress'
    Group = 'IPAddress'
    Property = @(
        @{ Label = 'Address'; Expression = { $_.ToString() }; Priority = 10 }
        @{ Label = 'Family'; Expression = { [string] $_.AddressFamily } }
    )
}
@{
    TypeName = 'System.Net.IPHostEntry'
    Group = 'DnsHost'
    Property = @(
        @{ Label = 'HostName'; Expression = 'HostName'; Priority = 10 }
        @{ Label = 'Addresses'; Expression = { ($_.AddressList | ForEach-Object { $_.ToString() }) -join ', ' } }
        @{ Label = 'Aliases'; Expression = { $_.Aliases -join ', ' } }
    )
}
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    @{
        TypeName = @(
            'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetIPAddress'
            'Microsoft.Management.Infrastructure.CimInstance#MSFT_NetIPAddress'
        )
        Group = 'NetIPAddress'
        Property = @(
            @{ Label = 'Interface'; Expression = 'InterfaceAlias'; Priority = 10 }
            @{ Label = 'Address'; Expression = 'IPAddress'; Priority = 9 }
            @{ Label = 'Prefix'; Expression = 'PrefixLength' }
            @{ Label = 'Family'; Expression = { [string] $_.AddressFamily } }
            @{ Label = 'State'; Expression = { [string] $_.AddressState }; Presenter = 'StatusWord' }
        )
    }
    @{
        TypeName = @(
            'Microsoft.Management.Infrastructure.CimInstance#ROOT/StandardCimv2/MSFT_NetAdapter'
            'Microsoft.Management.Infrastructure.CimInstance#MSFT_NetAdapter'
        )
        Group = 'NetAdapter'
        Property = @(
            @{ Label = 'Name'; Expression = 'Name'; Priority = 10 }
            @{ Label = 'Status'; Expression = { [string] $_.Status }; Presenter = 'StatusWord'; Priority = 8 }
            'MacAddress'
            'LinkSpeed'
            @{ Label = 'Description'; Expression = 'InterfaceDescription'; Overflow = 'Truncate' }
        )
    }
    @{
        TypeName = @(
            'Microsoft.DnsClient.Commands.DnsRecord'
            'Microsoft.DnsClient.Commands.DnsRecord_A'
            'Microsoft.DnsClient.Commands.DnsRecord_AAAA'
            'Microsoft.DnsClient.Commands.DnsRecord_CNAME'
            'Microsoft.DnsClient.Commands.DnsRecord_MX'
            'Microsoft.DnsClient.Commands.DnsRecord_NS'
            'Microsoft.DnsClient.Commands.DnsRecord_PTR'
            'Microsoft.DnsClient.Commands.DnsRecord_SRV'
            'Microsoft.DnsClient.Commands.DnsRecord_SOA'
            'Microsoft.DnsClient.Commands.DnsRecord_TXT'
        )
        Group = 'DnsRecord'
        Property = @(
            @{ Label = 'Name'; Expression = 'Name'; Priority = 10 }
            @{ Label = 'Type'; Expression = { [string] $_.Type } }
            'TTL'
            @{ Label = 'Answer'; Expression = {
                $record = $_
                switch ([string] $record.Type) {
                    'A' { $record.IPAddress }
                    'AAAA' { $record.IPAddress }
                    'CNAME' { $record.NameHost }
                    'NS' { $record.NameHost }
                    'PTR' { $record.NameHost }
                    'MX' { [string] $record.Preference + ' ' + $record.NameExchange }
                    'TXT' { $record.Strings -join ' ' }
                    'SRV' { [string] $record.NameTarget + ':' + $record.Port }
                    'SOA' { $record.PrimaryServer }
                }
            }; Priority = 9; MaxLines = 2 }
        )
    }
    @{
        TypeName = 'NetConnectionResults'
        Group = 'NetConnection'
        Property = @(
            @{ Label = 'ComputerName'; Expression = 'ComputerName'; Priority = 10 }
            @{ Label = 'RemoteAddress'; Expression = { [string] $_.RemoteAddress } }
            'RemotePort'
            @{ Label = 'Interface'; Expression = 'InterfaceAlias' }
            @{
                Label = 'Ping'
                Expression = {
                    $result = $_.PSObject.Properties['PingSucceeded']
                    if ($result -and $null -ne $result.Value) {
                        if ($result.Value) { 'Success' } else { 'Failed' }
                    }
                }
                Presenter = 'StatusWord'
            }
            @{
                Label = 'TCP'
                Expression = {
                    $result = $_.PSObject.Properties['TcpTestSucceeded']
                    if ($result -and $null -ne $result.Value) {
                        if ($result.Value) { 'Success' } else { 'Failed' }
                    }
                }
                Presenter = 'StatusWord'
            }
        )
    }
}
