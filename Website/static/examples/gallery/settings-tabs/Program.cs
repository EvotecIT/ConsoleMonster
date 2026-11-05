// Settings across tabs, with an outline that opens each section and a breadcrumb that shows where you are.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Layout: construct the widgets shown on screen.
Collapsible proxy = new Collapsible("Proxy", new Rows(
    new TextInput("ProxyServer", "proxy.contoso.test:8080") { Label = "Server" },
    new Toggle("ProxyBypass", true) { Label = "Bypass for local addresses" }))
{
    Expanded = true
};
Collapsible dns = new Collapsible("DNS", new Rows(
    new TextInput("DnsServers", "10.0.0.53, 10.0.1.53") { Label = "Servers" },
    new TextInput("DnsSuffix", "corp.contoso.test") { Label = "Search suffix" }));
Collapsible firewall = new Collapsible("Firewall", new Rows(
    new Toggle("FirewallOn", true) { Label = "Enabled" },
    new TextInput("FirewallPorts", "443, 8443") { Label = "Allowed ports" }));
Outline sections = new Outline("Sections")
{
    Items =
    {
        new OutlineItem("Proxy") { Target = proxy },
        new OutlineItem("DNS") { Target = dns },
        new OutlineItem("Firewall") { Target = firewall }
    },
    CurrentIndex = 0
};
Breadcrumb where = new Breadcrumb("Where") { Ancestors = { "Settings", "Network" }, Outline = sections };
Tabs layout = new Tabs()
    .Add("General", new Rows(
        new TextInput("DisplayName", "Contoso Operations") { Label = "Display name" },
        new Toggle("StartAtLogon", false) { Label = "Start at logon" }))
    .Add("Network", new Rows(
        where,
        new Splitter(sections, new Rows(proxy, dns, firewall)) { Ratio = 0.25 }))
    .Add("Logging", new Rows(
        new TextInput("LogPath", "C:\\Logs\\Operations") { Label = "Folder" },
        new Toggle("LogVerbose", false) { Label = "Verbose" }));

// Behaviour: 1-3 or Ctrl+PageUp/PageDown switch tabs, Enter on an outline entry opens its section; Escape exits.
ConsoleApp.Run(layout);