using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using ConsoleMonster.Building;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Highlighting;
using ConsoleMonster.Input;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

namespace ConsoleMonster.Examples.Showcases {
    /// <summary>How the fleet health check showcase paces itself.</summary>
    public sealed class FleetHealthOptions {
        /// <summary>Simulated seconds per real second during the run (default 7); zero or less runs without waiting.</summary>
        public double Speed { get; set; } = 7;

        /// <summary>How long the inventory takes to list a domain's servers (default 1.1 s).</summary>
        public TimeSpan LoadDelay { get; set; } = TimeSpan.FromMilliseconds(1100);

        /// <summary>How long the finished dashboard stays before the results (default 1.2 s).</summary>
        public TimeSpan Hold { get; set; } = TimeSpan.FromMilliseconds(1200);

        /// <summary>The seed of the check durations, so every run takes the same simulated time.</summary>
        public int Seed { get; set; } = 20261020;

        /// <summary>Options that never wait (tests).</summary>
        public static FleetHealthOptions Instant => new FleetHealthOptions { Speed = 0, LoadDelay = TimeSpan.Zero, Hold = TimeSpan.Zero };
    }

    /// <summary>
    /// The complete-application showcase, "Fleet health check": a Shell app with presets, a form whose servers load for
    /// the chosen domain, grouped checks with a preview, the review with the equivalent command, and a run into a
    /// dashboard (phases, category lanes, a map of every check, running checks, failures with expected and actual values,
    /// the slowest checks, a duration histogram and a log) that ends on a results summary with next actions. The clock is
    /// simulated and the outcomes are fixed, so two recordings show the same frames. All data is invented.
    /// </summary>
    public static class FleetHealth {
        /// <summary>The app name the menu stores its settings under.</summary>
        public const string AppName = "ConsoleMonster fleet health check";

        /// <summary>The menu's title, also the first word of every header.</summary>
        public const string Title = "Fleet health check";

        /// <summary>The keys the scripted demo presses, with pauses (milliseconds) a viewer can follow.</summary>
        public static string DemoKeys { get; } = string.Join(" ", new[] {
            "wait:1500",
            // Presets: look at two of them, apply "Nightly · all sites", and go back to the sidebar.
            "Enter", "wait:900", "Down", "wait:700", "Down", "wait:600", "Up", "wait:900", "Enter", "wait:900", "Esc",
            // New check: choose the domain and watch its servers load, then look at them.
            "wait:500", "Down", "wait:700", "Enter", "wait:700", "Enter", "wait:400", "Down", "wait:400", "Up", "wait:500", "Enter",
            "wait:2200", "Enter", "wait:800", "Down", "wait:250", "Down", "wait:250", "Down", "wait:600", "Enter",
            // Checks: walk to "Cert chain" with its preview and add it.
            "wait:500", "PageDown", "wait:600", "Enter", "wait:400",
            "Down", "wait:200", "Down", "wait:200", "Down", "wait:200", "Down", "wait:200", "Down", "wait:200", "Down", "wait:200", "Down", "wait:200", "Down", "wait:300", "Down", "wait:600",
            "Space", "wait:700", "Enter",
            // Options from the preset, then the review with the equivalent command, then run.
            "wait:500", "PageDown", "wait:1200", "PageDown", "wait:2800", "Enter",
            // Results: rerun the failed checks, back to the sidebar, look at the history and quit.
            "wait:2200", "Down", "wait:600", "Enter", "wait:1500", "Shift+Tab", "wait:600", "Down", "wait:1500", "End", "wait:600", "Enter"
        });

        /// <summary>Builds the app.</summary>
        /// <param name="options">Pacing; null uses the defaults.</param>
        /// <returns>The menu.</returns>
        public static Menu Build(FleetHealthOptions? options = null) {
            FleetHealthOptions settings = options ?? new FleetHealthOptions();
            // <example>
            Menu menu = new Menu(Title) { Layout = MenuLayout.Shell };
            FleetHistory history = new FleetHistory();
            // The form: a domain whose servers load, grouped checks with a preview, options, and the review with the command.
            Form check = CheckForm(settings.LoadDelay);
            // Presets that ship with the app, listed before the ones people save; each fills in the form.
            foreach (FleetPreset scope in FleetData.Presets) {
                menu.Preset(scope.Name, preset => preset.WithPreview(FleetData.PresetPreview(scope)).Form(check, scope.Values()));
            }

            menu.Item("Presets", item => item.Hotkey('p').WithIcon(GlyphIcon.Favorite)
                .Describe("Scopes for New check. Applying one fills in the form; the domain stays your choice.")
                .Presets());
            menu.Item("New check", item => item.Hotkey('n').WithIcon(GlyphIcon.Run)
                .Describe("Choose a domain, its servers and the checks, review the equivalent command, and run.")
                .Form(check, (context, result) => {
                    // The run draws its dashboard inside the app, then shows the results with next actions.
                    FleetJob job = new FleetJob(result.Values, settings);
                    context.Run(job.Dashboard, job.Execute, job.Options);
                    job.SaveTo(history);
                }));
            menu.Item("History", item => item.Hotkey('h').WithIcon(GlyphIcon.List)
                .Describe("Earlier runs, newest first.")
                .Page(context => history.Page())
                .Detail(() => history.Count.ToString(CultureInfo.InvariantCulture) + " runs"));
            menu.Separator();
            menu.Exit("Quit");
            // </example>
            // The invented data's fixed "now", so every recording shows the same header.
            menu.Subtitle = FleetData.NowText;
            return menu;
        }

        /// <summary>Runs the app for a person at the keyboard.</summary>
        public static void Show() {
            Build().Run(new MenuOptions { AppName = AppName });
        }

        /// <summary>Runs the scripted demo on the console: every key is pressed by <see cref="DemoKeys"/> at a viewer's pace.</summary>
        public static void Demo() {
            // A new, empty settings folder: presets or choices saved by whoever runs it never show up, and nothing is left.
            string storage = Path.Combine(Path.GetTempPath(), "consolemonster-fleet-demo-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try {
                Run(new FleetHealthOptions(), new InteractiveOptions { Input = new ScriptedInput(DemoKeys) { RealTime = true } }, storage);
            } finally {
                if (Directory.Exists(storage)) {
                    Directory.Delete(storage, true);
                }
            }
        }

        /// <summary>Runs the app with given pacing and interaction (scripted input, a screen capture, a fixed profile).</summary>
        /// <param name="options">Pacing.</param>
        /// <param name="interactive">Interaction settings.</param>
        /// <param name="storageRoot">Where settings (remembered choices and saved presets) are kept; null uses the per-user folder.</param>
        public static void Run(FleetHealthOptions options, InteractiveOptions interactive, string? storageRoot = null) {
            Build(options).Run(new MenuOptions { Layout = MenuLayout.Shell, AppName = AppName, Interactive = interactive, StorageRoot = storageRoot });
        }

        // ---- the form ----------------------------------------------------------------------------------------------

        private static Form CheckForm(TimeSpan loadDelay) {
            Form form = new Form("New check") {
                Command = "Invoke-FleetCheck",
                Description = "Checks the health of every chosen server.",
                SubmitText = "Run checks"
            };

            FormPage target = form.Page("Scope", "Which servers to check.");
            ChoiceField domain = target.Select("Domain", "Domain", FleetData.DomainChoices());
            domain.Required = true;
            domain.Parameter = "Domain";
            domain.Help = "Choosing a domain loads its servers from the inventory.";
            ChoiceField servers = target.MultiSelect("Servers", "Servers");
            servers.DependsOn.Add("Domain");
            servers.Minimum = 1;
            servers.Parameter = "ComputerName";
            servers.Loader = (values, token) => FleetData.LoadServersAsync(values.Get<string>("Domain"), loadDelay, token);
            servers.Help = "Grouped by site; offline servers cannot be picked. Space toggles, Ctrl+A selects all.";

            FormPage checks = form.Page("Checks", "What to check on every server.");
            List<Choice> checkChoices = FleetData.CheckChoices();
            foreach (Choice c in checkChoices) {
                c.Selected = (string)c.Value! == "SecureChannel" || (string)c.Value! == "TimeOffset" || (string)c.Value! == "ForwardRecord";
            }
            ChoiceField list = checks.MultiSelect("Checks", "Checks", checkChoices);
            list.Minimum = 1;
            list.Parameter = "Check";
            list.Preview = c => FleetData.CheckPreview(FleetData.Check((string)c.Value!));
            list.Help = "Grouped by category; the preview shows what a healthy server reports.";

            FormPage run = form.Page("Options", "How the run behaves.");
            NumberField parallel = run.Number("Parallel", "Servers at a time");
            parallel.Integer = true;
            parallel.Minimum = 1;
            parallel.Maximum = 16;
            parallel.Default = 4m;
            parallel.Parameter = "ThrottleLimit";
            ChoiceField format = run.Select("Format", "Report format", new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
            format.Default = "Html";
            format.Parameter = "Format";
            PathField report = run.Path("Report", "Report folder");
            report.PathKind = PathKind.Folder;
            report.Default = Path.Combine("Reports", "Fleet");
            report.Parameter = "ReportPath";
            ToggleField notify = run.Toggle("Notify", "Email the report");
            notify.Parameter = "Notify";
            TextField email = run.Text("Email", "Recipient");
            email.VisibleWhen = v => v.Get<bool>("Notify");
            email.Default = "ops@contoso.com";
            email.Parameter = "To";
            email.Validator = (value, values) => value is string s && s.Contains("@") ? null : "Enter an email address";
            return form;
        }

        // ---- the run -----------------------------------------------------------------------------------------------

        private static MenuRunResult Check(RunContext run, FleetView view, FleetPlan plan, SimulatedClock clock, FleetHealthOptions settings, FleetTally tally) {
            LiveSession session = run.Session;
            CancellationToken token = run.CancellationToken;
            // The clock moves at the options' speed; a stopped run (Ctrl+C) ends at the next wait.
            void Wait(double seconds) {
                clock.Wait(seconds, token);
                token.ThrowIfCancellationRequested();
            }
            Random random = new Random(settings.Seed);
            PhasesHandle phases = view.Phases.Handle(session);
            LanesHandle lanes = view.Lanes.Handle(session);
            LiveTableHandle failures = view.Failures.Handle(session);
            LiveTableHandle slowest = view.Slowest.Handle(session);
            SparklineHandle durations = view.Durations.Handle(session);
            ITextHighlighter highlighter = Highlighters.Smart;
            void Log(string text, string level = "info") => session.Post(new LiveEvent("log", "write", text: HighlightStyles.ToMarkup(text, highlighter), status: level, isMarkup: true));
            void Status(string text, StatusKind state) => session.Post(new LiveEvent("dashboard", "status", text: text, status: state.ToString()));
            void Activity(string key, string text, double percent = -1) => session.Post(new LiveEvent("running", "update", key: key, text: text, value: percent, total: -1));
            void Done(string key) => session.Post(new LiveEvent("running", "complete", key: key));
            // Each activity has its own key: connecting a server, one check on one server, writing the report.
            string ConnectKey(int s) => "connect:" + s.ToString(CultureInfo.InvariantCulture);
            string CheckKey(int s, int c) => "check:" + s.ToString(CultureInfo.InvariantCulture) + ":" + c.ToString(CultureInfo.InvariantCulture);

            // Every duration is drawn up front, server by server, so the schedule never changes what the seed gives.
            double[] connect = plan.Servers.Select(s => 0.4 + random.NextDouble() * 0.7).ToArray();
            double[,] seconds = new double[plan.Servers.Count, plan.Checks.Count];
            for (int s = 0; s < plan.Servers.Count; s++) {
                for (int c = 0; c < plan.Checks.Count; c++) {
                    // A check that finds a problem looks closer (a retry, a second source), so it takes a few seconds longer.
                    double retry = FleetData.Outcome(plan.Servers[s].Name, plan.Checks[c].Id) == null ? 0 : 3;
                    seconds[s, c] = Math.Round(plan.Checks[c].Seconds * plan.Servers[s].Slowness * (0.6 + random.NextDouble() * 0.9) + retry, 1);
                }
            }

            // Discover: the inventory and the plan.
            Status("discovering", StatusKind.Running);
            phases.Start("Discover");
            Log("Reading the inventory of " + plan.Domain);
            Wait(0.8);
            List<FleetServer> inDomain = FleetData.Servers.Where(s => s.Domain == plan.Domain).ToList();
            Log(inDomain.Count + " servers in " + inDomain.Select(s => s.Site).Distinct().Count() + " sites; " + plan.Servers.Count + " selected");
            foreach (FleetServer offline in inDomain.Where(s => s.Offline != null)) {
                Log(offline.Name + " skipped: " + offline.Offline, "warn");
            }
            Wait(0.9);
            Log(plan.Total + " checks planned: " + plan.Servers.Count + " servers, " + plan.Checks.Count + " checks, " + plan.Parallel + " at a time");
            Wait(0.5);
            phases.Complete("Discover", StatusKind.Pass, plan.Servers.Count + " servers");

            // Connect: a remote session per server, several at a time.
            Status("connecting", StatusKind.Running);
            phases.Start("Connect");
            Schedule(clock, token, plan.Servers.Count, plan.Parallel, s => new[] { connect[s] }, (s, c) => Activity(ConnectKey(s), "Connecting " + plan.Servers[s].Name), (s, c, took) => {
                Done(ConnectKey(s));
                Log("Connected to " + plan.Servers[s].Name + " (" + plan.Servers[s].Address + ") in " + Seconds(took));
            }, s => { });
            phases.Complete("Connect", StatusKind.Pass, plan.Servers.Count + "/" + plan.Servers.Count);

            // Check: every server runs its checks in order; the busiest part of the screen.
            Status("checking", StatusKind.Running);
            phases.Start("Check");
            int done = 0;
            int passed = 0;
            int warned = 0;
            int failed = 0;
            Dictionary<string, int> laneDone = new Dictionary<string, int>(StringComparer.Ordinal);
            Dictionary<string, StatusKind> laneWorst = new Dictionary<string, StatusKind>(StringComparer.Ordinal);
            List<FleetSlowCheck> slow = new List<FleetSlowCheck>();
            List<FleetFinding> findings = new List<FleetFinding>();
            double[] serverSeconds = new double[plan.Servers.Count];
            Schedule(clock, token, plan.Servers.Count, plan.Parallel, s => Enumerable.Range(0, plan.Checks.Count).Select(c => seconds[s, c]).ToArray(), (s, c) => {
                FleetCheck check = plan.Checks[c];
                session.Post(new LiveEvent("cells", "set", key: plan.Cell(s, c).ToString(CultureInfo.InvariantCulture), status: StatusKind.Running.ToString()));
                Activity(CheckKey(s, c), plan.Servers[s].Name + " · " + check.Name);
                if (!laneDone.ContainsKey(check.Category)) {
                    laneDone[check.Category] = 0;
                    laneWorst[check.Category] = StatusKind.Pass;
                    lanes.Start(check.Category, plan.Servers[s].Name + " · " + check.Name);
                }
            }, (s, c, took) => {
                FleetServer server = plan.Servers[s];
                FleetCheck check = plan.Checks[c];
                FleetOutcome? outcome = FleetData.Outcome(server.Name, check.Id);
                StatusKind status = outcome?.Status ?? StatusKind.Pass;
                done++;
                serverSeconds[s] += took;
                Done(CheckKey(s, c));
                session.Post(new LiveEvent("cells", "set", key: plan.Cell(s, c).ToString(CultureInfo.InvariantCulture), status: status.ToString()));
                lanes.Advance(check.Category, status, server.Name + " · " + check.Name);
                laneDone[check.Category]++;
                if (status == StatusKind.Fail || (status == StatusKind.Warn && laneWorst[check.Category] == StatusKind.Pass)) {
                    laneWorst[check.Category] = status;
                }
                if (laneDone[check.Category] == plan.LaneTotal(check.Category)) {
                    lanes.Complete(check.Category, laneWorst[check.Category]);
                }
                switch (status) {
                    case StatusKind.Fail:
                        failed++;
                        findings.Add(new FleetFinding(server.Name, check, outcome!));
                        failures.AddRow(server.Name + "/" + check.Id, server.Name, check.Name, check.Expected, new TableCell("[fail]" + Markup.Escape(outcome!.Actual) + "[/]"));
                        Log(server.Name + " · " + check.Name + ": " + outcome.Actual + ", expected " + check.Expected, "err");
                        break;
                    case StatusKind.Warn:
                        warned++;
                        Log(server.Name + " · " + check.Name + ": " + outcome!.Actual + ", expected " + check.Expected, "warn");
                        break;
                    default:
                        passed++;
                        break;
                }
                session.Post(new LiveEvent("summary", "progress", value: done, total: plan.Total));
                session.Post(new LiveEvent("summary", "counter", key: "passed", value: passed, status: StatusKind.Pass.ToString()));
                session.Post(new LiveEvent("summary", "counter", key: "warnings", value: warned, status: StatusKind.Warn.ToString()));
                session.Post(new LiveEvent("summary", "counter", key: "failed", value: failed, status: StatusKind.Fail.ToString()));
                durations.Add(took);
                if (FleetSlowCheck.Offer(slow, new FleetSlowCheck(check.Name, server.Name, took), 5)) {
                    for (int i = 0; i < slow.Count; i++) {
                        slowest.UpdateRow((i + 1).ToString(CultureInfo.InvariantCulture), slow[i].Check, slow[i].Server, slow[i].Seconds.ToString("0.0", CultureInfo.InvariantCulture) + "s");
                    }
                }
            }, s => Log(plan.Servers[s].Name + " finished " + plan.Checks.Count + " checks in " + Seconds(serverSeconds[s])));
            phases.Complete("Check", failed > 0 ? StatusKind.Fail : warned > 0 ? StatusKind.Warn : StatusKind.Pass, failed + " failed");

            // Report: the file, then the email.
            Status("writing the report", StatusKind.Running);
            phases.Start("Report");
            for (int step = 1; step <= 4; step++) {
                Activity("report", "Writing " + plan.ReportFile, step * 25);
                Wait(0.6);
            }
            Done("report");
            Log("Report saved: " + plan.ReportPath + " (412 KB)");
            if (plan.Notify) {
                Wait(0.5);
                Log("Report emailed to " + plan.Email);
            }
            phases.Complete("Report", StatusKind.Pass, plan.Format.ToUpperInvariant());
            StatusKind overall = failed > 0 ? StatusKind.Fail : warned > 0 ? StatusKind.Warn : StatusKind.Pass;
            Status(failed + " failed · " + warned + " warnings", overall);
            clock.Hold(settings.Hold.TotalSeconds, token);
            token.ThrowIfCancellationRequested();

            tally.Set(passed, warned, failed, clock.Elapsed);
            Table details = new Table { Border = TableBorder.Minimal };
            details.AddColumn("Server");
            details.AddColumn("Check");
            details.AddColumn("Expected");
            details.AddColumn(new TableColumn("Actual") { Flexible = true });
            foreach (FleetFinding finding in findings) {
                details.AddRow(finding.Server, Markup.Escape(finding.Check.Name), Markup.Escape(finding.Check.Expected), "[fail]" + Markup.Escape(finding.Outcome.Actual) + "[/]");
            }
            MenuRunResult result = new MenuRunResult("Fleet check finished", overall) {
                Summary = plan.Total + " checks on " + plan.Servers.Count + " servers in " + plan.Domain + ". "
                    + (failed > 0 ? failed + " need action now; " + warned + " warnings to review." : warned > 0 ? warned + " warnings to review; nothing failed." : "Nothing needs action."),
                Details = findings.Count > 0 ? details : null
            };
            result.Figure("passed", passed.ToString(CultureInfo.InvariantCulture), StatusKind.Pass);
            result.Figure("warnings", warned.ToString(CultureInfo.InvariantCulture), warned > 0 ? StatusKind.Warn : StatusKind.Pass);
            result.Figure("failed", failed.ToString(CultureInfo.InvariantCulture), failed > 0 ? StatusKind.Fail : StatusKind.Pass);
            result.Figure("servers", plan.Servers.Count.ToString(CultureInfo.InvariantCulture));
            result.Next("Open the report", context => context.Toast("Opened " + plan.ReportFile, StatusKind.Info));
            if (failed > 0) {
                result.Next("Rerun the failed checks", context => context.Toast("Queued " + failed + " failed checks for a rerun", StatusKind.Info));
            }
            result.Next("Export findings to CSV", context => context.Toast("Saved findings-" + FleetData.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + ".csv"));
            return result;
        }

        /// <summary>
        /// Runs work items on workers in simulated time: each item is a sequence of steps that run one after another on the
        /// same worker; a free worker takes the next item. Finishing order follows the simulated clock, never the real one.
        /// </summary>
        private static void Schedule(SimulatedClock clock, CancellationToken token, int items, int workers, Func<int, double[]> steps, Action<int, int> started, Action<int, int, double> finished, Action<int> itemDone) {
            List<FleetSlot> running = new List<FleetSlot>();
            int next = 0;
            while (next < items || running.Count > 0) {
                while (running.Count < Math.Max(1, workers) && next < items) {
                    double[] sequence = steps(next);
                    FleetSlot slot = new FleetSlot(next, sequence, clock.Seconds);
                    running.Add(slot);
                    started(slot.Item, 0);
                    next++;
                }
                double soonest = running.Min(r => r.Ends);
                clock.WaitUntil(soonest, token);
                token.ThrowIfCancellationRequested();
                // Whole ticks, as the clock counts them, so a step that ends exactly now is never missed by rounding.
                long now = clock.Elapsed.Ticks;
                foreach (FleetSlot slot in running.Where(r => (long)Math.Round(r.Ends * TimeSpan.TicksPerSecond) <= now).OrderBy(r => r.Ends).ThenBy(r => r.Item).ToList()) {
                    finished(slot.Item, slot.Step, slot.Steps[slot.Step]);
                    if (slot.Step + 1 < slot.Steps.Length) {
                        slot.Advance(clock.Seconds);
                        started(slot.Item, slot.Step);
                    } else {
                        running.Remove(slot);
                        itemDone(slot.Item);
                    }
                }
            }
        }

        private static string Seconds(double seconds) => seconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";

        // ---- the pieces ----------------------------------------------------------------------------------------------

        /// <summary>What the submitted form asks for.</summary>
        private sealed class FleetPlan {
            public FleetPlan(FormValues values) {
                Domain = values.Get<string>("Domain");
                Servers = values.GetList<string>("Servers").Select(FleetData.Server).ToList();
                HashSet<string> chosen = new HashSet<string>(values.GetList<string>("Checks"), StringComparer.OrdinalIgnoreCase);
                Checks = FleetData.Checks.Where(c => chosen.Contains(c.Id)).ToList();
                Parallel = (int)values.Get<decimal>("Parallel", 4m);
                Format = values.Get<string>("Format") ?? "Html";
                ReportFolder = values.Get<string>("Report") ?? Path.Combine("Reports", "Fleet");
                Notify = values.Get<bool>("Notify");
                Email = values.Get<string>("Email");
            }

            public string Domain { get; }

            public List<FleetServer> Servers { get; }

            public List<FleetCheck> Checks { get; }

            public int Parallel { get; }

            public string Format { get; }

            public string ReportFolder { get; }

            public bool Notify { get; }

            public string? Email { get; }

            public int Total => Servers.Count * Checks.Count;

            public IEnumerable<string> Categories => FleetData.Categories.Where(c => Checks.Any(k => k.Category == c));

            public string ReportFile => "fleet-" + FleetData.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture) + "." + Format.ToLowerInvariant();

            public string ReportPath => Path.Combine(ReportFolder, ReportFile);

            public int LaneTotal(string category) => Servers.Count * Checks.Count(c => c.Category == category);

            public int Cell(int server, int check) => server * Checks.Count + check;
        }

        /// <summary>The run's dashboard and the live widgets the run updates.</summary>
        private sealed class FleetView {
            public FleetView(FleetPlan plan) {
                Summary = new SummaryBar("summary") { BarWidth = 24 };
                Summary.SetProgress(0, plan.Total);
                Summary.SetCounter("passed", 0, StatusKind.Pass);
                Summary.SetCounter("warnings", 0, StatusKind.Warn);
                Summary.SetCounter("failed", 0, StatusKind.Fail);
                Phases = new Phases().Set("Discover").Set("Connect").Set("Check").Set("Report");
                // One row per server and one column per check, with the server names beside the rows and one legend below.
                Cells = new CellMap("cells") { Columns = plan.Checks.Count, LegendLines = 2 };
                foreach (FleetServer server in plan.Servers) {
                    Cells.RowLabels.Add(server.Name);
                }
                Cells.Resize(plan.Total);
                Lanes = new Lanes("categories") { MaxLabelWidth = 12 };
                foreach (string category in plan.Categories) {
                    Lanes.Add(category, category, plan.LaneTotal(category));
                }
                KeyValueList facts = new KeyValueList("facts")
                    .Add("Domain", "[accent]" + Markup.Escape(plan.Domain) + "[/]")
                    .Add("Servers", plan.Servers.Count + " [muted]· " + plan.Servers.Select(s => s.Site).Distinct().Count() + " sites[/]")
                    .Add("Checks", plan.Checks.Count + " [muted]· " + plan.Total + " in all[/]")
                    .Add("Report", plan.Format.ToUpperInvariant() + (plan.Notify ? " [muted]· emailed[/]" : string.Empty));
                Running = new ActivityList("running") { MaxVisible = 8 };
                Table failureColumns = new Table { Border = TableBorder.Minimal };
                failureColumns.AddColumn("Server");
                failureColumns.AddColumn("Check");
                failureColumns.AddColumn("Expected");
                failureColumns.AddColumn(new TableColumn("Actual") { Flexible = true });
                Failures = new LiveTable(failureColumns, "failures") { MaxRows = 5 };
                Table slowColumns = new Table { Border = TableBorder.Minimal };
                slowColumns.AddColumn("Check");
                slowColumns.AddColumn("Server");
                slowColumns.AddColumn(new TableColumn("Took") { Alignment = HorizontalAlignment.Right });
                Slowest = new LiveTable(slowColumns, "slowest");
                Durations = new Histogram(new double[] { 0, 2, 4, 8, 16 }, "durations") { Height = 5 };
                Dashboard = new DashboardBuilder("[bold]" + Title + "[/]")
                    .Subtitle(Markup.Escape(plan.Domain))
                    .Add(Summary)
                    .Add(Phases)
                    .Columns(c => c
                        .Panel("Check map", Cells, 1.0)
                        .Panel("Categories", new Rows(Lanes, new Rule(), facts), 1.15)
                        .Panel("Running", Running, 0.95, plan.Parallel + " workers"))
                    .Columns(c => c
                        .Panel("Failures", Failures, 1.52)
                        .Panel("Slowest checks", Slowest, 1)
                        .AddFixed(new Panel(Durations, "Durations") { PaddingLeft = 0, PaddingRight = 0 }, 17))
                    .Panel("Log", new LogRegion("log") { PreferredHeight = 6 }, fill: true)
                    .BuildDashboard();
            }

            public SummaryBar Summary { get; }

            public Phases Phases { get; }

            public CellMap Cells { get; }

            public Lanes Lanes { get; }

            public ActivityList Running { get; }

            public LiveTable Failures { get; }

            public LiveTable Slowest { get; }

            public Histogram Durations { get; }

            public Dashboard Dashboard { get; }
        }

        /// <summary>
        /// One run of the form's choices: the plan, its dashboard and simulated clock, the work that drives them, and the
        /// record it leaves in the history.
        /// </summary>
        private sealed class FleetJob {
            private readonly FleetPlan _plan;
            private readonly FleetView _view;
            private readonly SimulatedClock _clock;
            private readonly FleetHealthOptions _settings;
            private readonly FleetTally _tally = new FleetTally();

            public FleetJob(FormValues values, FleetHealthOptions settings) {
                _plan = new FleetPlan(values);
                _view = new FleetView(_plan);
                _settings = settings;
                _clock = new SimulatedClock(settings.Speed);
                Options = _clock.ToSessionOptions();
            }

            public Dashboard Dashboard => _view.Dashboard;

            public LiveSessionOptions Options { get; }

            public MenuRunResult Execute(RunContext run) => Check(run, _view, _plan, _clock, _settings, _tally);

            // Called on the menu's thread once the run has returned; a stopped run leaves nothing.
            public void SaveTo(FleetHistory history) {
                if (_tally.Finished) {
                    history.Add(new FleetRun(FleetData.Now, _plan.Domain, _plan.Servers.Count, _plan.Checks.Count, _tally.Passed, _tally.Warnings, _tally.Failed, _tally.Elapsed));
                }
            }
        }

        /// <summary>What a run counted, set by the run's work before it returns.</summary>
        private sealed class FleetTally {
            public int Passed { get; private set; }

            public int Warnings { get; private set; }

            public int Failed { get; private set; }

            public TimeSpan Elapsed { get; private set; }

            public bool Finished { get; private set; }

            public void Set(int passed, int warnings, int failed, TimeSpan elapsed) {
                Finished = true;
                Passed = passed;
                Warnings = warnings;
                Failed = failed;
                Elapsed = elapsed;
            }
        }

        /// <summary>A worker's current item: its steps and when the current one ends.</summary>
        private sealed class FleetSlot {
            public FleetSlot(int item, double[] steps, double now) {
                Item = item;
                Steps = steps;
                Ends = now + steps[0];
            }

            public int Item { get; }

            public double[] Steps { get; }

            public int Step { get; private set; }

            public double Ends { get; private set; }

            public void Advance(double now) {
                Step++;
                Ends = now + Steps[Step];
            }
        }

        /// <summary>A check that failed on a server.</summary>
        private sealed class FleetFinding {
            public FleetFinding(string server, FleetCheck check, FleetOutcome outcome) {
                Server = server;
                Check = check;
                Outcome = outcome;
            }

            public string Server { get; }

            public FleetCheck Check { get; }

            public FleetOutcome Outcome { get; }
        }

        /// <summary>One of the slowest checks so far.</summary>
        private sealed class FleetSlowCheck {
            public FleetSlowCheck(string check, string server, double seconds) {
                Check = check;
                Server = server;
                Seconds = seconds;
            }

            public string Check { get; }

            public string Server { get; }

            public double Seconds { get; }

            // Keeps the slowest few, slowest first; returns whether the list changed.
            public static bool Offer(List<FleetSlowCheck> list, FleetSlowCheck candidate, int keep) {
                if (list.Count >= keep && candidate.Seconds <= list[list.Count - 1].Seconds) {
                    return false;
                }
                int index = list.FindIndex(s => candidate.Seconds > s.Seconds);
                list.Insert(index < 0 ? list.Count : index, candidate);
                if (list.Count > keep) {
                    list.RemoveAt(list.Count - 1);
                }
                return true;
            }
        }

        /// <summary>A finished run in the history.</summary>
        private sealed class FleetRun {
            public FleetRun(DateTime when, string domain, int servers, int checks, int passed, int warnings, int failed, TimeSpan elapsed) {
                When = when;
                Domain = domain;
                Servers = servers;
                Checks = checks;
                Passed = passed;
                Warnings = warnings;
                Failed = failed;
                Elapsed = elapsed;
            }

            public DateTime When { get; }

            public string Domain { get; }

            public int Servers { get; }

            public int Checks { get; }

            public int Passed { get; }

            public int Warnings { get; }

            public int Failed { get; }

            public TimeSpan Elapsed { get; }
        }

        /// <summary>The runs of this session after a few earlier ones (invented), newest first on the page.</summary>
        private sealed class FleetHistory {
            private readonly List<FleetRun> _runs = new List<FleetRun> {
                new FleetRun(new DateTime(2026, 10, 18, 2, 0, 0), "corp.contoso.com", 11, 10, 104, 5, 1, TimeSpan.FromSeconds(83)),
                new FleetRun(new DateTime(2026, 10, 19, 2, 0, 0), "corp.contoso.com", 11, 10, 103, 6, 1, TimeSpan.FromSeconds(86)),
                new FleetRun(new DateTime(2026, 10, 19, 7, 0, 0), "fabrikam.net", 2, 6, 12, 0, 0, TimeSpan.FromSeconds(14))
            };

            public int Count => _runs.Count;

            public void Add(FleetRun run) => _runs.Add(run);

            public IWidget Page() {
                Table table = new Table { Border = TableBorder.Minimal };
                table.AddColumn("When");
                table.AddColumn("Domain");
                table.AddColumn(new TableColumn("Servers") { Alignment = HorizontalAlignment.Right });
                table.AddColumn(new TableColumn("Checks") { Alignment = HorizontalAlignment.Right });
                table.AddColumn("Result");
                table.AddColumn(new TableColumn("Took") { Alignment = HorizontalAlignment.Right });
                foreach (FleetRun run in Enumerable.Reverse(_runs)) {
                    StatusKind status = run.Failed > 0 ? StatusKind.Fail : run.Warnings > 0 ? StatusKind.Warn : StatusKind.Pass;
                    table.AddRow(run.When.ToString("ddd d MMM HH:mm", CultureInfo.InvariantCulture), Markup.Escape(run.Domain), run.Servers.ToString(CultureInfo.InvariantCulture),
                        (run.Servers * run.Checks).ToString(CultureInfo.InvariantCulture),
                        TableCell.ForStatus(status, run.Passed + " passed · " + run.Warnings + " warn · " + run.Failed + " failed"), NumberFormat.Clock(run.Elapsed));
                }
                return table;
            }
        }
    }
}
