// Settings across tabs, with an outline that opens each section and a breadcrumb that shows where you are.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Layout: construct the widgets shown on screen.
Collapsible proxy = Ui.Collapsible().Key("Proxy").Content(Ui.Rows().Content(rowsContent =>
{
    rowsContent.TextInput(textInput => textInput.Key("ProxyServer")
        .Value("proxy.contoso.test:8080")
        .Label("Server"));
    rowsContent.Toggle(toggle => toggle.Key("ProxyBypass")
        .Value(true)
        .Label("Bypass for local addresses"));
})).Expanded().Build();
Collapsible dns = Ui.Collapsible().Key("DNS").Content(Ui.Rows().Content(rowsContent =>
{
    rowsContent.TextInput(textInput => textInput.Key("DnsServers")
        .Value("10.0.0.53, 10.0.1.53")
        .Label("Servers"));
    rowsContent.TextInput(textInput => textInput.Key("DnsSuffix")
        .Value("corp.contoso.test")
        .Label("Search suffix"));
})).Build();
Collapsible firewall = Ui.Collapsible().Key("Firewall").Content(Ui.Rows().Content(rowsContent =>
{
    rowsContent.Toggle(toggle => toggle.Key("FirewallOn").Value(true).Label("Enabled"));
    rowsContent.TextInput(textInput => textInput.Key("FirewallPorts")
        .Value("443, 8443")
        .Label("Allowed ports"));
})).Build();
Outline sections = ((Outline)Ui.Outline()
    .Key("Sections")
    .Build()).AddItem(new OutlineItem("Proxy").Target(proxy))
    .AddItem(new OutlineItem("DNS").Target(dns))
    .AddItem(new OutlineItem("Firewall").Target(firewall))
    .SelectedIndex(0);
Breadcrumb where = ((Breadcrumb)Ui.Breadcrumb().Key("Where").Build()).AddAncestor("Settings")
    .AddAncestor("Network")
    .Outline(sections);
Tabs layout = Ui.Tabs().Add("General", Ui.Rows().Content(rowsContent =>
{
    rowsContent.TextInput(textInput => textInput.Key("DisplayName")
        .Value("Contoso Operations")
        .Label("Display name"));
    rowsContent.Toggle(toggle => toggle.Key("StartAtLogon").Value(false).Label("Start at logon"));
})).Add("Network", Ui.Rows().Content(rowsContent2 =>
{
    rowsContent2.Add(where);
    rowsContent2.Splitter(splitter => splitter.First(sections)
        .Second(Ui.Rows().Content(rowsContent =>
        {
            rowsContent.Add(proxy);
            rowsContent.Add(dns);
            rowsContent.Add(firewall);
        }))
        .Ratio(0.25));
})).Add("Logging", Ui.Rows().Content(rowsContent =>
{
    rowsContent.TextInput(textInput => textInput.Key("LogPath")
        .Value("C:\\Logs\\Operations")
        .Label("Folder"));
    rowsContent.Toggle(toggle => toggle.Key("LogVerbose").Value(false).Label("Verbose"));
}));

// Behaviour: Ctrl+PageUp/PageDown switch tabs, Enter on an outline entry opens its section; Escape exits.
ConsoleApp.Run(layout);
