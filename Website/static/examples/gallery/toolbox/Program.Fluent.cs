// Server toolbox: menus, selections, review, live work and actionable results.
// Synthetic data and timings; reports and notifications stay in memory. No remote effects.
using ConsoleMonster.Interactive;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: replace these inventories, operations and findings with your own sources.
Node[] inventory = new Node[]
{
    new("FS01", "lab.example.test", "Warsaw", "File", "Windows Server 2022", ""),
    new("FS02", "lab.example.test", "Warsaw", "File", "Windows Server 2022", ""),
    new("WEB01", "lab.example.test", "Warsaw", "Web", "Windows Server 2025", ""),
    new("SQL01", "lab.example.test", "Warsaw", "Database", "Windows Server 2019", ""),
    new("FS11", "lab.example.test", "Berlin", "File", "Windows Server 2022", ""),
    new("WEB11", "lab.example.test", "Berlin", "Web", "Windows Server 2025", ""),
    new("PRN11", "lab.example.test", "Berlin", "Print", "Windows Server 2016",
        "offline since 09:12"),
    new("BKP21", "lab.example.test", "Dublin", "Backup", "Windows Server 2022", ""),
    new("SQL21", "lab.example.test", "Dublin", "Database", "Windows Server 2022", ""),
    new("LAB01", "lab.example.test", "Lab", "Test", "Windows Server 2025", ""),
    new("LAB02", "lab.example.test", "Lab", "Test", "Windows Server 2025",
        "maintenance until Friday")
};
Check[] catalog = new Check[]
{
    new("DiskCleanup", "Disk cleanup", "Maintenance",
        "Removes temporary files and old update caches.", "cache under 1 GB", 3, true),
    new("LogRotation", "Log rotation", "Maintenance", "Compresses logs older than 7 days.",
        "retention 90 days", 2, true),
    new("RestartPending", "Pending restart check", "Maintenance",
        "Reports restarts waiting after updates.", "no restart pending", 1, false),
    new("PatchStatus", "Patch status", "Security", "Lists missing security updates.", "0 missing",
        2, false),
    new("Certificates", "Certificate expiry", "Security",
        "Finds certificates expiring within 30 days.", "30+ days", 1, false),
    new("LocalAdmins", "Local administrators audit", "Security",
        "Compares local Administrators with the approved list.", "approved list", 1, false),
    new("Firewall", "Firewall rules review", "Security",
        "Flags inbound allow rules open to any address.", "scoped rules", 2, false),
    new("EventErrors", "Event log errors", "Health", "Summarizes errors from the last 24 hours.",
        "under 20", 1, false),
    new("Services", "Service health", "Health", "Checks and restarts stopped automatic services.",
        "all running", 1, true),
    new("DiskSpace", "Disk space", "Health", "Warns below 15% and fails below 5% free.", "15% free",
        1, false),
    new("Uptime", "Uptime and reboots", "Health", "Reports unexpected reboots in the last 30 days.",
        "0 unexpected", 1, false)
};
Dictionary<string, (StatusKind Status, string Actual)> findings = new()
{
    ["FS02/PatchStatus"] = (StatusKind.Fail, "3 updates missing for 41 days"),
    ["WEB01/Certificates"] = (StatusKind.Warn, "expires in 24 days"),
    ["SQL01/DiskSpace"] = (StatusKind.Fail, "4% free on data volume"),
    ["FS11/Services"] = (StatusKind.Warn, "1 service restarted"),
    ["FS01/DiskCleanup"] = (StatusKind.Warn, "1.4 GB remains"),
};
List<HistoryRow> history = [
    new("Mon 19 Oct 21:04", 3, 4, 10, 1, 1, TimeSpan.FromSeconds(372)),
    new("Tue 20 Oct 07:30", 2, 3, 6, 0, 0, TimeSpan.FromSeconds(66)),
];
string reportRoot = "Reports/Toolbox";
Node[] targets = inventory.Where(n => n.Domain == "lab.example.test" && n.Offline == "")
    .Take(4)
    .ToArray();
Check[] checks = catalog.Where(c => new[] { "DiskCleanup", "PatchStatus", "Certificates",
    "DiskSpace" }.Contains(c.Id))
    .ToArray();
string domain = "lab.example.test", format = "Html", report = reportRoot, email = "ops@example.test";
int parallel = 3;
bool notify = false, dryRun = false;
List<Work> Plan() => checks.SelectMany(c => targets.Select(n =>
{
    (StatusKind Status, string Actual) outcome = findings.GetValueOrDefault(n.Name + "/" + c.Id,
        (StatusKind.Pass, c.Expected));
    return new Work(n, c, outcome.Status, outcome.Actual);
})).ToList();
StatusKind Verdict(IEnumerable<Work> work) => work.Any(w => w.Status == StatusKind.Fail)
    ? StatusKind.Fail
    : work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass;
Table TableOf(string[] headers)
{
    Table table = Ui.Table().Border(TableBorder.Minimal).RecordViewRows(0);
    foreach (string header in headers)
        table.AddColumn(header, HorizontalAlignment.Left);
    return table;
}

IWidget Findings(IEnumerable<Work> work)
{
    Table table = TableOf(new[] { "Server", "Task", "Expected", "Actual" });
    table.Columns[3].Flexible();
    foreach (Work w in work.Where(w => w.Status != StatusKind.Pass))
        table.AddRow(
            w.Node.Name,
            w.Check.Name,
            w.Check.Expected,
            TableCell.FromValue(RichText.Plain(
                w.Actual,
                new RichStyle(w.Status == StatusKind.Fail ? ThemeToken.Fail : ThemeToken.Warn))));
    return table;
}

IWidget History()
{
    Table table = TableOf(new[] { "When", "Servers", "Tasks", "Result", "Duration" });
    foreach (HistoryRow h in history.AsEnumerable().Reverse())
        table.AddRow(h.When, h.Servers.ToString(), h.Tasks.ToString(),
            $"{h.Passed} passed · {h.Warnings} warn · {h.Failed} failed",
            h.Elapsed.ToString(@"hh\:mm\:ss"));
    return table;
}

IWidget Preview(Check c) => Ui.Rows().Gap(1).Content(c2 =>
{
    c2.Markup(Markup.Escape(c.Description));
    c2.KeyValue(kv => kv.Add("Group", c.Category)
        .Add("Expected", c.Expected)
        .Add("Duration", "about " + c.Minutes + " min per server")
        .Add("Changes", c.Changes ? "[warn]yes[/]" : "[pass]no[/]")
        .Add("Parameter", c.Id));
});

// Layout: build the dashboard from the completed and active work.
IWidget Body(List<Work> work, int done, int phase)
{
    Work[] finished = work.Take(done).ToArray();
    Work[] active = phase == 2
        ? work.Skip(done).TakeWhile(w => w.Check.Id == work[done].Check.Id).Take(parallel).ToArray()
        : Array.Empty<Work>();
    SummaryBar summary = Ui.SummaryBar()
        .Elapsed(TimeSpan.FromSeconds(done * .24 / parallel))
        .SetProgress(done, work.Count)
        .SetCounter("passed", finished.Count(w => w.Status == StatusKind.Pass), StatusKind.Pass)
        .SetCounter("warnings", finished.Count(w => w.Status == StatusKind.Warn), StatusKind.Warn)
        .SetCounter("failed", finished.Count(w => w.Status == StatusKind.Fail), StatusKind.Fail);
    Phases phases = Ui.Phases();
    string[] names = { "Discover", "Connect", "Tasks", "Report" };
    for (int i = 0; i < names.Length; i++)
        phases.Set(names[i],
            i < phase
                ? (i == 2 ? Verdict(finished) : StatusKind.Pass)
                : i == phase ? StatusKind.Running : StatusKind.Pending);
    Lanes lanes = Ui.Lanes("lanes");
    foreach (Node node in targets)
    {
        Work[] planned = work.Where(w => w.Node == node).ToArray();
        Work[] ready = finished.Where(w => w.Node == node).ToArray();
        Lane lane = lanes.Add(node.Name, RichText.Plain(node.Name + "  " + node.Site),
            planned.Length)
            .Value(ready.Length)
            .Passed(ready.Count(w => w.Status == StatusKind.Pass))
            .Warned(ready.Count(w => w.Status == StatusKind.Warn))
            .Failed(ready.Count(w => w.Status == StatusKind.Fail))
            .Status(ready.Length == planned.Length
                ? Verdict(ready)
                : active.Any(w => w.Node == node) ? StatusKind.Running : StatusKind.Pending)
            .Detail(active.FirstOrDefault(w => w.Node == node)?.Check.Name
                ?? ready.LastOrDefault()?.Check.Name);
    }

    LogRegion log = Ui.LogRegion().ShowElapsed(false).PreferredHeight(7);
    log.Write($"Inventory: {domain} · {targets.Length} servers · {work.Count} tasks");
    if (phase >= 1)
        log.Write($"Connecting up to {parallel} servers at a time");
    foreach (Work w in finished.TakeLast(5))
        log.Write($"{w.Node.Name} · {w.Check.Name}: {w.Actual}",
            w.Status == StatusKind.Fail ? "err" : w.Status == StatusKind.Warn ? "warn" : "info");
    if (phase >= 3)
        log.Write("Preparing " + format.ToUpperInvariant() + " report: " + report);
    if (phase >= 4)
        log.Write(notify
            ? "Report ready · recipient " + email
            : "Report ready · " + finished.Count(w => w.Status != StatusKind.Pass)
                + " findings to review");
    KeyValueList facts = Ui.KeyValue()
        .Add("Targets", targets.Length + " servers")
        .Add("Tasks", checks.Length + " per server")
        .Add("Parallel", parallel.ToString())
        .Add("Report", format.ToUpperInvariant())
        .Add("Dry run", dryRun ? "yes" : "no");
    Columns info = Ui.Columns().CollapseBelow(90).Content(c3 =>
    {
        c3.Panel(p2 => p2.Title("Servers").Content(c2 =>
        {
            c2.Add(lanes);
        }));
        c3.Panel(p2 => p2.Title("Run options").Content(c2 =>
        {
            c2.Add(facts);
        }));
    });
    return Ui.Rows().Content(c3 =>
    {
        c3.Add(summary);
        c3.Add(phases);
        c3.Add(info);
        c3.Panel(p2 => p2.Title("Findings").Content(c2 =>
        {
            c2.Add(Findings(finished));
        }));
        c3.Panel(p2 => p2.Title("Log").Content(c2 =>
        {
            c2.Add(log);
        }));
    });
}

// Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
void Execute(MenuContext context, List<Work> work)
{
    RegionWidget body = Ui.Region("body").Content(c2 =>
    {
        c2.Add(Body(work, 0, 0));
    });
    Dashboard dashboard = Ui.Dashboard()
        .Content(c2 =>
        {
            c2.Add(body);
        })
        .Title("[bold]Server toolbox[/]")
        .Subtitle(domain)
        .Footer("Ctrl+C cancel · results keep the findings");
    bool completed = false;
    MenuRunResult outcome = context.Run(dashboard, run =>
    {
        void Show(int done, int phase)
        {
            body.Handle(run.Session).Set(Body(work, done, phase));
            run.Session.Delay(TimeSpan.FromMilliseconds(240));
        }

        Show(0, 0);
        Show(0, 1);
        int done = 0;
        foreach (var operation in work.GroupBy(w => w.Check.Id))
        {
            foreach (Work[] batch in operation.Chunk(parallel))
            {
                Show(done, 2);
                done += batch.Length;
            }
        }

        Show(done, 3);
        Show(done, 4);
        MenuRunResult result = Result(work);
        completed = true;
        return result;
    });
    if (!completed)
        return;
    history.Add(new(
        "Tue 20 Oct 09:30",
        work.Select(w => w.Node.Name).Distinct().Count(),
        work.Select(w => w.Check.Id).Distinct().Count(),
        work.Count(w => w.Status == StatusKind.Pass),
        work.Count(w => w.Status == StatusKind.Warn),
        work.Count(w => w.Status == StatusKind.Fail),
        outcome.Elapsed));
}

MenuRunResult Result(List<Work> work)
{
    MenuRunResult result = Ui.MenuRunResult()
        .Title("Run finished")
        .Status(Verdict(work))
        .Summary(
            $"{work.Count} tasks on {work.Select(w => w.Node.Name).Distinct().Count()} servers. Review the findings and choose the next action.")
        .Details(Findings(work));
    result.Figure(
        "passed",
        work.Count(w => w.Status == StatusKind.Pass).ToString(),
        StatusKind.Pass);
    result.Figure(
        "warnings",
        work.Count(w => w.Status == StatusKind.Warn).ToString(),
        work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass);
    result.Figure(
        "failed",
        work.Count(w => w.Status == StatusKind.Fail).ToString(),
        work.Any(w => w.Status == StatusKind.Fail) ? StatusKind.Fail : StatusKind.Pass);
    result.Next(
        "View report",
        ctx => ctx.ShowResult(Ui.MenuRunResult()
            .Title("Report · " + format.ToUpperInvariant())
            .Status(Verdict(work))
            .Summary(report)
            .Details(Findings(work))));
    if (work.Any(w => w.Status == StatusKind.Fail))
        result.Next(
            "Rerun failed tasks",
            ctx => Execute(ctx, work.Where(w => w.Status == StatusKind.Fail).ToList()));
    result.Next(
        "View history",
        ctx => ctx.ShowResult(Ui.MenuRunResult().Title("Run history").Details(History())));
    return result;
}

Form Options()
{
    Form form = Ui.Form().Title("Options").Review(false).SubmitText("Save");
    FormPage page = form.Page("Options", "Parallelism, reports and notifications.");
    NumberField throttle = page.Number("Parallel", "Servers at a time")
        .Default(parallel)
        .Minimum(1)
        .Maximum(16)
        .Integer()
        .ParameterName("ThrottleLimit");
    PathField folder = page.Path("Report", "Report folder")
        .Default(report)
        .PathKind(PathKind.Folder)
        .ParameterName("ReportPath");
    ChoiceField output = page.Select(
        "Format",
        "Report format",
        new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") })
        .Default(format)
        .ParameterName("Format");
    ToggleField send = page.Toggle("Notify", "Email the report").ParameterName("Notify");
    TextField recipient = page.Text("Email", "Recipient")
        .Default(email)
        .ParameterName("To")
        .Required()
        .Configure(configured => configured.VisibleWhen = v => v.Get<bool>("Notify"))
        .Configure(configured => configured.Validator = (value,
            _) => value is string s && s.Contains('@') ? null : "Enter an email address");
    ToggleField dry = page.Toggle("DryRun", "Dry run").Default(false).ParameterName("WhatIf");
    return form;
}

Form options = Options();
void ApplyOptions(FormValues values)
{
    parallel = (int)values.Get<decimal>("Parallel", 3m);
    report = values.Get<string>("Report", reportRoot);
    format = values.Get<string>("Format", "Html");
    notify = values.Get<bool>("Notify");
    email = values.Get<string>("Email", "ops@example.test");
    dryRun = values.Get<bool>("DryRun");
}

Menu menu = Ui.Menu()
    .Title("Server toolbox")
    .Layout(MenuLayout.Shell)
    .Subtitle("Tue 20 Oct 2026 · 09:30");
MenuItem targetItem = Ui.MenuItem()
    .Label("Targets")
    .Hotkey('t')
    .IconName(GlyphIcon.Targets)
    .Description("Inventory grouped by site; unavailable servers stay visible.")
    .Configure(configured => configured.LoadChoices = async (_, token) =>
    {
        await Task.Delay(700, token);
        return Choice.From(
            inventory,
            n => n.Name,
            n => n.Site,
            n => n.Detail,
            n => RichText.Plain(n.Role, new RichStyle(ThemeToken.Muted)),
            n => n.Offline);
    });
targetItem.SetSelection(targets.Cast<object?>());
targetItem.Minimum(1).Configure(configured => configured.Preview = c =>
{
    Node n = (Node)c.Value!;
    return Ui.KeyValue()
        .Add("Server", n.Name)
        .Add("Site", n.Site)
        .Add("Role", n.Role)
        .Add("OS", n.Detail);
});
MenuItem taskItem = Ui.MenuItem()
    .Label("Tasks")
    .Hotkey('k')
    .IconName(GlyphIcon.Tasks)
    .MultiSelect(catalog.Select(c => new Choice(c.Id, c.Name)
        .Group(c.Category)
        .Description(c.Description)
        .Badge(RichText.Plain(
            (c.Changes ? "changes · " : "") + "~" + c.Minutes + " min",
            new RichStyle(c.Changes ? ThemeToken.Warn : ThemeToken.Muted)))
        .Selected(checks.Contains(c))))
    .Minimum(1)
    .Configure(
        configured => configured.Preview = c => Preview(
            catalog.First(k => k.Id == (string)c.Value!)));
menu.Add(targetItem).Add(taskItem);
MenuItem optionItem = Ui.MenuItem()
    .Label("Options")
    .Hotkey('o')
    .IconName(GlyphIcon.Settings)
    .Form(options, (_, result) => ApplyOptions(result.Values));
menu.Add(optionItem)
    .Preset(
        "Morning · Warsaw",
        p => p.Selection(targetItem, inventory.Where(n => n.Site == "Warsaw").Cast<object?>())
            .Selection(taskItem, new object? [] { "PatchStatus", "Certificates", "DiskSpace" })
            .Form(options, new Dictionary<string, object?> { ["Parallel"] = 2m, ["DryRun"] = true })
            .Preview(Ui.KeyValue()
                .Add("Scope", "Warsaw servers")
                .Add("Tasks", "Security and disk space")
                .Add("Dry run", "yes")))
    .Preset(
        "Health · all sites",
        p => p.Selection(targetItem, inventory.Where(n => n.Offline == "").Cast<object?>())
            .Selection(
                taskItem,
                new object? [] { "EventErrors", "Services", "DiskSpace", "Uptime" }))
    .Item("Presets", i => i.Hotkey('p').IconName(GlyphIcon.Favorite).Presets())
    .Separator()
    .Item("Review & run", i =>
    {
        i.Hotkey('r').IconName(GlyphIcon.Run).Configure(configured2 => configured2.Review = _ =>
        {
            targets = inventory.Where(n => targetItem.GetSelection<Node>()
                .Contains(n) && n.Offline == "")
                .ToArray();
            checks = catalog.Where(c => taskItem.GetSelection<string>().Contains(c.Id)).ToArray();
            ApplyOptions(options.GetValues());
            ReviewModel review = Ui.Review()
                .Title("Review & run")
                .Description("Everything the run uses; the command carries the same options.")
                .Row("Targets", string.Join(", ", targets.Select(n => n.Name)))
                .Row("Tasks", string.Join(", ", checks.Select(c => c.Name)))
                .Row("Servers at a time", parallel.ToString())
                .Row("Report", report + " (" + format.ToUpperInvariant() + ")")
                .Row("Email", notify ? email : "off")
                .Row("Dry run", dryRun ? "yes" : "no");
            if (targets.Length == 0)
                review.Problem("Pick at least one target", targetItem);
            if (checks.Length == 0)
                review.Problem("Pick at least one task", taskItem);
            if (notify && !email.Contains('@'))
                review.Problem("Enter the email recipient in Options", optionItem);
            Dictionary<string, object?> commandArguments = new()
            {
                ["ComputerName"] = targets.Select(n => n.Name).ToArray(),
                ["Task"] = checks.Select(c => c.Id).ToArray()
            };
            review.Command(CommandLine.Build("Invoke-ServerToolbox",
                commandArguments) + " " + options.BuildCommand())
                .Configure(configured => configured.Run = ctx => Execute(ctx, Plan()));
            return review;
        });
    })
    .Item("History", i =>
    {
        i.Hotkey('h')
            .IconName(GlyphIcon.List)
            .Configure(configured => configured.Page = _ => History())
            .Configure(configured => configured.Detail = () => history.Count + " runs");
    })
    .Separator()
    .Exit("Quit");
menu.Run(options2 =>
{
    options2.Layout(MenuLayout.Shell).AppName("Server toolbox gallery");
});

record Node(string Name, string Domain, string Site, string Role, string Detail, string Offline)
{
    public override string ToString() => Name;
}

record Check(string Id, string Name, string Category, string Description, string Expected,
    double Minutes, bool Changes);

record Work(Node Node, Check Check, StatusKind Status, string Actual);

record HistoryRow(string When, int Servers, int Tasks, int Passed, int Warnings, int Failed,
    TimeSpan Elapsed);
