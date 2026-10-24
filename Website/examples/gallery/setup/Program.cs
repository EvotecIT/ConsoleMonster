// A complete installer: location, components, service, connection, review and progress.
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: invented installation inputs. The bounded driver only delays and updates widgets.
// No files, services, launch entries, network connections or persistent presets are created.
string installFolder = "./toolbox-agent";
string dataFolder = "./toolbox-data";
Part[] components = [
    new(Id: "Agent", Name: "Agent service", Group: "Core", Size: "18 MB", Description: "Runs tasks and reports results.", Selected: true),
    new(Id: "Cli", Name: "Command-line tools", Group: "Core", Size: "6 MB", Description: "The toolbox command and PowerShell module.", Selected: true),
    new(Id: "Html", Name: "HTML reports", Group: "Reporting", Size: "4 MB", Description: "Interactive reports with tables and charts.", Selected: true),
    new(Id: "Pdf", Name: "PDF export", Group: "Reporting", Size: "31 MB", Description: "Prints reports to PDF."),
    new(Id: "Chat", Name: "Chat notifications", Group: "Integrations", Size: "1 MB", Description: "Posts run summaries to a channel."),
    new(Id: "Mail", Name: "Email notifications", Group: "Integrations", Size: "1 MB", Description: "Sends reports by email."),
    new(Id: "Syslog", Name: "Syslog forwarder", Group: "Integrations", Size: "1 MB", Description: "Forwards findings to a collector.", Selected: false, Disabled: "needs the Enterprise licence"),
];
var accounts = new[] {
    new Choice("CurrentUser", "Current user") { Description = "Runs with your signed-in identity." },
    new Choice("ServiceUser", "Dedicated service identity") { Description = "Uses the platform's managed service identity." },
    new Choice("Custom", "A specific account") { Description = "An account you name; password stays out of the command." }
};
string[] installSteps = { "Copy files", "Register agent", "Configure connection", "Start agent" };
Endpoint[] endpoints = [
    new(Region: "eu", Host: "eu-west.example.test", City: "Dublin", Latency: "12 ms"),
    new(Region: "eu", Host: "eu-central.example.test", City: "Frankfurt", Latency: "9 ms"),
    new(Region: "eu", Host: "eu-north.example.test", City: "Stockholm", Latency: "31 ms"),
    new(Region: "us", Host: "us-east.example.test", City: "East", Latency: "18 ms"),
    new(Region: "us", Host: "us-west.example.test", City: "West", Latency: "74 ms"),
    new(Region: "ap", Host: "ap-south.example.test", City: "South", Latency: "121 ms"),
    new(Region: "ap", Host: "ap-east.example.test", City: "East", Latency: "", Disabled: "maintenance until 18:00"),
];
Choice[] Parts() => components.Select(p => new Choice(p.Id, p.Name) {
    Group = p.Group, Description = p.Description, Badge = RichText.Plain(p.Size, new RichStyle(ThemeToken.Muted)),
    Selected = p.Selected, DisabledReason = p.Disabled
}).ToArray();
Choice[] Servers(string? region) => endpoints.Where(e => e.Region == region).Select(e => new Choice(e.Host) {
    Description = e.City, Badge = RichText.Plain(e.Latency, new RichStyle(ThemeToken.Muted)), DisabledReason = e.Disabled
}).ToArray();

// Layout: the native form owns validation, dependent choices, editors and review.
Form BuildForm() {
    Form form = new Form("Install Toolbox Agent") {
        Command = "Install-ToolboxAgent", SubmitText = "Install",
        Description = "Sets up the agent that runs toolbox tasks on this machine."
    };
    FormPage location = form.Page("Location");
    PathField path = location.Path("Path", "Install folder");
    path.PathKind = PathKind.Folder; path.Default = installFolder; path.Required = true; path.Parameter = "Path";
    path.Help = "Created during installation. Tab completes folder names.";
    PathField data = location.Path("Data", "Data folder");
    data.PathKind = PathKind.Folder; data.Default = dataFolder; data.Parameter = "DataPath";
    data.Help = "Logs, reports and the local queue.";
    ToggleField launcher = location.Toggle("Launcher", "Launcher entry");
    launcher.Default = true; launcher.Parameter = "Launcher";
    FormPage parts = form.Page("Components");
    ChoiceField selection = parts.MultiSelect("Components", "Components", Parts());
    selection.Minimum = 1; selection.Parameter = "Component";
    selection.Help = "Space toggles, Ctrl+A selects all, Enter closes the list.";
    FormPage service = form.Page("Service");
    ChoiceField account = service.Select("Account", "Service account", accounts);
    account.Default = "ServiceUser"; account.Parameter = "Account";
    TextField user = service.Text("UserName", "User name");
    user.VisibleWhen = v => Equals(v["Account"], "Custom"); user.Required = true;
    user.Placeholder = "toolbox-agent"; user.Parameter = "UserName";
    SecretField password = service.Secret("Password", "Password");
    password.VisibleWhen = v => Equals(v["Account"], "Custom"); password.Parameter = "Password";
    password.CommandPlaceholder = "(Read-Host -AsSecureString)";
    ToggleField automatic = service.Toggle("AutoStart", "Start automatically");
    automatic.Default = true; automatic.Parameter = "AutoStart";
    FormPage connection = form.Page("Connection");
    ChoiceField region = connection.Select("Region", "Region", new[] {
        new Choice("eu", "Europe"), new Choice("us", "Americas"), new Choice("ap", "Asia Pacific")
    });
    region.Default = "eu"; region.Required = true; region.Parameter = "Region";
    region.Help = "Choosing a region loads its servers.";
    ChoiceField server = connection.Select("Server", "Server");
    server.DependsOn.Add("Region"); server.Required = true; server.Parameter = "Server";
    server.Loader = async (values, token) => { await Task.Delay(300, token); return Servers(values.Get<string>("Region")); };
    server.Help = "Loaded for the chosen region; Esc cancels loading.";
    NumberField port = connection.Number("Port", "Port");
    port.WholeNumber = true; port.Minimum = 1; port.Maximum = 65535; port.Default = 443m; port.Parameter = "Port";
    ToggleField proxy = connection.Toggle("UseProxy", "Use a proxy"); proxy.Parameter = "UseProxy";
    TextField address = connection.Text("Proxy", "Proxy address");
    address.VisibleWhen = v => v.Get<bool>("UseProxy"); address.Required = true;
    address.Placeholder = "http://proxy.example.test:8080"; address.Parameter = "Proxy";
    return form;
}

// Behaviour: edit every page, review, then run a bounded in-memory installation.
FormResult answers = BuildForm().Show();
if (!answers.Submitted) return;
StatusList install = new StatusList("steps");
foreach (string label in installSteps) install.Add(label, StatusKind.Pending);
ProgressBar bar = new ProgressBar("Install") { Total = 40 };
Dashboard dashboard = new DashboardBuilder("Installing Toolbox Agent")
    .Subtitle(Markup.Escape(answers.Values.Get<string>("Path")))
    .Panel("Steps", install).Add(new Padding(bar, 1, 0, 1, 0)).BuildDashboard();
using (LiveSession session = new LiveSession(dashboard)) {
    try {
        session.Run(s => {
            StatusListHandle status = install.Handle(s);
            ProgressHandle progress = bar.Handle(s);
            foreach (string label in installSteps) {
                status.Set(label, StatusKind.Running);
                progress.Label(label);
                for (int i = 0; i < 10; i++) {
                    s.Cancellation.ThrowIfCancellationRequested();
                    progress.Advance();
                    s.Delay(TimeSpan.FromMilliseconds(90));
                }
                status.Set(label, StatusKind.Pass);
            }
            progress.Complete();
        });
    } catch (OperationCanceledException cancelled) when (cancelled.CancellationToken == session.Cancellation) {
        // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
        return;
    }
}
Terminal.Write(new Panel(new KeyValueList().Add("Install folder", Markup.Escape(answers.Values.Get<string>("Path")))
    .Add("Components", Markup.Escape(string.Join(", ", answers.Values.GetList<object>("Components"))))
    .Add("Server", Markup.Escape(answers.Values.Get<string>("Server")))
    .Add("Steps done", "4", StatusKind.Pass).Add("Launch", "toolbox"), "Toolbox Agent installed"));

record Part(string Id, string Name, string Group, string Size, string Description, bool Selected = false, string? Disabled = null);
record Endpoint(string Region, string Host, string City, string Latency, string? Disabled = null);
