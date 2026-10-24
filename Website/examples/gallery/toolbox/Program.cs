// Server toolbox: menus, selections, review, live work and actionable results.
// Synthetic data and timings; reports and notifications stay in memory. No remote effects.
using ConsoleMonster.Interactive;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: replace these inventories, operations and findings with your own sources.
Node[] inventory = [
    new(Name: "FS01", Domain: "lab.example.test", Site: "Warsaw", Role: "File", Detail: "Windows Server 2022", Offline: ""),
    new(Name: "FS02", Domain: "lab.example.test", Site: "Warsaw", Role: "File", Detail: "Windows Server 2022", Offline: ""),
    new(Name: "WEB01", Domain: "lab.example.test", Site: "Warsaw", Role: "Web", Detail: "Windows Server 2025", Offline: ""),
    new(Name: "SQL01", Domain: "lab.example.test", Site: "Warsaw", Role: "Database", Detail: "Windows Server 2019", Offline: ""),
    new(Name: "FS11", Domain: "lab.example.test", Site: "Berlin", Role: "File", Detail: "Windows Server 2022", Offline: ""),
    new(Name: "WEB11", Domain: "lab.example.test", Site: "Berlin", Role: "Web", Detail: "Windows Server 2025", Offline: ""),
    new(Name: "PRN11", Domain: "lab.example.test", Site: "Berlin", Role: "Print", Detail: "Windows Server 2016", Offline: "offline since 09:12"),
    new(Name: "BKP21", Domain: "lab.example.test", Site: "Dublin", Role: "Backup", Detail: "Windows Server 2022", Offline: ""),
    new(Name: "SQL21", Domain: "lab.example.test", Site: "Dublin", Role: "Database", Detail: "Windows Server 2022", Offline: ""),
    new(Name: "LAB01", Domain: "lab.example.test", Site: "Lab", Role: "Test", Detail: "Windows Server 2025", Offline: ""),
    new(Name: "LAB02", Domain: "lab.example.test", Site: "Lab", Role: "Test", Detail: "Windows Server 2025", Offline: "maintenance until Friday"),
];
Check[] catalog = [
    new(Id: "DiskCleanup", Name: "Disk cleanup", Category: "Maintenance", Description: "Removes temporary files and old update caches.", Expected: "cache under 1 GB", Minutes: 3, Changes: true),
    new(Id: "LogRotation", Name: "Log rotation", Category: "Maintenance", Description: "Compresses logs older than 7 days.", Expected: "retention 90 days", Minutes: 2, Changes: true),
    new(Id: "RestartPending", Name: "Pending restart check", Category: "Maintenance", Description: "Reports restarts waiting after updates.", Expected: "no restart pending", Minutes: 1, Changes: false),
    new(Id: "PatchStatus", Name: "Patch status", Category: "Security", Description: "Lists missing security updates.", Expected: "0 missing", Minutes: 2, Changes: false),
    new(Id: "Certificates", Name: "Certificate expiry", Category: "Security", Description: "Finds certificates expiring within 30 days.", Expected: "30+ days", Minutes: 1, Changes: false),
    new(Id: "LocalAdmins", Name: "Local administrators audit", Category: "Security", Description: "Compares local Administrators with the approved list.", Expected: "approved list", Minutes: 1, Changes: false),
    new(Id: "Firewall", Name: "Firewall rules review", Category: "Security", Description: "Flags inbound allow rules open to any address.", Expected: "scoped rules", Minutes: 2, Changes: false),
    new(Id: "EventErrors", Name: "Event log errors", Category: "Health", Description: "Summarizes errors from the last 24 hours.", Expected: "under 20", Minutes: 1, Changes: false),
    new(Id: "Services", Name: "Service health", Category: "Health", Description: "Checks and restarts stopped automatic services.", Expected: "all running", Minutes: 1, Changes: true),
    new(Id: "DiskSpace", Name: "Disk space", Category: "Health", Description: "Warns below 15% and fails below 5% free.", Expected: "15% free", Minutes: 1, Changes: false),
    new(Id: "Uptime", Name: "Uptime and reboots", Category: "Health", Description: "Reports unexpected reboots in the last 30 days.", Expected: "0 unexpected", Minutes: 1, Changes: false),
];
Dictionary<string, (StatusKind Status, string Actual)> findings = new() {
    ["FS02/PatchStatus"] = (StatusKind.Fail, "3 updates missing for 41 days"),
    ["WEB01/Certificates"] = (StatusKind.Warn, "expires in 24 days"),
    ["SQL01/DiskSpace"] = (StatusKind.Fail, "4% free on data volume"),
    ["FS11/Services"] = (StatusKind.Warn, "1 service restarted"),
    ["FS01/DiskCleanup"] = (StatusKind.Warn, "1.4 GB remains"),
};
List<HistoryRow> history = [
    new(When: "Mon 19 Oct 21:04", Servers: 3, Tasks: 4, Passed: 10, Warnings: 1, Failed: 1, Elapsed: TimeSpan.FromSeconds(372)),
    new(When: "Tue 20 Oct 07:30", Servers: 2, Tasks: 3, Passed: 6, Warnings: 0, Failed: 0, Elapsed: TimeSpan.FromSeconds(66)),
];
string reportRoot = "Reports/Toolbox";
Node[] targets = inventory.Where(n => n.Domain == "lab.example.test" && n.Offline == "").Take(4).ToArray();
Check[] checks = catalog.Where(c => new[] { "DiskCleanup", "PatchStatus", "Certificates", "DiskSpace" }.Contains(c.Id)).ToArray();
string domain = "lab.example.test", format = "Html", report = reportRoot, email = "ops@example.test";
int parallel = 3;
bool notify = false, dryRun = false;

List<Work> Plan() => checks.SelectMany(c => targets.Select(n => {
    (StatusKind Status, string Actual) outcome = findings.GetValueOrDefault(n.Name + "/" + c.Id, (StatusKind.Pass, c.Expected));
    return new Work(n, c, outcome.Status, outcome.Actual);
})).ToList();
StatusKind Verdict(IEnumerable<Work> work) => work.Any(w => w.Status == StatusKind.Fail) ? StatusKind.Fail
    : work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass;
Table TableOf(string[] headers) {
    Table table = new() { Border = TableBorder.Minimal, RecordViewRows = 0 };
    foreach (string header in headers) table.AddColumn(header, HorizontalAlignment.Left);
    return table;
}
IWidget Findings(IEnumerable<Work> work) {
    Table table = TableOf(new[] { "Server", "Task", "Expected", "Actual" });
    table.Columns[3].Flexible = true;
    foreach (Work w in work.Where(w => w.Status != StatusKind.Pass))
        table.AddRow(w.Node.Name, w.Check.Name, w.Check.Expected, TableCell.FromValue(RichText.Plain(w.Actual, new RichStyle(w.Status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Warn))));
    return table;
}
IWidget History() {
    Table table = TableOf(new[] { "When", "Servers", "Tasks", "Result", "Duration" });
    foreach (HistoryRow h in history.AsEnumerable().Reverse())
        table.AddRow(h.When, h.Servers.ToString(), h.Tasks.ToString(),
            $"{h.Passed} passed · {h.Warnings} warn · {h.Failed} failed", h.Elapsed.ToString(@"hh\:mm\:ss"));
    return table;
}
IWidget Preview(Check c) => new Rows(new TextBlock(Markup.Escape(c.Description)), new KeyValueList()
    .Add("Group", c.Category).Add("Expected", c.Expected).Add("Duration", "about " + c.Minutes + " min per server")
    .Add("Changes", c.Changes ? "[warn]yes[/]" : "[pass]no[/]").Add("Parameter", c.Id)) { Gap = 1 };

// Layout: build the dashboard from the completed and active work.
IWidget Body(List<Work> work, int done, int phase) {
    Work[] finished = work.Take(done).ToArray();
    Work[] active = phase == 2 ? work.Skip(done).TakeWhile(w => w.Check.Id == work[done].Check.Id).Take(parallel).ToArray() : Array.Empty<Work>();
    SummaryBar summary = new SummaryBar { Elapsed = TimeSpan.FromSeconds(done * .24 / parallel) }.SetProgress(done, work.Count)
        .SetCounter("passed", finished.Count(w => w.Status == StatusKind.Pass), StatusKind.Pass)
        .SetCounter("warnings", finished.Count(w => w.Status == StatusKind.Warn), StatusKind.Warn)
        .SetCounter("failed", finished.Count(w => w.Status == StatusKind.Fail), StatusKind.Fail);
    Phases phases = new();
    string[] names = { "Discover", "Connect", "Tasks", "Report" };
    for (int i = 0; i < names.Length; i++) phases.Set(names[i], i < phase ? (i == 2 ? Verdict(finished) : StatusKind.Pass) : i == phase ? StatusKind.Running : StatusKind.Pending);
    Lanes lanes = new("lanes");
    foreach (Node node in targets) {
        Work[] planned = work.Where(w => w.Node == node).ToArray();
        Work[] ready = finished.Where(w => w.Node == node).ToArray();
        Lane lane = lanes.Add(node.Name, RichText.Plain(node.Name + "  " + node.Site), planned.Length); lane.Done = ready.Length;
        lane.Passed = ready.Count(w => w.Status == StatusKind.Pass); lane.Warned = ready.Count(w => w.Status == StatusKind.Warn); lane.Failed = ready.Count(w => w.Status == StatusKind.Fail);
        lane.Status = ready.Length == planned.Length ? Verdict(ready) : active.Any(w => w.Node == node) ? StatusKind.Running : StatusKind.Pending;
        lane.Detail = active.FirstOrDefault(w => w.Node == node)?.Check.Name ?? ready.LastOrDefault()?.Check.Name;
    }
    LogRegion log = new() { ShowTime = false, PreferredHeight = 7 };
    log.Write($"Inventory: {domain} · {targets.Length} servers · {work.Count} tasks");
    if (phase >= 1) log.Write($"Connecting up to {parallel} servers at a time");
    foreach (Work w in finished.TakeLast(5)) log.Write($"{w.Node.Name} · {w.Check.Name}: {w.Actual}", w.Status == StatusKind.Fail ? "err" : w.Status == StatusKind.Warn ? "warn" : "info");
    if (phase >= 3) log.Write("Preparing " + format.ToUpperInvariant() + " report: " + report);
    if (phase >= 4) log.Write(notify ? "Report ready · recipient " + email : "Report ready · " + finished.Count(w => w.Status != StatusKind.Pass) + " findings to review");
    KeyValueList facts = new KeyValueList().Add("Targets", targets.Length + " servers").Add("Tasks", checks.Length + " per server")
        .Add("Parallel", parallel.ToString()).Add("Report", format.ToUpperInvariant()).Add("Dry run", dryRun ? "yes" : "no");
    Columns info = new(new Panel(lanes, "Servers"), new Panel(facts, "Run options")) { CollapseBelow = 90 };
    return new Rows(summary, phases, info, new Panel(Findings(finished), "Findings"), new Panel(log, "Log"));
}

// Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
void Execute(MenuContext context, List<Work> work) {
    RegionWidget body = new("body", Body(work, 0, 0));
    Dashboard dashboard = new("[bold]Server toolbox[/]", body) { Subtitle = domain, Footer = "Ctrl+C cancel · results keep the findings" };
    bool completed = false;
    MenuRunResult outcome = context.Run(dashboard, run => {
        void Show(int done, int phase) {
            body.Handle(run.Session).Set(Body(work, done, phase));
            run.Session.Delay(TimeSpan.FromMilliseconds(240));
        }
        Show(0, 0); Show(0, 1);
        int done = 0;
        foreach (var operation in work.GroupBy(w => w.Check.Id)) {
            foreach (Work[] batch in operation.Chunk(parallel)) { Show(done, 2); done += batch.Length; }
        }
        Show(done, 3); Show(done, 4);
        MenuRunResult result = Result(work); completed = true; return result;
    });
    if (!completed) return;
    history.Add(new(When: "Tue 20 Oct 09:30", Servers: work.Select(w => w.Node.Name).Distinct().Count(), Tasks: work.Select(w => w.Check.Id).Distinct().Count(),
        Passed: work.Count(w => w.Status == StatusKind.Pass), Warnings: work.Count(w => w.Status == StatusKind.Warn), Failed: work.Count(w => w.Status == StatusKind.Fail), Elapsed: outcome.Elapsed));
}
MenuRunResult Result(List<Work> work) {
    MenuRunResult result = new("Run finished", Verdict(work)) {
        Summary = $"{work.Count} tasks on {work.Select(w => w.Node.Name).Distinct().Count()} servers. Review the findings and choose the next action.", Details = Findings(work)
    };
    result.Figure("passed", work.Count(w => w.Status == StatusKind.Pass).ToString(), StatusKind.Pass);
    result.Figure("warnings", work.Count(w => w.Status == StatusKind.Warn).ToString(), work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass);
    result.Figure("failed", work.Count(w => w.Status == StatusKind.Fail).ToString(), work.Any(w => w.Status == StatusKind.Fail) ? StatusKind.Fail : StatusKind.Pass);
    result.Next("View report", ctx => ctx.ShowResult(new MenuRunResult("Report · " + format.ToUpperInvariant(), Verdict(work)) { Summary = report, Details = Findings(work) }));
    if (work.Any(w => w.Status == StatusKind.Fail)) result.Next("Rerun failed tasks", ctx => Execute(ctx, work.Where(w => w.Status == StatusKind.Fail).ToList()));
    result.Next("View history", ctx => ctx.ShowResult(new MenuRunResult("Run history") { Details = History() }));
    return result;
}
Form Options() {
    Form form = new("Options") { Review = false, SubmitText = "Save" };
    FormPage page = form.Page("Options", "Parallelism, reports and notifications.");
    NumberField throttle = page.Number("Parallel", "Servers at a time");
    throttle.Default = parallel; throttle.Minimum = 1; throttle.Maximum = 16; throttle.WholeNumber = true; throttle.ParameterName = "ThrottleLimit";
    PathField folder = page.Path("Report", "Report folder"); folder.Default = report; folder.PathKind = PathKind.Folder; folder.ParameterName = "ReportPath";
    ChoiceField output = page.Select("Format", "Report format", new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
    output.Default = format; output.ParameterName = "Format";
    ToggleField send = page.Toggle("Notify", "Email the report"); send.ParameterName = "Notify";
    TextField recipient = page.Text("Email", "Recipient"); recipient.Default = email; recipient.ParameterName = "To"; recipient.Required = true;
    recipient.VisibleWhen = v => v.Get<bool>("Notify"); recipient.Validator = (value, _) => value is string s && s.Contains('@') ? null : "Enter an email address";
    ToggleField dry = page.Toggle("DryRun", "Dry run"); dry.Default = false; dry.ParameterName = "WhatIf";
    return form;
}
Form options = Options();
void ApplyOptions(FormValues values) {
    parallel = (int)values.Get<decimal>("Parallel", 3m); report = values.Get<string>("Report", reportRoot); format = values.Get<string>("Format", "Html");
    notify = values.Get<bool>("Notify"); email = values.Get<string>("Email", "ops@example.test"); dryRun = values.Get<bool>("DryRun");
}
Menu menu = new("Server toolbox") { Layout = MenuLayout.Shell, Subtitle = "Tue 20 Oct 2026 · 09:30" };
MenuItem targetItem = new MenuItem("Targets").Hotkey('t').WithIcon(GlyphIcon.Targets).Describe("Inventory grouped by site; unavailable servers stay visible.")
    .LoadChoices(async (_, token) => {
        await Task.Delay(700, token);
        return Choice.From(inventory, n => n.Name, n => n.Site, description: n => n.Detail,
            badge: n => RichText.Plain(n.Role, new RichStyle(ThemeToken.Muted)), disabled: n => n.Offline);
    });
targetItem.SetSelection(targets.Cast<object?>());
targetItem.Minimum = 1;
targetItem.Preview = c => { Node n = (Node)c.Value!; return new KeyValueList().Add("Server", n.Name).Add("Site", n.Site).Add("Role", n.Role).Add("OS", n.Detail); };
MenuItem taskItem = new MenuItem("Tasks").Hotkey('k').WithIcon(GlyphIcon.Tasks).MultiSelect(catalog.Select(c => new Choice(c.Id, c.Name) {
    Group = c.Category, Description = c.Description, Badge = RichText.Plain((c.Changes ? "changes · " : "") + "~" + c.Minutes + " min", new RichStyle(c.Changes ? ThemeToken.Warn : ThemeToken.Muted)), Selected = checks.Contains(c)
}));
taskItem.Minimum = 1; taskItem.Preview = c => Preview(catalog.First(k => k.Id == (string)c.Value!));
menu.Add(targetItem); menu.Add(taskItem);
MenuItem optionItem = new MenuItem("Options").Hotkey('o').WithIcon(GlyphIcon.Settings).Form(options, (_, result) => ApplyOptions(result.Values)); menu.Add(optionItem);
menu.Preset("Morning · Warsaw", p => p.Selection(targetItem, inventory.Where(n => n.Site == "Warsaw").Cast<object?>())
    .Selection(taskItem, new object?[] { "PatchStatus", "Certificates", "DiskSpace" }).Form(options, new Dictionary<string, object?> { ["Parallel"] = 2m, ["DryRun"] = true })
    .WithPreview(new KeyValueList().Add("Scope", "Warsaw servers").Add("Tasks", "Security and disk space").Add("Dry run", "yes")));
menu.Preset("Health · all sites", p => p.Selection(targetItem, inventory.Where(n => n.Offline == "").Cast<object?>())
    .Selection(taskItem, new object?[] { "EventErrors", "Services", "DiskSpace", "Uptime" }));
menu.Item("Presets", i => i.Hotkey('p').WithIcon(GlyphIcon.Favorite).Presets()); menu.Separator();
menu.Item("Review & run", i => i.Hotkey('r').WithIcon(GlyphIcon.Run).Review(_ => {
    targets = inventory.Where(n => targetItem.GetSelection<Node>().Contains(n) && n.Offline == "").ToArray();
    checks = catalog.Where(c => taskItem.GetSelection<string>().Contains(c.Id)).ToArray(); ApplyOptions(options.GetValues());
    ReviewModel review = new("Review & run") { Description = "Everything the run uses; the command carries the same options." };
    review.Row("Targets", string.Join(", ", targets.Select(n => n.Name))); review.Row("Tasks", string.Join(", ", checks.Select(c => c.Name)));
    review.Row("Servers at a time", parallel.ToString()); review.Row("Report", report + " (" + format.ToUpperInvariant() + ")");
    review.Row("Email", notify ? email : "off"); review.Row("Dry run", dryRun ? "yes" : "no");
    if (targets.Length == 0) review.Problem("Pick at least one target", targetItem);
    if (checks.Length == 0) review.Problem("Pick at least one task", taskItem);
    if (notify && !email.Contains('@')) review.Problem("Enter the email recipient in Options", optionItem);
    Dictionary<string, object?> commandArguments = new() {
        ["ComputerName"] = targets.Select(n => n.Name).ToArray(),
        ["Task"] = checks.Select(c => c.Id).ToArray()
    };
    review.Command = CommandLine.Build("Invoke-ServerToolbox", commandArguments) + " " + options.BuildCommand();
    review.Run = ctx => Execute(ctx, Plan()); return review;
}));
menu.Item("History", i => i.Hotkey('h').WithIcon(GlyphIcon.List).Page(_ => History()).State(() => history.Count + " runs"));
menu.Separator(); menu.Exit("Quit");
menu.Run(new MenuOptions { Layout = MenuLayout.Shell, AppName = "Server toolbox gallery" });

record Node(string Name, string Domain, string Site, string Role, string Detail, string Offline) {
    public override string ToString() => Name;
}
record Check(string Id, string Name, string Category, string Description, string Expected, double Minutes, bool Changes);
record Work(Node Node, Check Check, StatusKind Status, string Actual);
record HistoryRow(string When, int Servers, int Tasks, int Passed, int Warnings, int Failed, TimeSpan Elapsed);
