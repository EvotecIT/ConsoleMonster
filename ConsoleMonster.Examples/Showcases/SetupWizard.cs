using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ConsoleMonster.Building;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>
    /// The installer showcase: a multi-page wizard with a path prompt, grouped components, conditional and dependent fields
    /// (the region loads its servers), a secret, the review page with the equivalent command, then a progress run.
    /// Nothing is installed; the data is invented.
    /// </summary>
    public static class SetupWizard {
        /// <summary>Builds the wizard.</summary>
        /// <returns>The form.</returns>
        public static Form Build() {
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
            path.ParameterName = "Path";
            path.Help = "Created when it does not exist. Tab completes folder names.";
            PathField data = location.Path("Data", "Data folder");
            data.PathKind = PathKind.Folder;
            data.Default = windows ? @"C:\ProgramData\Toolbox Agent" : mac ? "/Library/Application Support/Toolbox Agent" : "/var/lib/toolbox-agent";
            data.ParameterName = "DataPath";
            data.Help = "Logs, reports and the local queue. Needs a few hundred MB.";
            ToggleField shortcuts = location.Toggle("Shortcuts", windows ? "Start menu shortcuts" : "Command-line shortcuts");
            shortcuts.Default = true;
            shortcuts.ParameterName = "Shortcuts";

            FormPage components = form.Page("Components", "Pick what to install. You can add more later.");
            ChoiceField parts = components.MultiSelect("Components", "Components", new[] {
                new Choice("Agent", "Agent service") { Group = "Core", Selected = true, Description = "Runs tasks and reports results. Required.", Badge = RichText.Plain("18 MB") },
                new Choice("Cli", "Command-line tools") { Group = "Core", Selected = true, Description = windows ? "toolbox.exe and the PowerShell module." : "toolbox and the PowerShell module.", Badge = RichText.Plain("6 MB") },
                new Choice("Html", "HTML reports") { Group = "Reporting", Selected = true, Description = "Interactive reports with tables and charts.", Badge = RichText.Plain("4 MB") },
                new Choice("Pdf", "PDF export") { Group = "Reporting", Description = "Prints reports to PDF.", Badge = RichText.Plain("31 MB") },
                new Choice("Teams", "Teams notifications") { Group = "Integrations", Description = "Posts run summaries to a Teams channel.", Badge = RichText.Plain("1 MB") },
                new Choice("Mail", "Email notifications") { Group = "Integrations", Description = "Sends reports by email.", Badge = RichText.Plain("1 MB") },
                new Choice("Syslog", "Syslog forwarder") { Group = "Integrations", Description = "Forwards findings to a syslog collector.", DisabledReason = "needs the Enterprise licence" }
            });
            parts.Minimum = 1;
            parts.ParameterName = "Component";
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
            account.ParameterName = "Account";
            TextField user = service.Text("UserName", "User name");
            user.VisibleWhen = v => Equals(v["Account"], "Custom");
            user.Required = true;
            user.Placeholder = windows ? @"CONTOSO\svc-toolbox" : "toolbox";
            user.ParameterName = "UserName";
            SecretField password = service.Secret("Password", "Password");
            password.VisibleWhen = v => Equals(v["Account"], "Custom");
            password.ParameterName = "Password";
            password.CommandPlaceholder = "(Read-Host -AsSecureString)";
            ToggleField automatic = service.Toggle("AutoStart", "Start automatically");
            automatic.Default = true;
            automatic.ParameterName = "AutoStart";

            FormPage connection = form.Page("Connection", "Where the agent reports to.");
            ChoiceField region = connection.Select("Region", "Region", new[] {
                new Choice("eu", "Europe"),
                new Choice("us", "Americas"),
                new Choice("ap", "Asia Pacific")
            });
            region.Required = true;
            region.ParameterName = "Region";
            region.Help = "Choosing a region loads its servers.";
            ChoiceField server = connection.Select("Server", "Server");
            server.DependsOn.Add("Region");
            server.Required = true;
            server.ParameterName = "Server";
            server.LoadChoices = (values, token) => LoadServersAsync(values.Get<string>("Region"), token);
            server.Help = "Loaded for the chosen region; Esc cancels loading.";
            NumberField port = connection.Number("Port", "Port");
            port.Integer = true;
            port.Minimum = 1;
            port.Maximum = 65535;
            port.Default = 443m;
            port.ParameterName = "Port";
            ToggleField proxy = connection.Toggle("UseProxy", "Use a proxy");
            proxy.ParameterName = "UseProxy";
            TextField proxyAddress = connection.Text("Proxy", "Proxy address");
            proxyAddress.VisibleWhen = v => v.Get<bool>("UseProxy");
            proxyAddress.Required = true;
            proxyAddress.Placeholder = "http://proxy.example.org:8080";
            proxyAddress.ParameterName = "Proxy";
            return form;
        }

        private static async Task<IEnumerable<Choice>> LoadServersAsync(string? region, CancellationToken token) {
            await Task.Delay(1400, token).ConfigureAwait(false);
            switch (region) {
                case "us":
                    return new[] { new Choice("us-east-1", "us-east-1") { Badge = RichText.Plain("18 ms") }, new Choice("us-west-2", "us-west-2") { Badge = RichText.Plain("74 ms") } };
                case "ap":
                    return new[] { new Choice("ap-south-1", "ap-south-1") { Badge = RichText.Plain("121 ms") }, new Choice("ap-east-1", "ap-east-1") { DisabledReason = "maintenance until 18:00" } };
                default:
                    return new[] {
                        new Choice("eu-west-1", "eu-west-1") { Badge = RichText.Plain("12 ms"), Description = "Dublin" },
                        new Choice("eu-central-1", "eu-central-1") { Badge = RichText.Plain("9 ms"), Description = "Frankfurt" },
                        new Choice("eu-north-1", "eu-north-1") { Badge = RichText.Plain("31 ms"), Description = "Stockholm" }
                    };
            }
        }

        /// <summary>Shows the wizard and, when submitted, the install run in the same frame, ending on a summary.</summary>
        public static void Show() {
            Show(new FormOptions { AppName = "ConsoleMonster setup wizard" });
        }

        /// <summary>Shows the wizard with options (scripted input, a capture) and runs the install after submitting.</summary>
        /// <param name="options">The options; AfterSubmit is set here.</param>
        /// <returns>The form result (with the run's result).</returns>
        public static FormResult Show(FormOptions options) {
            Form form = Build();
            options.AfterSubmit = context => Install(context);
            FormResult result = form.Show(options);
            if (!result.Submitted) {
                Terminal.WriteMarkupLine("[muted]Setup was cancelled; nothing was changed.[/]");
            }
            return result;
        }

        private static void Install(MenuContext context) {
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
                MenuRunResult result = new MenuRunResult("Toolbox Agent installed") {
                    Summary = Markup.Escape(values.Get<string>("Path")) + " · " + string.Join(", ", values.GetList<object>("Components")) + " · " + Markup.Escape(values.Get<string>("Server"))
                };
                result.Figure("steps done", steps.Length.ToString(System.Globalization.CultureInfo.CurrentCulture), StatusKind.Pass);
                return result;
            });
        }    }
}
