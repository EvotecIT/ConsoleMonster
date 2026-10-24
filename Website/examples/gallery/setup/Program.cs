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
var components = new[] {
    new Part("Agent", "Agent service", "Core", "18 MB", "Runs tasks and reports results.", true),
    new Part("Cli", "Command-line tools", "Core", "6 MB", "The toolbox command and PowerShell module.", true),
    new Part("Html", "HTML reports", "Reporting", "4 MB", "Interactive reports with tables and charts.", true),
    new Part("Pdf", "PDF export", "Reporting", "31 MB", "Prints reports to PDF."),
    new Part("Chat", "Chat notifications", "Integrations", "1 MB", "Posts run summaries to a channel."),
    new Part("Mail", "Email notifications", "Integrations", "1 MB", "Sends reports by email."),
    new Part("Syslog", "Syslog forwarder", "Integrations", "1 MB", "Forwards findings to a collector.", false, "needs the Enterprise licence")
};
var accounts = new[] {
    new Choice("CurrentUser", "Current user") { Description = "Runs with your signed-in identity." },
    new Choice("ServiceUser", "Dedicated service identity") { Description = "Uses the platform's managed service identity." },
    new Choice("Custom", "A specific account") { Description = "An account you name; password stays out of the command." }
};
string[] pageNames = { "Location", "Components", "Service", "Connection", "Review", "Install", "Result" };
string[] installSteps = { "Copy files", "Register agent", "Configure connection", "Start agent" };
var endpoints = new[] {
    new Endpoint("eu", "eu-west.example.test", "Dublin", "12 ms"),
    new Endpoint("eu", "eu-central.example.test", "Frankfurt", "9 ms"),
    new Endpoint("eu", "eu-north.example.test", "Stockholm", "31 ms"),
    new Endpoint("us", "us-east.example.test", "East", "18 ms"),
    new Endpoint("us", "us-west.example.test", "West", "74 ms"),
    new Endpoint("ap", "ap-south.example.test", "South", "121 ms"),
    new Endpoint("ap", "ap-east.example.test", "East", "", "maintenance until 18:00")
};
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 6) throw new ArgumentOutOfRangeException(nameof(step));
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
    path.PathKind = PathKind.Folder; path.Default = installFolder; path.Required = true; path.ParameterName = "Path";
    path.Help = "Created during installation. Tab completes folder names.";
    PathField data = location.Path("Data", "Data folder");
    data.PathKind = PathKind.Folder; data.Default = dataFolder; data.ParameterName = "DataPath";
    data.Help = "Logs, reports and the local queue.";
    ToggleField launcher = location.Toggle("Launcher", "Launcher entry");
    launcher.Default = true; launcher.ParameterName = "Launcher";
    FormPage parts = form.Page("Components");
    ChoiceField selection = parts.MultiSelect("Components", "Components", Parts());
    selection.Minimum = 1; selection.ParameterName = "Component";
    selection.Help = "Space toggles, Ctrl+A selects all, Enter closes the list.";
    FormPage service = form.Page("Service");
    ChoiceField account = service.Select("Account", "Service account", accounts);
    account.Default = "ServiceUser"; account.ParameterName = "Account";
    TextField user = service.Text("UserName", "User name");
    user.VisibleWhen = v => Equals(v["Account"], "Custom"); user.Required = true;
    user.Placeholder = "toolbox-agent"; user.ParameterName = "UserName";
    SecretField password = service.Secret("Password", "Password");
    password.VisibleWhen = v => Equals(v["Account"], "Custom"); password.ParameterName = "Password";
    password.CommandPlaceholder = "(Read-Host -AsSecureString)";
    ToggleField automatic = service.Toggle("AutoStart", "Start automatically");
    automatic.Default = true; automatic.ParameterName = "AutoStart";
    FormPage connection = form.Page("Connection");
    ChoiceField region = connection.Select("Region", "Region", new[] {
        new Choice("eu", "Europe"), new Choice("us", "Americas"), new Choice("ap", "Asia Pacific")
    });
    region.Default = "eu"; region.Required = true; region.ParameterName = "Region";
    region.Help = "Choosing a region loads its servers.";
    ChoiceField server = connection.Select("Server", "Server");
    server.DependsOn.Add("Region"); server.Required = true; server.ParameterName = "Server";
    server.Loader = async (values, token) => { await Task.Delay(300, token); return Servers(values.Get<string>("Region")); };
    server.Help = "Loaded for the chosen region; Esc cancels loading.";
    NumberField port = connection.Number("Port", "Port");
    port.Integer = true; port.Minimum = 1; port.Maximum = 65535; port.Default = 443m; port.ParameterName = "Port";
    ToggleField proxy = connection.Toggle("UseProxy", "Use a proxy"); proxy.ParameterName = "UseProxy";
    TextField address = connection.Text("Proxy", "Proxy address");
    address.VisibleWhen = v => v.Get<bool>("UseProxy"); address.Required = true;
    address.Placeholder = "http://proxy.example.test:8080"; address.ParameterName = "Proxy";
    return form;
}

// The snapshot uses native app controls on each wizard page, without asking for input.
IWidget SnapshotPage(int page) {
    IWidget content;
    switch (page) {
        case 0:
            content = new Rows(new TextBlock("Where the agent and its data go."),
                new TextInput("Path", installFolder) { Label = "Install folder" },
                new TextInput("Data", dataFolder) { Label = "Data folder" },
                new Toggle("Launcher", true) { Label = "Launcher entry" },
                new TextBlock("[muted]Logs, reports and the local queue use the data folder.[/]")) { Gap = 1 };
            break;
        case 1:
            Table parts = Table.For(components.Select(p => new { Component = p.Name, Group = p.Group, Size = p.Size, Description = p.Description }))
                .WithExpand().Column(p => p.Component).Column(p => p.Group).Column(p => p.Size).Column(p => p.Description, c => c.WithFlexible());
            content = new Rows(new MultiSelect("Components", Parts()) { Label = "Components" }, parts,
                new TextBlock("[muted]Syslog forwarder needs the Enterprise licence. At least one component is required.[/]")) { Gap = 1 };
            break;
        case 2:
            content = new Rows(new Select("Account", accounts) { Label = "Service account", Value = "ServiceUser" },
                new Toggle("AutoStart", true) { Label = "Start automatically" },
                new TextBlock("Choose a specific account to reveal user name and password fields."),
                new TextBlock("[muted]Secrets are omitted from the equivalent command.[/]")) { Gap = 1 };
            break;
        case 3:
            content = new Rows(new Select("Region", new[] { new Choice("eu", "Europe"), new Choice("us", "Americas"), new Choice("ap", "Asia Pacific") }) { Label = "Region", Value = "eu" },
                new Select("Server", Servers("eu")) { Label = "Server", Value = "eu-central.example.test" },
                new TextInput("Port", "443") { Label = "Port (1–65535)" },
                new Toggle("UseProxy") { Label = "Use a proxy" },
                new TextBlock("[muted]Changing the region reloads servers. Enabling a proxy reveals its address.[/]")) { Gap = 1 };
            break;
        case 4:
            content = new Rows(new KeyValueList().Add("Install folder", installFolder).Add("Data folder", dataFolder)
                .Add("Components", "Agent, Cli, Html").Add("Service account", "Dedicated service identity")
                .Add("Server", "eu-central.example.test:443").Add("Launcher / auto start", "Yes / Yes"),
                new Panel(new TextBlock("Install-ToolboxAgent -Path './toolbox-agent' -DataPath './toolbox-data' -Component Agent,Cli,Html -Account ServiceUser -Region eu -Server eu-central.example.test -Port 443 -Launcher -AutoStart"), "Equivalent command"),
                new Button("Install", "Install"), new Button("Back", "Back")) { Gap = 1 };
            break;
        default:
            StatusList tasks = new StatusList();
            for (int i = 0; i < installSteps.Length; i++) tasks.Add(installSteps[i], page == 6 || i < 2 ? StatusKind.Pass : i == 2 ? StatusKind.Running : StatusKind.Pending);
            ProgressBar progress = new ProgressBar("Install") {
                Total = 40, Value = page == 6 ? 40 : 24,
                Status = page == 6 ? StatusKind.Pass : StatusKind.Running
            };
            content = new Rows(tasks, progress, new TextBlock(page == 6
                ? "[pass]Toolbox Agent installed[/] · 4 steps done · launch with the toolbox command"
                : "Configuring eu-central.example.test:443")) { Gap = 1 };
            break;
    }
    Phases strip = new Phases();
    for (int i = 0; i < pageNames.Length; i++) strip.Set(pageNames[i], page == 6 || i < page ? StatusKind.Pass : i == page ? StatusKind.Running : StatusKind.Pending);
    return new Rows(new Rule("[bold]Install Toolbox Agent[/]"), strip,
        new Panel(content, $"{page + 1} / 7 · {pageNames[page]}")) { Gap = 1 };
}

// Behaviour: edit every page, review, then run a bounded in-memory installation.
if (snapshot) {
    ConsoleApp.Run(SnapshotPage(step), options: new ConsoleAppOptions { StaticSnapshot = true });
    return;
}
FormResult answers = BuildForm().Show(new FormOptions());
if (!answers.Submitted) return;
StatusList install = new StatusList("steps");
foreach (string label in installSteps) install.Add(label, StatusKind.Pending);
ProgressBar bar = new ProgressBar("Install") { Total = 40 };
Dashboard dashboard = new DashboardBuilder("Installing Toolbox Agent")
    .Subtitle(Markup.Escape(answers.Values.Get<string>("Path")))
    .Panel("Steps", install).Add(new Padding(bar, 1, 0, 1, 0)).BuildDashboard();
using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => {
        StatusListHandle status = install.Handle(s);
        ProgressHandle progress = bar.Handle(s);
        foreach (string label in installSteps) {
            status.Set(label, StatusKind.Running);
            progress.Label(label);
            for (int i = 0; i < 10; i++) {
                s.Cancellation.ThrowIfCancellationRequested();
                Thread.Sleep(90); progress.Advance();
            }
            status.Set(label, StatusKind.Pass);
        }
        progress.Complete();
    });
}
Terminal.Write(new Panel(new KeyValueList().Add("Install folder", Markup.Escape(answers.Values.Get<string>("Path")))
    .Add("Components", Markup.Escape(string.Join(", ", answers.Values.GetList<object>("Components"))))
    .Add("Server", Markup.Escape(answers.Values.Get<string>("Server")))
    .Add("Steps done", "4", StatusKind.Pass).Add("Launch", "toolbox"), "Toolbox Agent installed"));

record Part(string Id, string Name, string Group, string Size, string Description, bool Selected = false, string? Disabled = null);
record Endpoint(string Region, string Host, string City, string Latency, string? Disabled = null);
