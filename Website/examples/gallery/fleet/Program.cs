// Fleet health check: menus, selections, review, live work and actionable results.
// Synthetic data and timings; reports and notifications stay in memory. No remote effects.
using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: replace these inventories, operations and findings with your own sources.
Node[] inventory = {
    new("DC01", "corp.example.test", "Warsaw", "Domain controller", "10.20.1.11", ""),
    new("DC02", "corp.example.test", "Warsaw", "Domain controller", "10.20.1.12", ""),
    new("FS01", "corp.example.test", "Dublin", "File", "10.30.1.21", ""),
    new("SQL01", "corp.example.test", "Dublin", "Database", "10.30.1.31", ""),
    new("WEB01", "corp.example.test", "Berlin", "Web", "10.40.1.41", ""),
    new("FS22", "corp.example.test", "Berlin", "File", "10.40.1.22", "offline since 07:12"),
    new("LAB01", "lab.example.test", "Lab", "Web", "192.168.50.40", ""),
    new("LAB02", "lab.example.test", "Lab", "Database", "192.168.50.30", ""),
};
Check[] catalog = {
    new("SecureChannel", "Secure channel", "Identity", "Tests the computer account trust with the domain.", "trusted", 1.2, false),
    new("TimeOffset", "Time offset", "Identity", "Compares the clock with the domain time source.", "under 1 s", 0.8, false),
    new("ForwardRecord", "Forward record", "DNS", "Compares the A record with the server address.", "matches IP", 0.6, false),
    new("CertExpiry", "Cert expiry", "Certificates", "Finds machine certificates expiring within 30 days.", "30+ days", 1.8, false),
    new("CertChain", "Cert chain", "Certificates", "Builds each certificate chain to a trusted root.", "trusted root", 3.5, false),
    new("MissingUpdates", "Missing updates", "Updates", "Lists missing security updates older than 14 days.", "0", 6.5, false),
    new("FreeSpace", "Free space", "Storage", "Measures free space on every fixed volume.", "15% free", 1.3, false),
    new("AutoServices", "Auto services", "Services", "Checks that automatic services are running.", "all running", 2.2, false),
};
Dictionary<string, (StatusKind Status, string Actual)> findings = new() {
    ["DC02/TimeOffset"] = (StatusKind.Fail, "4.7 s"),
    ["FS01/MissingUpdates"] = (StatusKind.Fail, "3 updates"),
    ["SQL01/FreeSpace"] = (StatusKind.Fail, "6% on data volume"),
    ["WEB01/CertExpiry"] = (StatusKind.Fail, "9 days"),
    ["WEB01/CertChain"] = (StatusKind.Warn, "intermediate ends in 40 days"),
    ["SQL01/AutoServices"] = (StatusKind.Warn, "1 restarted"),
};
List<HistoryRow> history = new() {
    new("Mon 19 Oct 21:04", 3, 4, 10, 1, 1, TimeSpan.FromSeconds(372)), new("Tue 20 Oct 07:30", 2, 3, 6, 0, 0, TimeSpan.FromSeconds(66))
};
string reportRoot = OperatingSystem.IsWindows() ? @"C:\Reports\Fleet" : "/tmp/reports/fleet";
Node[] targets = inventory.Where(n => n.Domain == "corp.example.test" && n.Offline == "").Take(5).ToArray();
Check[] checks = catalog.Where(c => new[] { "SecureChannel", "TimeOffset", "ForwardRecord", "CertExpiry", "CertChain", "MissingUpdates", "FreeSpace", "AutoServices" }.Contains(c.Id)).ToArray();
string domain = "corp.example.test", format = "Html", report = reportRoot, email = "ops@example.test";
int parallel = 3;
bool notify = false;
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 5) throw new ArgumentOutOfRangeException(nameof(step));

List<Work> Plan() => checks.SelectMany((c, ci) => targets.Select((n, ni) => {
    var outcome = findings.GetValueOrDefault(n.Name + "/" + c.Id, (StatusKind.Pass, c.Expected));
    return new Work(n, c, ni * checks.Length + ci, outcome.Item1, outcome.Item2, Math.Round(c.Seconds * (1 + ni * .17), 1));
})).ToList();
StatusKind Verdict(IEnumerable<Work> work) => work.Any(w => w.Status == StatusKind.Fail) ? StatusKind.Fail
    : work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass;
string Seconds(double seconds) => seconds.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";
Table TableOf(string[] headers) {
    Table table = new() { Border = TableBorder.Minimal, RecordViewRows = 0 };
    foreach (string header in headers) table.AddColumn(header, HorizontalAlignment.Left);
    return table;
}
IWidget Findings(IEnumerable<Work> work) {
    Table table = TableOf(new[] { "Server", "Check", "Expected", "Actual" });
    table.Columns[3].Flexible = true;
    foreach (Work w in work.Where(w => w.Status != StatusKind.Pass))
        table.AddRow(w.Node.Name, w.Check.Name, w.Check.Expected, TableCell.FromValue(RichText.Plain(w.Actual, new RichStyle(w.Status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Warn))));
    return table;
}
IWidget History() {
    Table table = TableOf(new[] { "When", "Servers", "Checks", "Result", "Duration" });
    foreach (HistoryRow h in history.AsEnumerable().Reverse())
        table.AddRow(h.When, h.Servers.ToString(), h.Checks.ToString(),
            $"{h.Passed} passed · {h.Warnings} warn · {h.Failed} failed", h.Elapsed.ToString(@"hh\:mm\:ss"));
    return table;
}
IWidget Preview(Check c) => new Rows(new TextBlock(Markup.Escape(c.Description)), new KeyValueList()
    .Add("Group", c.Category).Add("Expected", c.Expected).Add("Duration", "about " + Seconds(c.Seconds))
    .Add("Changes", c.Changes ? "[warn]yes[/]" : "[pass]no[/]").Add("Parameter", c.Id)) { Gap = 1 };

// Layout: snapshots and live work use this same widget tree.
IWidget Body(List<Work> work, int done, int phase) {
    Work[] finished = work.Take(done).ToArray();
    Work[] active = phase == 2 ? work.Skip(done).TakeWhile(w => w.Check.Id == work[done].Check.Id).Take(parallel).ToArray() : Array.Empty<Work>();
    SummaryBar summary = new SummaryBar { Elapsed = TimeSpan.FromSeconds(done * .24 / parallel) }.SetProgress(done, work.Count)
        .SetCounter("passed", finished.Count(w => w.Status == StatusKind.Pass), StatusKind.Pass)
        .SetCounter("warnings", finished.Count(w => w.Status == StatusKind.Warn), StatusKind.Warn)
        .SetCounter("failed", finished.Count(w => w.Status == StatusKind.Fail), StatusKind.Fail);
    Phases phases = new();
    string[] names = { "Discover", "Connect", "Check", "Report" };
    for (int i = 0; i < names.Length; i++) phases.Set(names[i], i < phase ? (i == 2 ? Verdict(finished) : StatusKind.Pass) : i == phase ? StatusKind.Running : StatusKind.Pending);
    Lanes lanes = new("lanes");
    foreach (string category in work.Select(w => w.Check.Category).Distinct()) {
        Work[] planned = work.Where(w => w.Check.Category == category).ToArray();
        Work[] ready = finished.Where(w => w.Check.Category == category).ToArray();
        Lane lane = lanes.Add(category, category, planned.Length); lane.Done = ready.Length;
        lane.Passed = ready.Count(w => w.Status == StatusKind.Pass); lane.Warned = ready.Count(w => w.Status == StatusKind.Warn); lane.Failed = ready.Count(w => w.Status == StatusKind.Fail);
        lane.Status = ready.Length == planned.Length ? Verdict(ready) : active.Any(w => w.Check.Category == category) ? StatusKind.Running : StatusKind.Pending;
    }
    LogRegion log = new() { ShowTime = false, PreferredHeight = 7 };
    log.Write($"Inventory: {domain} · {targets.Length} servers · {work.Count} checks");
    if (phase >= 1) log.Write($"Connecting up to {parallel} servers at a time");
    foreach (Work w in finished.TakeLast(5)) log.Write($"{w.Node.Name} · {w.Check.Name}: {w.Actual}", w.Status == StatusKind.Fail ? "err" : w.Status == StatusKind.Warn ? "warn" : "info");
    if (phase >= 3) log.Write("Preparing " + format.ToUpperInvariant() + " report: " + report);
    if (phase >= 4) log.Write(notify ? "Report ready · recipient " + email : "Report ready · " + finished.Count(w => w.Status != StatusKind.Pass) + " findings to review");
    CellMap map = new() { Columns = checks.Length, LegendLines = 2 };
    foreach (Node node in targets) map.RowLabels.Add(node.Name);
    map.Resize(targets.Length * checks.Length);
    for (int i = 0; i < map.Count; i++) if (!work.Any(w => w.Cell == i)) map.Set(i, StatusKind.Skip);
    foreach (Work w in finished) map.Set(w.Cell, w.Status);
    foreach (Work w in active) map.Set(w.Cell, StatusKind.Running);
    StatusList running = new("running");
    foreach (Work w in active) running.Add(Markup.Escape(w.Node.Name + " · " + w.Check.Name), StatusKind.Running);
    if (active.Length == 0) running.Add(phase >= 3 ? "Checks complete" : "Waiting for connections", phase >= 3 ? Verdict(finished) : StatusKind.Pending);
    KeyValueList facts = new KeyValueList().Add("Domain", Markup.Escape(domain)).Add("Servers", targets.Length + " · " + targets.Select(n => n.Site).Distinct().Count() + " sites")
        .Add("Checks", checks.Length + " · " + work.Count + " in all").Add("Report", format.ToUpperInvariant());
    Table slow = TableOf(new[] { "Check", "Server", "Took" });
    foreach (Work w in finished.OrderByDescending(w => w.Seconds).Take(5)) slow.AddRow(w.Check.Name, w.Node.Name, Seconds(w.Seconds));
    Histogram durations = new(new double[] { 0, 2, 4, 8, 16 }, "durations") { Height = 5 };
    foreach (Work w in finished) durations.Add(w.Seconds);
    Columns top = new Columns().Add(new Panel(map, "Check map"))
        .Add(new Panel(new Rows(lanes, new Rule(), facts), "Categories"), LayoutSize.Fraction(1.15))
        .Add(new Panel(running, "Running"), LayoutSize.Fraction(.95));
    top.CollapseBelow = 100;
    Columns bottom = new Columns().Add(new Panel(Findings(finished), "Findings"), LayoutSize.Fraction(1.5))
        .Add(new Panel(slow, "Slowest checks")).Add(new Panel(durations, "Durations"), LayoutSize.Fixed(20));
    bottom.CollapseBelow = 100;
    return new Rows(summary, phases, top, bottom, new Panel(log, "Log"));
}
Dashboard Dashboard(List<Work> work, int done, int phase) => new("[bold]Fleet health check[/]", Body(work, done, phase)) {
    Subtitle = $"{domain} · {targets.Length} servers · {checks.Length} checks", Footer = "Ctrl+C cancel · results keep the findings"
};

// Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
void Execute(MenuContext context, List<Work> work) {
    Slot body = new("body", Body(work, 0, 0));
    Dashboard dashboard = new("[bold]Fleet health check[/]", body) { Subtitle = domain, Footer = "Ctrl+C cancel · results keep the findings" };
    bool completed = false;
    RunResult outcome = context.Run(dashboard, run => {
        void Show(int done, int phase) { body.Handle(run.Session).Set(Body(work, done, phase)); if (run.CancellationToken.WaitHandle.WaitOne(240)) run.CancellationToken.ThrowIfCancellationRequested(); }
        Show(0, 0); Show(0, 1);
        int done = 0;
        foreach (var operation in work.GroupBy(w => w.Check.Id)) {
            foreach (Work[] batch in operation.Chunk(parallel)) { Show(done, 2); done += batch.Length; }
        }
        Show(done, 3); Show(done, 4);
        RunResult result = Result(work); completed = true; return result;
    });
    if (!completed) return;
    history.Add(new("Tue 20 Oct 09:30", work.Select(w => w.Node.Name).Distinct().Count(), work.Select(w => w.Check.Id).Distinct().Count(),
        work.Count(w => w.Status == StatusKind.Pass), work.Count(w => w.Status == StatusKind.Warn), work.Count(w => w.Status == StatusKind.Fail), outcome.Elapsed));
}
RunResult Result(List<Work> work) {
    RunResult result = new("Fleet check finished", Verdict(work)) {
        Summary = $"{work.Count} checks on {work.Select(w => w.Node.Name).Distinct().Count()} servers. Review the findings and choose the next action.", Details = Findings(work)
    };
    result.Figure("passed", work.Count(w => w.Status == StatusKind.Pass).ToString(), StatusKind.Pass);
    result.Figure("warnings", work.Count(w => w.Status == StatusKind.Warn).ToString(), work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass);
    result.Figure("failed", work.Count(w => w.Status == StatusKind.Fail).ToString(), work.Any(w => w.Status == StatusKind.Fail) ? StatusKind.Fail : StatusKind.Pass);
    result.Next("View report", ctx => ctx.ShowResult(new RunResult("Report · " + format.ToUpperInvariant(), Verdict(work)) { Summary = report, Details = Findings(work) }));
    if (work.Any(w => w.Status == StatusKind.Fail)) result.Next("Rerun failed checks", ctx => Execute(ctx, work.Where(w => w.Status == StatusKind.Fail).ToList()));
    result.Next("View history", ctx => ctx.ShowResult(new RunResult("Run history") { Details = History() }));
    return result;
}
Form Options() {
    Form form = new("Options") { Review = false, SubmitText = "Save" };
    FormPage page = form.Page("Options", "Parallelism, reports and notifications.");
    NumberField throttle = page.Number("Parallel", "Servers at a time");
    throttle.Default = parallel; throttle.Minimum = 1; throttle.Maximum = 16; throttle.WholeNumber = true; throttle.Parameter = "ThrottleLimit";
    PathField folder = page.Path("Report", "Report folder"); folder.Default = report; folder.PathKind = PathKind.Folder; folder.Parameter = "ReportPath";
    ChoiceField output = page.Select("Format", "Report format", new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
    output.Default = format; output.Parameter = "Format";
    ToggleField send = page.Toggle("Notify", "Email the report"); send.Parameter = "Notify";
    TextField recipient = page.Text("Email", "Recipient"); recipient.Default = email; recipient.Parameter = "To"; recipient.Required = true;
    recipient.VisibleWhen = v => v.Get<bool>("Notify"); recipient.Validator = (value, _) => value is string s && s.Contains('@') ? null : "Enter an email address";

    return form;
}
Form options = Options();
void ApplyOptions(FormValues values) {
    parallel = (int)values.Get<decimal>("Parallel", 3m); report = values.Get<string>("Report", reportRoot); format = values.Get<string>("Format", "Html");
    notify = values.Get<bool>("Notify"); email = values.Get<string>("Email", "ops@example.test");
}
Menu menu = new("Fleet health check") { Layout = MenuLayout.Shell, Subtitle = "Tue 20 Oct 2026 · 09:30" };
Form checkForm = new("New check") { Command = "Invoke-FleetCheck", SubmitText = "Run checks", Description = "Choose the scope, checks and run options." };
FormPage scope = checkForm.Page("Scope", "Which servers to check.");
ChoiceField domains = scope.Select("Domain", "Domain", inventory.Select(n => n.Domain).Distinct().Select(d => new Choice(d)));
domains.Default = domain; domains.Parameter = "Domain";
ChoiceField servers = scope.MultiSelect("Servers", "Servers"); servers.Minimum = 1; servers.Parameter = "ComputerName"; servers.DependsOn.Add("Domain");
servers.Loader = async (values, token) => {
    await Task.Delay(350, token);
    return inventory.Where(n => n.Domain == values.Get<string>("Domain")).Select(n => new Choice(n.Name) {
        Title = n.Name, Group = n.Site, Badge = RichText.Plain(n.Role, new RichStyle(ThemeToken.Muted)),
        Description = n.Role + " · " + n.Detail, DisabledReason = n.Offline == "" ? null : n.Offline, Selected = n.Offline == ""
    });
};
servers.Default = targets.Select(n => n.Name).ToArray();
FormPage choose = checkForm.Page("Checks", "What a healthy server reports appears in the preview.");
ChoiceField operations = choose.MultiSelect("Checks", "Checks", catalog.Select(c => new Choice(c.Id, c.Name) {
    Group = c.Category, Description = c.Description, Badge = RichText.Plain("~" + Seconds(c.Seconds), new RichStyle(ThemeToken.Muted)), Selected = checks.Contains(c)
}));
operations.Minimum = 1; operations.Parameter = "Check"; operations.Preview = c => Preview(catalog.First(k => k.Id == (string)c.Value!));
FormPage settings = checkForm.Page("Options", "How the run behaves.");
foreach (Field field in options.Fields) settings.Add(field);
menu.Preset("Morning · identity & DNS", p => p.Form(checkForm, new Dictionary<string, object?> {
    ["Servers"] = inventory.Where(n => n.Role == "Domain controller").Select(n => n.Name).ToArray(),
    ["Checks"] = new[] { "SecureChannel", "TimeOffset", "ForwardRecord" }, ["Parallel"] = 2m
}).WithPreview(new KeyValueList().Add("Scope", "Domain controllers").Add("Checks", "Identity and DNS")));
menu.Preset("Nightly · all sites", p => p.Form(checkForm, new Dictionary<string, object?> {
    ["Servers"] = inventory.Where(n => n.Domain == domain && n.Offline == "").Select(n => n.Name).ToArray(), ["Checks"] = catalog.Select(c => c.Id).ToArray(), ["Parallel"] = 3m, ["Notify"] = true
}).WithPreview(new KeyValueList().Add("Scope", "Every online server").Add("Checks", "All categories")));
menu.Preset("Patch Tuesday · updates", p => p.Form(checkForm, new Dictionary<string, object?> { ["Checks"] = new[] { "MissingUpdates", "AutoServices" }, ["Format"] = "Csv" }));
menu.Preset("Certificates · web tier", p => p.Form(checkForm, new Dictionary<string, object?> { ["Servers"] = new[] { "WEB01" }, ["Checks"] = new[] { "CertExpiry", "CertChain", "ForwardRecord" } }));
menu.Item("Presets", i => i.Hotkey('p').WithIcon(GlyphIcon.Favorite).Presets());
menu.Item("New check", i => i.Hotkey('n').WithIcon(GlyphIcon.Run).Describe("Choose a domain, servers and checks; review the command and run.").Form(checkForm, (context, result) => {
    domain = result.Values.Get<string>("Domain", domain);
    targets = inventory.Where(n => n.Domain == domain && n.Offline == "" && result.Values.GetList<string>("Servers").Contains(n.Name)).ToArray();
    checks = catalog.Where(c => result.Values.GetList<string>("Checks").Contains(c.Id)).ToArray();
    ApplyOptions(result.Values); Execute(context, Plan());
}));
menu.Item("History", i => i.Hotkey('h').WithIcon(GlyphIcon.List).Page(_ => History()).State(() => history.Count + " runs"));
menu.Separator(); menu.Exit("Quit");
if (snapshot) {
    List<Work> work = Plan();
    int done = step < 2 ? 0 : step == 2 ? Math.Min(work.Count, targets.Length + 2) : step == 3 ? work.Count * 2 / 3 : work.Count;
    int phase = step < 2 ? step : step < 4 ? 2 : step == 4 ? 3 : 4;
    Terminal.Write(Dashboard(work, done, phase));
} else menu.Run(new MenuOptions { Layout = MenuLayout.Shell, AppName = "Fleet health check gallery" });

record Node(string Name, string Domain, string Site, string Role, string Detail, string Offline);
record Check(string Id, string Name, string Category, string Description, string Expected, double Seconds, bool Changes);
record Work(Node Node, Check Check, int Cell, StatusKind Status, string Actual, double Seconds);
record HistoryRow(string When, int Servers, int Checks, int Passed, int Warnings, int Failed, TimeSpan Elapsed);
