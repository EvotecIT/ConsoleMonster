using System;
using System.Runtime.InteropServices;
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// A setup wizard: a page per step, grouped components, fields that appear for a custom account, a server list that
// loads for the chosen region, the review with the equivalent command, then the install run. Nothing is installed.
bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
bool mac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
Form form = new Form("Install Toolbox Agent") {
    Command = "Install-ToolboxAgent",
    Description = "Sets up the agent that runs toolbox tasks on this machine.",
    SubmitText = "Install"
};

FormPage location = form.Page("Location", "Where the agent and its data go.");
PathField path = location.Path("Path", "Install folder");
path.PathKind = PathKind.Folder;
path.Default = windows ? @"C:\Program Files\Toolbox Agent" : "/usr/local/lib/toolbox-agent";
path.Required = true;
path.Parameter = "Path";
path.Help = "Created when it does not exist. Tab completes folder names.";
// <more>
PathField data = location.Path("Data", "Data folder");
data.PathKind = PathKind.Folder;
data.Default = windows ? @"C:\ProgramData\Toolbox Agent" : mac ? "/Library/Application Support/Toolbox Agent" : "/var/lib/toolbox-agent";
data.Parameter = "DataPath";
data.Help = "Logs, reports and the local queue. Needs a few hundred MB.";
ToggleField shortcuts = location.Toggle("Shortcuts", windows ? "Start menu shortcuts" : "Command-line shortcuts");
shortcuts.Default = true;
shortcuts.Parameter = "Shortcuts";
// </more>

FormPage components = form.Page("Components", "Pick what to install. You can add more later.");
ChoiceField parts = components.MultiSelect("Components", "Components", new[] {
    new Choice("Agent", "Agent service") { Group = "Core", Selected = true, Description = "Runs tasks and reports results. Required.", Badge = RichText.Plain("18 MB") },
    // <more>
    new Choice("Cli", "Command-line tools") { Group = "Core", Selected = true, Description = windows ? "toolbox.exe and the PowerShell module." : "toolbox and the PowerShell module.", Badge = RichText.Plain("6 MB") },
    new Choice("Html", "HTML reports") { Group = "Reporting", Selected = true, Description = "Interactive reports with tables and charts.", Badge = RichText.Plain("4 MB") },
    new Choice("Pdf", "PDF export") { Group = "Reporting", Description = "Prints reports to PDF.", Badge = RichText.Plain("31 MB") },
    new Choice("Teams", "Teams notifications") { Group = "Integrations", Description = "Posts run summaries to a Teams channel.", Badge = RichText.Plain("1 MB") },
    new Choice("Mail", "Email notifications") { Group = "Integrations", Description = "Sends reports by email.", Badge = RichText.Plain("1 MB") },
    new Choice("Syslog", "Syslog forwarder") { Group = "Integrations", Description = "Forwards findings to a syslog collector.", DisabledReason = "needs the Enterprise licence" }
    // </more>
});
parts.Minimum = 1;
parts.Parameter = "Component";
parts.Help = "Space toggles, Ctrl+A selects all, Enter closes the list.";

FormPage service = form.Page("Service", "How the agent runs.");
ChoiceField account = service.Select("Account", "Service account", windows ? new[] {
    new Choice("LocalSystem", "Local System") { Description = "Full rights on this machine; no password." },
    new Choice("NetworkService", "Network Service") { Description = "Limited rights; uses the computer account on the network." },
    new Choice("Custom", "A specific account") { Description = "A domain or local account you name." }
} : new[] {
    new Choice("Dedicated", "Dedicated service user") { Description = "A restricted toolbox user for this service." },
    new Choice("Custom", "A specific user") { Description = "An existing local user you name." }
});
account.Default = windows ? "NetworkService" : "Dedicated";
account.Parameter = "Account";
TextField user = service.Text("UserName", "User name");
user.VisibleWhen = v => Equals(v["Account"], "Custom");
user.Required = true;
user.Placeholder = windows ? @"CONTOSO\svc-toolbox" : "toolbox";
user.Parameter = "UserName";
SecretField password = service.Secret("Password", "Password");
password.VisibleWhen = v => Equals(v["Account"], "Custom");
password.Parameter = "Password";
password.CommandPlaceholder = "(Read-Host -AsSecureString)";
// <more>
ToggleField automatic = service.Toggle("AutoStart", "Start automatically");
automatic.Default = true;
automatic.Parameter = "AutoStart";
// </more>

FormPage connection = form.Page("Connection", "Where the agent reports to.");
ChoiceField region = connection.Select("Region", "Region", new[] {
    new Choice("eu", "Europe"),
    new Choice("us", "Americas"),
    new Choice("ap", "Asia Pacific")
});
region.Required = true;
region.Parameter = "Region";
region.Help = "Choosing a region loads its servers.";
ChoiceField server = connection.Select("Server", "Server");
server.DependsOn.Add("Region");
server.Required = true;
server.Parameter = "Server";
server.Loader = (values, token) => LoadServersAsync(values.Get<string>("Region"), token);
server.Help = "Loaded for the chosen region; Esc cancels loading.";
// <more>
NumberField port = connection.Number("Port", "Port");
port.WholeNumber = true;
port.Minimum = 1;
port.Maximum = 65535;
port.Default = 443m;
port.Parameter = "Port";
ToggleField proxy = connection.Toggle("UseProxy", "Use a proxy");
proxy.Parameter = "UseProxy";
TextField proxyAddress = connection.Text("Proxy", "Proxy address");
proxyAddress.VisibleWhen = v => v.Get<bool>("UseProxy");
proxyAddress.Required = true;
proxyAddress.Placeholder = "http://proxy.example.org:8080";
proxyAddress.Parameter = "Proxy";
// </more>

FormResult result = form.Show(new FormOptions { AfterSubmit = Install });
if (!result.Submitted) {
    Terminal.WriteMarkupLine("[muted]Setup was cancelled; nothing was changed.[/]");
}

// <more>
async Task<IEnumerable<Choice>> LoadServersAsync(string? chosen, CancellationToken token) {
    await Task.Delay(1400, token);
    return chosen switch {
        "us" => new[] { new Choice("us-east-1", "us-east-1") { Badge = RichText.Plain("18 ms") }, new Choice("us-west-2", "us-west-2") { Badge = RichText.Plain("74 ms") } },
        "ap" => new[] { new Choice("ap-south-1", "ap-south-1") { Badge = RichText.Plain("121 ms") }, new Choice("ap-east-1", "ap-east-1") { DisabledReason = "maintenance until 18:00" } },
        _ => new[] {
            new Choice("eu-west-1", "eu-west-1") { Badge = RichText.Plain("12 ms"), Description = "Dublin" },
            new Choice("eu-central-1", "eu-central-1") { Badge = RichText.Plain("9 ms"), Description = "Frankfurt" },
            new Choice("eu-north-1", "eu-north-1") { Badge = RichText.Plain("31 ms"), Description = "Stockholm" }
        }
    };
}

void Install(MenuContext context) {
    FormValues values = context.Values!;
    string[] steps = { "Copy files", "Register service", "Configure connection", "Start service" };
    StatusList list = new StatusList("steps");
    foreach (string step in steps) {
        list.Set(step, step, StatusKind.Pending);
    }
    ProgressBar progress = new ProgressBar("Install", "progress") { Total = steps.Length * 10, LabelWidth = 22 };
    Dashboard dashboard = new DashboardBuilder("Installing")
        .Subtitle(Markup.Escape(values.Get<string>("Path")))
        .Panel("Steps", list)
        .Add(new Padding(progress, 1, 0, 1, 0))
        .BuildDashboard();
    context.Run(dashboard, run => {
        StatusListHandle handle = list.Handle(run.Session);
        ProgressHandle bar = progress.Handle(run.Session);
        foreach (string step in steps) {
            handle.Set(step, StatusKind.Running);
            bar.Label(step);
            for (int i = 0; i < 10 && !run.CancellationToken.IsCancellationRequested; i++) {
                Thread.Sleep(90);
                bar.Advance();
            }
            handle.Set(step, StatusKind.Pass);
        }
        bar.Complete();
        Thread.Sleep(300);
        RunResult done = new RunResult("Toolbox Agent installed") {
            Summary = Markup.Escape(values.Get<string>("Path")) + " · " + string.Join(", ", values.GetList<object>("Components")) + " · " + Markup.Escape(values.Get<string>("Server"))
        };
        done.Figure("steps done", steps.Length.ToString(), StatusKind.Pass);
        return done;
    });
}
// </more>