// Fleet health check: menus, selections, review, live work and actionable results.
// Synthetic data and timings; reports and notifications stay in memory. No remote effects.
using ConsoleMonster.Interactive;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: replace these inventories, operations and findings with your own sources.
Node[] inventory = new Node[]
{
    new(Name: "DC01", Domain: "corp.example.test", Site: "Warsaw", Role: "Domain controller",
        Detail: "10.20.1.11", Offline: "", Online: true),
    new(Name: "DC02", Domain: "corp.example.test", Site: "Warsaw", Role: "Domain controller",
        Detail: "10.20.1.12", Offline: "", Online: true),
    new(Name: "FS01", Domain: "corp.example.test", Site: "Dublin", Role: "File",
        Detail: "10.30.1.21", Offline: "", Online: true),
    new(Name: "SQL01", Domain: "corp.example.test", Site: "Dublin", Role: "Database",
        Detail: "10.30.1.31", Offline: "", Online: true),
    new(Name: "WEB01", Domain: "corp.example.test", Site: "Berlin", Role: "Web",
        Detail: "10.40.1.41", Offline: "", Online: true),
    new(Name: "FS22", Domain: "corp.example.test", Site: "Berlin", Role: "File",
        Detail: "10.40.1.22", Offline: "offline since 07:12", Online: false),
    new(Name: "LAB01", Domain: "lab.example.test", Site: "Lab", Role: "Web",
        Detail: "192.168.50.40", Offline: "", Online: true),
    new(Name: "LAB02", Domain: "lab.example.test", Site: "Lab", Role: "Database",
        Detail: "192.168.50.30", Offline: "", Online: true)
};
Check[] catalog = new Check[]
{
    new(Id: "SecureChannel", Name: "Secure channel", Category: "Identity",
        Description: "Tests the computer account trust with the domain.", Expected: "trusted",
        Seconds: 1.2, Changes: false),
    new(Id: "TimeOffset", Name: "Time offset", Category: "Identity",
        Description: "Compares the clock with the domain time source.", Expected: "under 1 s",
        Seconds: 0.8, Changes: false),
    new(Id: "ForwardRecord", Name: "Forward record", Category: "DNS",
        Description: "Compares the A record with the server address.", Expected: "matches IP",
        Seconds: 0.6, Changes: false),
    new(Id: "CertExpiry", Name: "Cert expiry", Category: "Certificates",
        Description: "Finds machine certificates expiring within 30 days.", Expected: "30+ days",
        Seconds: 1.8, Changes: false),
    new(Id: "CertChain", Name: "Cert chain", Category: "Certificates",
        Description: "Builds each certificate chain to a trusted root.", Expected: "trusted root",
        Seconds: 3.5, Changes: false),
    new(Id: "MissingUpdates", Name: "Missing updates", Category: "Updates",
        Description: "Lists missing security updates older than 14 days.", Expected: "0",
        Seconds: 6.5, Changes: false),
    new(Id: "FreeSpace", Name: "Free space", Category: "Storage",
        Description: "Measures free space on every fixed volume.", Expected: "15% free",
        Seconds: 1.3, Changes: false),
    new(Id: "AutoServices", Name: "Auto services", Category: "Services",
        Description: "Checks that automatic services are running.", Expected: "all running",
        Seconds: 2.2, Changes: false)
};
Dictionary<string, (StatusKind Status, string Actual)> findings = new()
{
    ["DC02/TimeOffset"] = (StatusKind.Fail, "4.7 s"),
    ["FS01/MissingUpdates"] = (StatusKind.Fail, "3 updates"),
    ["SQL01/FreeSpace"] = (StatusKind.Fail, "6% on data volume"),
    ["WEB01/CertExpiry"] = (StatusKind.Fail, "9 days"),
    ["WEB01/CertChain"] = (StatusKind.Warn, "intermediate ends in 40 days"),
    ["SQL01/AutoServices"] = (StatusKind.Warn, "1 restarted"),
};
List<HistoryRow> history = [
    new(When: "Mon 19 Oct 21:04", Servers: 3, Checks: 4, Passed: 10, Warnings: 1, Failed: 1,
        Elapsed: TimeSpan.FromSeconds(372)),
    new(When: "Tue 20 Oct 07:30", Servers: 2, Checks: 3, Passed: 6, Warnings: 0, Failed: 0,
        Elapsed: TimeSpan.FromSeconds(66)),
];
string reportRoot = "Reports/Fleet";
Node[] targets = inventory.Where(n => n.Domain == "corp.example.test" && n.Offline == "")
    .Take(5)
    .ToArray();
Check[] checks = catalog.Where(c => new[] { "SecureChannel", "TimeOffset", "ForwardRecord",
    "CertExpiry", "CertChain", "MissingUpdates", "FreeSpace", "AutoServices" }.Contains(c.Id))
    .ToArray();
string domain = "corp.example.test", format = "Html", report = reportRoot, email = "ops@example.test";
int parallel = 3;
bool notify = false;
List<Work> Plan() => checks.SelectMany((c, ci) => targets.Select((n, ni) =>
{
    (StatusKind Status, string Actual) outcome = findings.GetValueOrDefault(n.Name + "/" + c.Id,
        (StatusKind.Pass, c.Expected));
    return new Work(n, c, ni * checks.Length + ci, outcome.Status, outcome.Actual,
        Math.Round(c.Seconds * (1 + ni * .17), 1));
})).ToList();
StatusKind Verdict(IEnumerable<Work> work) => work.Any(w => w.Status == StatusKind.Fail)
    ? StatusKind.Fail
    : work.Any(w => w.Status == StatusKind.Warn) ? StatusKind.Warn : StatusKind.Pass;
string Seconds(double seconds) => seconds.ToString("0.0",
    System.Globalization.CultureInfo.InvariantCulture) + "s";
Table TableOf(string[] headers)
{
    Table table = new Table()
    {
        Border = TableBorder.Minimal,
        RecordViewRows = 0
    };
    foreach (string header in headers)
        table.AddColumn(header, HorizontalAlignment.Left);
    return table;
}

IWidget Findings(IEnumerable<Work> work)
{
    Table table = TableOf(new[] { "Server", "Check", "Expected", "Actual" });
    table.Columns[3].Flexible = true;
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
    Table table = TableOf(new[] { "When", "Servers", "Checks", "Result", "Duration" });
    foreach (HistoryRow h in history.AsEnumerable().Reverse())
        table.AddRow(h.When, h.Servers.ToString(), h.Checks.ToString(),
            $"{h.Passed} passed · {h.Warnings} warn · {h.Failed} failed",
            h.Elapsed.ToString(@"hh\:mm\:ss"));
    return table;
}

IWidget Preview(Check c) => new Rows(
    new TextBlock(Markup.Escape(c.Description)),
    new KeyValueList()
        .Add("Group", c.Category)
        .Add("Expected", c.Expected)
        .Add("Duration", "about " + Seconds(c.Seconds))
        .Add("Changes", c.Changes ? "[warn]yes[/]" : "[pass]no[/]")
        .Add("Parameter", c.Id))
{
    Gap = 1
};

// Layout: build the dashboard from the completed and active work.
IWidget Body(List<Work> work, int done, int phase)
{
    Work[] finished = work.Take(done).ToArray();
    Work[] active = phase == 2
        ? work.Skip(done).TakeWhile(w => w.Check.Id == work[done].Check.Id).Take(parallel).ToArray()
        : Array.Empty<Work>();
    SummaryBar summary = new SummaryBar
    {
        Elapsed = TimeSpan.FromSeconds(done * .24 / parallel)
    }
        .SetProgress(done, work.Count)
        .SetCounter("passed", finished.Count(w => w.Status == StatusKind.Pass), StatusKind.Pass)
        .SetCounter("warnings", finished.Count(w => w.Status == StatusKind.Warn), StatusKind.Warn)
        .SetCounter("failed", finished.Count(w => w.Status == StatusKind.Fail), StatusKind.Fail);
    Phases phases = new Phases();
    string[] names = { "Discover", "Connect", "Check", "Report" };
    for (int i = 0; i < names.Length; i++)
        phases.Set(names[i],
            i < phase
                ? (i == 2 ? Verdict(finished) : StatusKind.Pass)
                : i == phase ? StatusKind.Running : StatusKind.Pending);
    Lanes lanes = new Lanes("lanes");
    foreach (string category in work.Select(w => w.Check.Category).Distinct())
    {
        Work[] planned = work.Where(w => w.Check.Category == category).ToArray();
        Work[] ready = finished.Where(w => w.Check.Category == category).ToArray();
        Lane lane = lanes.Add(category, category, planned.Length);
        lane.Value = ready.Length;
        lane.Passed = ready.Count(w => w.Status == StatusKind.Pass);
        lane.Warned = ready.Count(w => w.Status == StatusKind.Warn);
        lane.Failed = ready.Count(w => w.Status == StatusKind.Fail);
        lane.Status = ready.Length == planned.Length
            ? Verdict(ready)
            : active.Any(w => w.Check.Category == category)
                ? StatusKind.Running
                : StatusKind.Pending;
    }

    LogRegion log = new LogRegion()
    {
        ShowElapsed = false,
        PreferredHeight = 7
    };
    log.Write($"Inventory: {domain} · {targets.Length} servers · {work.Count} checks");
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
    CellMap map = new CellMap()
    {
        Columns = checks.Length,
        LegendLines = 2
    };
    foreach (Node node in targets)
        map.RowLabels.Add(node.Name);
    map.Resize(targets.Length * checks.Length);
    for (int i = 0; i < map.Count; i++)
        if (!work.Any(w => w.Cell == i))
            map.Set(i, StatusKind.Skip);
    foreach (Work w in finished)
        map.Set(w.Cell, w.Status);
    foreach (Work w in active)
        map.Set(w.Cell, StatusKind.Running);
    StatusList running = new StatusList("running");
    foreach (Work w in active)
        running.Add(Markup.Escape(w.Node.Name + " · " + w.Check.Name), StatusKind.Running);
    if (active.Length == 0)
        running.Add(phase >= 3 ? "Checks complete" : "Waiting for connections",
            phase >= 3 ? Verdict(finished) : StatusKind.Pending);
    KeyValueList facts = new KeyValueList()
        .Add("Domain", Markup.Escape(domain))
        .Add(
            "Servers",
            targets.Length + " · " + targets.Select(n => n.Site).Distinct().Count() + " sites")
        .Add("Checks", checks.Length + " · " + work.Count + " in all")
        .Add("Report", format.ToUpperInvariant());
    Table slow = TableOf(new[] { "Check", "Server", "Took" });
    foreach (Work w in finished.OrderByDescending(w => w.Seconds).Take(5))
        slow.AddRow(w.Check.Name, w.Node.Name, Seconds(w.Seconds));
    Histogram durations = new Histogram(new double[] { 0, 2, 4, 8, 16 }, "durations")
    {
        Height = 5
    };
    foreach (Work w in finished)
        durations.Add(w.Seconds);
    Columns top = new Columns()
        .Add(new Panel() { Title = "Check map", Content = map })
        .Add(
            new Panel() { Title = "Categories", Content = new Rows(lanes, new Rule(), facts) },
            LayoutSize.Fraction(1.15))
        .Add(new Panel() { Title = "Running", Content = running }, LayoutSize.Fraction(.95));
    top.CollapseBelow = 100;
    Columns bottom = new Columns()
        .Add(
            new Panel() { Title = "Findings", Content = Findings(finished) },
            LayoutSize.Fraction(1.5))
        .Add(new Panel() { Title = "Slowest checks", Content = slow })
        .Add(new Panel() { Title = "Durations", Content = durations }, LayoutSize.Fixed(20));
    bottom.CollapseBelow = 100;
    return new Rows(summary, phases, top, bottom, new Panel() { Title = "Log", Content = log });
}

// Behaviour: native form/menu navigation, then a bounded run with cancellation and history.
void Execute(MenuContext context, List<Work> work)
{
    RegionWidget body = new RegionWidget(widgetId: "body", content: Body(work, 0, 0));
    Dashboard dashboard = new Dashboard()
    {
        Content = body,
        Title = "[bold]Fleet health check[/]",
        Subtitle = domain,
        Footer = "Ctrl+C cancel · results keep the findings"
    };
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
        When: "Tue 20 Oct 09:30",
        Servers: work.Select(w => w.Node.Name).Distinct().Count(),
        Checks: work.Select(w => w.Check.Id).Distinct().Count(),
        Passed: work.Count(w => w.Status == StatusKind.Pass),
        Warnings: work.Count(w => w.Status == StatusKind.Warn),
        Failed: work.Count(w => w.Status == StatusKind.Fail),
        Elapsed: outcome.Elapsed));
}

MenuRunResult Result(List<Work> work)
{
    MenuRunResult result = new MenuRunResult("Fleet check finished", Verdict(work))
    {
        Summary = $"{work.Count} checks on {work.Select(w => w.Node.Name).Distinct().Count()} servers. Review the findings and choose the next action.",
        Details = Findings(work)
    };
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
        ctx => ctx.ShowResult(new MenuRunResult("Report · " + format.ToUpperInvariant(),
            Verdict(work)) { Summary = report, Details = Findings(work) }));
    if (work.Any(w => w.Status == StatusKind.Fail))
        result.Next(
            "Rerun failed checks",
            ctx => Execute(ctx, work.Where(w => w.Status == StatusKind.Fail).ToList()));
    result.Next(
        "View history",
        ctx => ctx.ShowResult(new MenuRunResult("Run history") { Details = History() }));
    return result;
}

Form Options()
{
    Form form = new Form("Options")
    {
        Review = false,
        SubmitText = "Save"
    };
    FormPage page = form.Page("Options", "Parallelism, reports and notifications.");
    NumberField throttle = page.Number("Parallel", "Servers at a time");
    throttle.Default = parallel;
    throttle.Minimum = 1;
    throttle.Maximum = 16;
    throttle.Integer = true;
    throttle.ParameterName = "ThrottleLimit";
    PathField folder = page.Path("Report", "Report folder");
    folder.Default = report;
    folder.PathKind = PathKind.Folder;
    folder.ParameterName = "ReportPath";
    ChoiceField output = page.Select(
        "Format",
        "Report format",
        new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
    output.Default = format;
    output.ParameterName = "Format";
    ToggleField send = page.Toggle("Notify", "Email the report");
    send.ParameterName = "Notify";
    TextField recipient = page.Text("Email", "Recipient");
    recipient.Default = email;
    recipient.ParameterName = "To";
    recipient.Required = true;
    recipient.VisibleWhen = v => v.Get<bool>("Notify");
    recipient.Validator = (value, _) => value is string s && s.Contains('@')
        ? null
        : "Enter an email address";
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
}

Menu menu = new Menu("Fleet health check")
{
    Layout = MenuLayout.Shell,
    Subtitle = "Tue 20 Oct 2026 · 09:30"
};
Form checkForm = new Form("New check")
{
    Command = "Invoke-FleetCheck",
    SubmitText = "Run checks",
    Description = "Choose the scope, checks and run options."
};
FormPage scope = checkForm.Page("Scope", "Which servers to check.");
ChoiceField domains = scope.Select(
    "Domain",
    "Domain",
    inventory.Select(n => n.Domain).Distinct().Select(d => new Choice(d)));
domains.Default = domain;
domains.ParameterName = "Domain";
ChoiceField servers = scope.MultiSelect("Servers", "Servers");
servers.Minimum = 1;
servers.ParameterName = "ComputerName";
servers.DependsOn.Add("Domain");
servers.LoadChoices = async (values, token) =>
{
    await Task.Delay(350, token);
    return inventory.Where(n => n.Domain == values.Get<string>("Domain"))
        .Select(n => new Choice(n.Name)
        {
            Label = n.Name,
            Group = n.Site,
            Badge = RichText.Plain(n.Role, new RichStyle(ThemeToken.Muted)),
            Description = n.Detail,
            DisabledReason = n.Offline == "" ? null : n.Offline,
            Selected = n.Offline == ""
        });
};
servers.Default = targets.Select(n => n.Name).ToArray();
FormPage choose = checkForm.Page("Checks", "What a healthy server reports appears in the preview.");
ChoiceField operations = choose.MultiSelect(
    "Checks",
    "Checks",
    catalog.Select(c => new Choice(c.Id, c.Name)
    {
        Group = c.Category,
        Description = c.Description,
        Badge = RichText.Plain("~" + Seconds(c.Seconds), new RichStyle(ThemeToken.Muted)),
        Selected = checks.Contains(c)
    }));
operations.Minimum = 1;
operations.ParameterName = "Check";
operations.Preview = c => Preview(catalog.First(k => k.Id == (string)c.Value!));
FormPage settings = checkForm.Page("Options", "How the run behaves.");
foreach (Field field in options.Fields)
    settings.Add(field);
menu.Preset(
    "Morning · identity & DNS",
    p => p.Form(
        checkForm,
        new Dictionary<string, object?>
        {
            ["Servers"] = inventory.Where(n => n.Role == "Domain controller")
                .Select(n => n.Name)
                .ToArray(),
            ["Checks"] = new[] { "SecureChannel", "TimeOffset", "ForwardRecord" },
            ["Parallel"] = 2m
        })
        .Preview(new KeyValueList()
            .Add("Scope", "Domain controllers")
            .Add("Checks", "Identity and DNS")));
menu.Preset(
    "Nightly · all sites",
    p => p.Form(
        checkForm,
        new Dictionary<string, object?>
        {
            ["Servers"] = inventory.Where(n => n.Domain == domain && n.Offline == "")
                .Select(n => n.Name)
                .ToArray(),
            ["Checks"] = catalog.Select(c => c.Id).ToArray(),
            ["Parallel"] = 3m,
            ["Notify"] = true
        })
        .Preview(new KeyValueList()
            .Add("Scope", "Every online server")
            .Add("Checks", "All categories")));
menu.Preset(
    "Patch Tuesday · updates",
    p => p.Form(
        checkForm,
        new Dictionary<string, object?>
        {
            ["Checks"] = new[] { "MissingUpdates", "AutoServices" },
            ["Format"] = "Csv"
        }));
menu.Preset(
    "Certificates · web tier",
    p => p.Form(
        checkForm,
        new Dictionary<string, object?>
        {
            ["Servers"] = new[] { "WEB01" },
            ["Checks"] = new[] { "CertExpiry", "CertChain", "ForwardRecord" }
        }));
menu.Item("Presets", i => i.Hotkey('p').IconName(GlyphIcon.Favorite).Presets());
menu.Item(
    "New check",
    i => i.Hotkey('n')
        .IconName(GlyphIcon.Run)
        .Description("Choose a domain, servers and checks; review the command and run.")
        .Form(checkForm, (context, result) =>
        {
            domain = result.Values.Get<string>("Domain", domain);
            targets = inventory.Where(n => n.Domain == domain && n.Offline == ""
                && result.Values.GetList<string>("Servers")
                    .Contains(n.Name))
                .ToArray();
            checks = catalog.Where(c => result.Values.GetList<string>("Checks").Contains(c.Id))
                .ToArray();
            ApplyOptions(result.Values);
            Execute(context, Plan());
        }));
menu.Item("History", i =>
{
    i.Hotkey = 'h';
    i.IconName = GlyphIcon.List;
    i.Page = _ => History();
    i.Detail = () => history.Count + " runs";
});
menu.Separator();
menu.Exit("Quit");
menu.Run(new MenuOptions { Layout = MenuLayout.Shell, AppName = "Fleet health check gallery" });

record Node(string Name, string Domain, string Site, string Role, string Detail, string Offline,
    bool Online);

record Check(string Id, string Name, string Category, string Description, string Expected,
    double Seconds, bool Changes);

record Work(Node Node, Check Check, int Cell, StatusKind Status, string Actual, double Seconds);

record HistoryRow(string When, int Servers, int Checks, int Passed, int Warnings, int Failed,
    TimeSpan Elapsed);
