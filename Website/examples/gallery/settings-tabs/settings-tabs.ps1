# Settings across tabs, with an outline that opens each section and a breadcrumb that shows where you are.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Layout: construct the widgets shown on screen.
$proxy = New-ConsoleCollapsible -Key Proxy -Expanded -Content {
    New-ConsoleTextInput -Key ProxyServer -Label 'Server' -Value 'proxy.contoso.test:8080'
    New-ConsoleToggle -Key ProxyBypass -Label 'Bypass for local addresses' -Value $true
}
$dns = New-ConsoleCollapsible -Key DNS -Content {
    New-ConsoleTextInput -Key DnsServers -Label 'Servers' -Value '10.0.0.53, 10.0.1.53'
    New-ConsoleTextInput -Key DnsSuffix -Label 'Search suffix' -Value 'corp.contoso.test'
}
$firewall = New-ConsoleCollapsible -Key Firewall -Content {
    New-ConsoleToggle -Key FirewallOn -Label 'Enabled' -Value $true
    New-ConsoleTextInput -Key FirewallPorts -Label 'Allowed ports' -Value '443, 8443'
}
$sections = New-ConsoleOutline -Key Sections -CurrentIndex 0 -Item @(
    [pscustomobject]@{ Title = 'Proxy'; Target = $proxy }
    [pscustomobject]@{ Title = 'DNS'; Target = $dns }
    [pscustomobject]@{ Title = 'Firewall'; Target = $firewall }
)
$general = New-ConsoleRows -Content {
    New-ConsoleTextInput -Key DisplayName -Label 'Display name' -Value 'Contoso Operations'
    New-ConsoleToggle -Key StartAtLogon -Label 'Start at logon' -Value $false
}
$network = New-ConsoleRows -Content {
    New-ConsoleBreadcrumb -Key Where -Ancestor 'Settings', 'Network' -Outline $sections
    New-ConsoleSplitter -Ratio 0.25 -First { $sections } -Second { $proxy; $dns; $firewall }
}
$logging = New-ConsoleRows -Content {
    New-ConsoleTextInput -Key LogPath -Label 'Folder' -Value 'C:\Logs\Operations'
    New-ConsoleToggle -Key LogVerbose -Label 'Verbose' -Value $false
}
$content = {
    New-ConsoleTabs -Content {
        @{ Title = 'General'; Content = $general }
        @{ Title = 'Network'; Content = $network }
        @{ Title = 'Logging'; Content = $logging }
    }
}

# Behaviour: 1-3 or Ctrl+PageUp/PageDown switch tabs, Enter on an outline entry opens its section; Escape exits.
Show-ConsoleApp -Content $content