# Trusted built-in definitions. Native Format-* still owns its selected columns and expressions.
@{
    TypeName = 'Microsoft.PowerShell.Commands.MemberDefinition'
    Group = 'Member'
    GroupBy = 'MemberType'
    Title = { "TypeName: $($_.TypeName)" }
    GroupHeadersAsRows = $true
    Property = @(
        'Name'
        @{ Label = 'MemberType'; Expression = { [string] $_.MemberType } }
        @{ Label = 'Definition'; Expression = 'Definition'; Presenter = 'MemberSignature'; MaxLines = 0 }
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
        @{ Label = 'CommandType'; Expression = { if ($_.CommandType -eq 'ExternalScript') { 'Script' } else { [string] $_.CommandType } }; Presenter = 'Chip'; Priority = 8 }
        @{ Label = 'Version'; Expression = { if ($_.Version -and $_.Version -ne [version] '0.0' -and $_.Version -ne [version] '0.0.0.0') { [string] $_.Version } } }
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
        @{ Label = 'Path'; Expression = {
            $displayPath = [string] $_.Path
            $path = $displayPath.Replace('\', '/')
            $homeRoot = $HOME.Replace('\', '/').TrimEnd('/')
            $comparison = if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
            if ($path.StartsWith('~/')) {
                'User ' + $displayPath
            } elseif ($homeRoot -and $path.StartsWith($homeRoot + '/', $comparison)) {
                'User ~' + $displayPath.Substring($homeRoot.Length)
            } else {
                $systemRoots = @($PSHOME, '/usr/local/share/powershell', '/usr/share/powershell', '/opt', '/Library/PowerShell', '/System/Library/PowerShell',
                    [Environment]::GetFolderPath('ProgramFiles'), [Environment]::GetFolderPath('Windows'))
                $system = $false
                foreach ($root in $systemRoots) {
                    if ($root -and $path.StartsWith($root.Replace('\', '/').TrimEnd('/') + '/', $comparison)) { $system = $true; break }
                }
                if ($system) { 'System ' + $displayPath } else { $displayPath }
            }
        }; Overflow = 'TruncateMiddle'; Priority = 7 }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.HistoryInfo'
    Group = 'History'
    Property = @(
        @{ Label = 'Id'; Expression = 'Id'; Presenter = 'Identifier' }
        @{ Label = 'Started'; Expression = { $_.StartExecutionTime.ToString('HH:mm:ss') }; Alignment = 'Right' }
        @{ Label = 'Duration'; Expression = { $_.EndExecutionTime - $_.StartExecutionTime }; Presenter = 'Duration' }
        @{ Label = 'Status'; Expression = { ([string] $_.ExecutionStatus) -replace 'NotStarted', 'Not started' -replace 'AtBreakpoint', 'At breakpoint' }; Presenter = 'StatusWord' }
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
        @{ Label = 'Status'; Expression = {
            $name = ([string] $_.Status) -replace '^Destination', '' -replace '^Ttl', 'TTL' -replace '^Icmp', 'ICMP'
            $words = ($name -creplace '(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])', ' ') -split ' ', 2
            if ($words.Count -gt 1) { $words[0] + ' ' + $words[1].ToLowerInvariant() } else { $words[0] }
        }; Presenter = 'Text'; Priority = 8
            Role = @(
                @{ Equals = 'Success'; Role = 'Pass' }
                foreach ($status in 'Unknown', 'Network unreachable', 'Host unreachable', 'Protocol unreachable',
                    'Port unreachable', 'No resources', 'Bad option', 'Hardware error', 'Packet too big', 'Timed out',
                    'Bad route', 'TTL expired', 'TTL reassembly time exceeded', 'Parameter problem', 'Source quench', 'Bad destination',
                    'Unreachable', 'Time exceeded', 'Bad header', 'Unrecognized next header', 'ICMP error', 'Scope mismatch') {
                    @{ Equals = $status; Role = 'Fail' }
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
        @{ Label = 'Family'; Expression = {
            switch ([string] $_.AddressFamily) { { $_ -in 'InterNetwork', '2', 'IPv4' } { 'IPv4' } { $_ -in 'InterNetworkV6', '23', 'IPv6' } { 'IPv6' } default { $_ } }
        } }
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
# Select-String: one display row per matched or -Context line, grouped by file, with the matched text highlighted.
@{
    TypeName = 'Microsoft.PowerShell.Commands.MatchInfo'
    RowTypeName = 'ConsoleMonster.Everyday.MatchLine'
    Group = 'MatchInfo'
    Rows = {
        $match = $_
        $file = ''
        if ($match.Path -and $match.Path -ne 'InputStream') {
            $file = $match.Path
            if ($PWD.Provider.Name -eq 'FileSystem') { try { $file = $match.RelativePath($PWD.ProviderPath) } catch { } }
        }
        # Literal text in markup: brackets doubled, as Markup.Escape does.
        $tidy = { param($text) ([string] $text).Replace("`t", '    ').Replace('[', '[[').Replace(']', ']]') }
        $line = [string] $match.Line
        $marked = [Text.StringBuilder]::new()
        $at = 0
        foreach ($found in @($match.Matches)) {
            if ($null -eq $found -or $found.Length -le 0 -or $found.Index -lt $at -or $found.Index + $found.Length -gt $line.Length) { continue }
            [void] $marked.Append((& $tidy $line.Substring($at, $found.Index - $at)))
            [void] $marked.Append('[highlight]').Append((& $tidy $found.Value)).Append('[/]')
            $at = $found.Index + $found.Length
        }
        [void] $marked.Append((& $tidy $line.Substring($at)))
        $before = @(if ($match.Context) { $match.Context.DisplayPreContext })
        $after = @(if ($match.Context) { $match.Context.DisplayPostContext })
        for ($i = 0; $i -lt $before.Count; $i++) {
            [pscustomobject]@{ PSTypeName = 'ConsoleMonster.Everyday.MatchLine'; File = $file; Line = '[muted]' + ($match.LineNumber - $before.Count + $i) + '[/]'; Text = '[muted]' + (& $tidy $before[$i]) + '[/]' }
        }
        [pscustomobject]@{ PSTypeName = 'ConsoleMonster.Everyday.MatchLine'; File = $file; Line = [string] $match.LineNumber; Text = $marked.ToString() }
        for ($i = 0; $i -lt $after.Count; $i++) {
            [pscustomobject]@{ PSTypeName = 'ConsoleMonster.Everyday.MatchLine'; File = $file; Line = '[muted]' + ($match.LineNumber + $i + 1) + '[/]'; Text = '[muted]' + (& $tidy $after[$i]) + '[/]' }
        }
    }
    Title = { if ($_.File) { 'File: ' + $_.File } }
    Property = @(
        @{ Label = 'Line'; Expression = 'Line'; Presenter = 'Markup'; Alignment = 'Right' }
        @{ Label = 'Text'; Expression = 'Text'; Presenter = 'Markup'; Priority = 10 }
    )
}
# Certificates (Get-ChildItem Cert:\...): expiry status uses the same pass/warn/fail visuals as the certificate gallery.
@{
    TypeName = 'System.Security.Cryptography.X509Certificates.X509Certificate2'
    Group = 'Certificate'
    Title = { $parent = [string] $_.PSParentPath; if ($parent) { 'Store: ' + ($parent -replace '^Microsoft\.PowerShell\.Security\\Certificate::', 'Cert:\') } }
    Property = @(
        @{ Label = 'Status'; Expression = {
            $now = [datetime]::Now
            if ($_.NotAfter -lt $now) { 'Expired' } elseif ($_.NotBefore -gt $now) { 'Not yet valid' } elseif ($_.NotAfter -lt $now.AddDays(30)) { 'Expiring' } else { 'Valid' }
        }; Presenter = 'StatusWord'; Priority = 9 }
        @{ Label = 'Subject'; Expression = { $name = $_.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $false); if ($name) { $name } else { $_.Subject } }; Priority = 10 }
        @{ Label = 'Issuer'; Expression = { $name = $_.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::SimpleName, $true); if ($name) { $name } else { $_.Issuer } }; Overflow = 'Truncate'; Priority = 6 }
        @{ Label = 'Expires'; Expression = { $_.NotAfter }; Presenter = 'Date'; Priority = 8 }
        @{ Label = 'Days'; Expression = { ($_.NotAfter.Date - [datetime]::Today).Days }; Alignment = 'Right'; Priority = 7 }
        @{ Label = 'Thumbprint'; Expression = 'Thumbprint'; Overflow = 'TruncateMiddle'; Priority = 5 }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.GroupInfo'
    Group = 'GroupInfo'
    Property = @(
        @{ Label = 'Name'; Expression = 'Name'; Priority = 10 }
        @{ Label = 'Count'; Expression = 'Count'; Presenter = 'DataBar'; Priority = 9 }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.GenericMeasureInfo'
    Group = 'GenericMeasureInfo'
    SingleRecord = $true
    HideEmptyColumns = $true
    Property = @(
        @{ Label = 'Property'; Expression = { if ($_.Property) { $_.Property } else { 'Input' } }; Priority = 10 }
        'Count'
        'Sum'
        'Average'
        'Minimum'
        'Maximum'
        @{ Label = 'StandardDeviation'; Expression = { $deviation = $_.PSObject.Properties['StandardDeviation']; if ($deviation) { $deviation.Value } } }
    )
}
@{
    TypeName = 'Microsoft.PowerShell.Commands.TextMeasureInfo'
    Group = 'TextMeasureInfo'
    SingleRecord = $true
    HideEmptyColumns = $true
    Property = @(
        @{ Label = 'Property'; Expression = { if ($_.Property) { $_.Property } else { 'Input' } }; Priority = 10 }
        'Lines'
        'Words'
        'Characters'
    )
}
# Measure-Command and date arithmetic: a readable duration beside PowerShell's exact value.
@{
    TypeName = 'System.TimeSpan'
    Group = 'TimeSpan'
    Property = @(
        @{ Label = 'Duration'; Expression = { [timespan] $_ }; Presenter = 'Duration'; Priority = 10 }
        @{ Label = 'Exact'; Expression = { ([timespan] $_).ToString() }; Alignment = 'Right' }
    )
}
if ([Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT) {
    # Get-Acl: the owner heads a table of access rules; the security object itself stays unchanged.
    @{
        TypeName = @(
            'System.Security.AccessControl.FileSystemSecurity'
            'System.Security.AccessControl.RegistrySecurity'
        )
        RowTypeName = 'ConsoleMonster.Everyday.AccessRule'
        Group = 'AccessRule'
        HideEmptyColumns = $true
        Rows = {
            # The security object's own methods: its Owner and Access properties need Microsoft.PowerShell.Security loaded.
            $security = $_.PSObject.BaseObject
            $path = ([string] $_.PSPath) -replace '^Microsoft\.PowerShell\.Core\\(FileSystem|Registry)::', ''
            $owner = try { [string] $security.GetOwner([System.Security.Principal.NTAccount]) } catch { [string] $security.GetOwner([System.Security.Principal.SecurityIdentifier]) }
            $heading = (@($path, $(if ($owner) { 'Owner: ' + $owner })) | Where-Object { $_ }) -join ' · '
            $registry = $security -is [System.Security.AccessControl.RegistrySecurity]
            $container = $registry -or $security -is [System.Security.AccessControl.DirectorySecurity]
            $sentence = { param($words) if ($words.Count -gt 1) { ($words[0..($words.Count - 2)] -join ', ') + ' and ' + $words[-1] } else { $words -join '' } }
            foreach ($rule in @($security.GetAccessRules($true, $true, [System.Security.Principal.NTAccount]))) {
                $raw = if ($registry) { [int] $rule.RegistryRights } else { [int] $rule.FileSystemRights }
                $rights = [Collections.Generic.List[string]]::new()
                if ($raw -band 0x10000000) { $rights.Add('full control') }
                if ($raw -band [int]::MinValue) { $rights.Add('read') }
                if ($raw -band 0x40000000) { $rights.Add('write') }
                if ($raw -band 0x20000000) { $rights.Add('execute') }
                $specific = $raw -band 0x0FFFFFFF
                if ($specific) {
                    $name = if ($registry) { [string] [System.Security.AccessControl.RegistryRights] $specific } else { [string] [System.Security.AccessControl.FileSystemRights] $specific }
                    # Synchronize accompanies most file rights; it is named only when it is the whole grant.
                    $parts = @($name -split ', ')
                    if ($parts.Count -gt 1) { $parts = @($parts | Where-Object { $_ -ne 'Synchronize' }) }
                    if ($name -match '^-?\d+$') { $rights.Add('special') }
                    else { foreach ($part in $parts) { $rights.Add(($part -creplace '(?<=[a-z])(?=[A-Z])', ' ').ToLowerInvariant()) } }
                }
                $text = & $sentence $rights
                $appliesTo = $null
                if ($container) {
                    $only = [int] $rule.PropagationFlags -band 2
                    $targets = @(
                        if (-not $only) { if ($registry) { 'this key' } else { 'this folder' } }
                        if ([int] $rule.InheritanceFlags -band 1) { if ($registry) { 'subkeys' } else { 'subfolders' } }
                        if (([int] $rule.InheritanceFlags -band 2) -and -not $registry) { 'files' }
                    )
                    $appliesTo = (& $sentence $targets) + $(if ($only) { ' only' })
                    if ($appliesTo) { $appliesTo = $appliesTo.Substring(0, 1).ToUpperInvariant() + $appliesTo.Substring(1) }
                }
                [pscustomobject]@{
                    PSTypeName = 'ConsoleMonster.Everyday.AccessRule'
                    Heading = $heading
                    Access = [string] $rule.AccessControlType
                    Identity = [string] $rule.IdentityReference
                    Rights = if ($text) { $text.Substring(0, 1).ToUpperInvariant() + $text.Substring(1) } else { 'None' }
                    AppliesTo = $appliesTo
                    Inherited = if ($rule.IsInherited) { 'Inherited' } else { 'Explicit' }
                }
            }
        }
        Title = { $_.Heading }
        Property = @(
            @{ Label = 'Access'; Expression = 'Access'; Presenter = 'StatusWord'; Priority = 9 }
            @{ Label = 'Identity'; Expression = 'Identity'; MaxWidth = 44; Priority = 10 }
            @{ Label = 'Rights'; Expression = 'Rights'; Priority = 8 }
            @{ Label = 'Applies to'; Expression = 'AppliesTo'; Priority = 6 }
            @{ Label = 'Inherited'; Expression = 'Inherited'; Presenter = 'Text'; Role = @{ Equals = 'Inherited'; Role = 'Muted' }; Priority = 7 }
        )
    }
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
            @{ Label = 'Family'; Expression = {
                switch ([string] $_.AddressFamily) { { $_ -in 'InterNetwork', '2', 'IPv4' } { 'IPv4' } { $_ -in 'InterNetworkV6', '23', 'IPv6' } { 'IPv6' } default { $_ } }
            } }
            @{ Label = 'State'; Expression = { ([string] $_.AddressState) -creplace '(?<=[a-z])(?=[A-Z])', ' ' }; Presenter = 'StatusWord' }
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
