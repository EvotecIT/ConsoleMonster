using System.Diagnostics;
using System.Globalization;
using ConsoleMonster.Building;
using ConsoleMonster.Glyphs;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// A "Server toolbox" in the Shell layout: targets loaded from an inventory, grouped tasks with a preview, an options
// form, presets, a review with the equivalent command and a run into a live dashboard. All data is invented.
Server[] inventory = {
    new("FS01", "Warsaw", "File", "Windows Server 2022"),
    // <more>
    new("FS02", "Warsaw", "File", "Windows Server 2022"),
    new("WEB01", "Warsaw", "Web", "Windows Server 2025"),
    new("SQL01", "Warsaw", "Database", "Windows Server 2019"),
    new("FS11", "Berlin", "File", "Windows Server 2022"),
    new("WEB11", "Berlin", "Web", "Windows Server 2025"),
    new("PRN11", "Berlin", "Print", "Windows Server 2016", "offline since 09:12"),
    new("BKP21", "Dublin", "Backup", "Windows Server 2022"),
    new("SQL21", "Dublin", "Database", "Windows Server 2022"),
    new("LAB01", "Lab", "Test", "Windows Server 2025"),
    new("LAB02", "Lab", "Test", "Windows Server 2025", "in maintenance until Friday")
    // </more>
};
MaintenanceTask[] allTasks = {
    new("DiskCleanup", "Disk cleanup", "Maintenance", "Removes temporary files, old update caches and crash dumps older than 30 days.", 3, true),
    // <more>
    new("LogRotation", "Log rotation", "Maintenance", "Compresses application logs older than 7 days and deletes archives older than 90 days.", 2, true),
    new("RestartPending", "Pending restart check", "Maintenance", "Reports servers waiting for a restart after updates or role changes.", 1, false),
    new("PatchStatus", "Patch status", "Security", "Lists missing security updates and the date of the last successful install.", 2, false),
    new("Certificates", "Certificate expiry", "Security", "Finds machine certificates that expire within 30 days and the services that use them.", 1, false),
    new("LocalAdmins", "Local administrators audit", "Security", "Compares local Administrators membership with the approved list.", 1, false),
    new("Firewall", "Firewall rules review", "Security", "Flags inbound allow rules open to any address.", 2, false),
    new("EventErrors", "Event log errors", "Health", "Summarizes System and Application errors from the last 24 hours.", 1, false),
    new("Services", "Service health", "Health", "Checks that automatic services are running and restarts the ones that stopped.", 1, true),
    new("DiskSpace", "Disk space", "Health", "Warns below 15% free and fails below 5% free on every fixed volume.", 1, false),
    new("Uptime", "Uptime and reboots", "Health", "Reports uptime and unexpected reboots in the last 30 days.", 1, false)
    // </more>
};
List<PastRun> history = new() {
    new(new DateTime(2026, 10, 15, 21, 4, 0), 8, 5, 37, 2, 1, TimeSpan.FromMinutes(6.2)),
    new(new DateTime(2026, 10, 16, 7, 30, 0), 3, 2, 6, 0, 0, TimeSpan.FromMinutes(1.1)),
    new(new DateTime(2026, 10, 16, 18, 45, 0), 9, 11, 91, 6, 2, TimeSpan.FromMinutes(14.8))
};

Menu menu = new Menu("Server toolbox") { Subtitle = "lab · 11 servers", Layout = MenuLayout.Shell };
MenuItem targets = new MenuItem("Targets")
    .Hotkey('t')
    .WithIcon(GlyphIcon.Targets)
    .Describe("Servers the tasks run on. The list is loaded from the inventory; offline servers cannot be picked.")
    .LoadChoices((context, token) => LoadServersAsync(token));
targets.Minimum = 1;
MenuItem tasks = new MenuItem("Tasks")
    .Hotkey('k')
    .WithIcon(GlyphIcon.Tasks)
    .Describe("What to run on every target. Tasks marked 'changes' modify the servers; the rest only read.")
    .MultiSelect(allTasks, t => t.Name, t => t.Category, TaskPreview);
foreach (Choice c in tasks.Choices) {
    MaintenanceTask task = (MaintenanceTask)c.Value!;
    RichTextBuilder badge = new RichTextBuilder();
    if (task.Changes) {
        badge.Append("changes ", ThemeToken.Warn);
    }
    c.Badge = badge.Append("~" + task.Minutes + " min", ThemeToken.Muted).Build();
    c.Description = task.Description;
}
tasks.SetSelection(new object?[] { allTasks[0], allTasks[3], allTasks[4] });
Form options = OptionsForm();
MenuItem optionsItem = new MenuItem("Options")
    .Hotkey('o')
    .WithIcon(GlyphIcon.Settings)
    .Describe("How the run behaves: parallelism, the report and notifications.")
    .Form(options, (context, result) => context.Toast("Options saved"));
menu.Add(targets);
menu.Add(tasks);
menu.Add(optionsItem);
menu.Item("Presets", i => i.Hotkey('p').WithIcon(GlyphIcon.Favorite).Describe("Saved combinations of targets, tasks and options. Save one from Review & run.").Presets());
menu.Separator();
menu.Item("Review & run", i => i.Hotkey('r').WithIcon(GlyphIcon.Run).Review(Review));
menu.Item("History", i => i.Hotkey('h').Page(context => HistoryPage()).State(() => history.Count + " runs"));
menu.Separator();
menu.Exit("Quit");
menu.CommandProviders.Add(new CommandProvider("Task", () => allTasks.Select(task => new PaletteCommand("Toggle " + task.Name, context => {
    List<object?> selection = tasks.Selection.ToList();
    bool on = selection.Remove(task);
    if (!on) {
        selection.Add(task);
    }
    tasks.SetSelection(allTasks.Where(t => selection.Contains(t)).Cast<object?>());
    context.Toast((on ? "Removed " : "Added ") + task.Name, on ? StatusKind.Info : StatusKind.Pass);
}) { Description = task.Description })));

menu.Run(new MenuOptions { StorageRoot = Path.Combine(Path.GetTempPath(), "server-toolbox") });

// <more>
async Task<IEnumerable<Choice>> LoadServersAsync(CancellationToken token) {
    await Task.Delay(1100, token);
    return inventory.Select(server => new Choice(server, server.Name) {
        Group = server.Site,
        Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted)),
        Description = server.Role + " server · " + server.OperatingSystem,
        DisabledReason = server.Offline
    }).ToList();
}

IWidget TaskPreview(MaintenanceTask task) => new Rows(
    new TextBlock(Markup.Escape(task.Description)),
    new KeyValueList()
        .Add("Group", Markup.Escape(task.Category))
        .Add("Duration", "about " + task.Minutes + " min per server")
        .Add("Changes", task.Changes ? "[warn]yes[/], makes changes" : "[pass]no[/], read only")
        .Add("Parameter", "[accent]" + Markup.Escape(task.Id) + "[/]")).WithGap(1);

Form OptionsForm() {
    Form form = new Form("Options") { Review = false, SubmitText = "Save", Command = string.Empty };
    FormPage page = form.Page("Options", "Saved with presets; used by Review & run.");
    NumberField parallel = page.Number("Parallel", "Servers at a time");
    parallel.WholeNumber = true;
    parallel.Minimum = 1;
    parallel.Maximum = 16;
    parallel.Default = 4m;
    parallel.Parameter = "ThrottleLimit";
    parallel.Help = "How many servers run their tasks at the same time.";
    PathField report = page.Path("Report", "Report folder");
    report.PathKind = PathKind.Folder;
    report.Default = "reports/toolbox";
    report.Parameter = "ReportPath";
    report.Help = "Created when it does not exist. Tab completes folder names.";
    ChoiceField format = page.Select("Format", "Report format", new[] { new Choice("Html", "HTML"), new Choice("Csv", "CSV"), new Choice("Json", "JSON") });
    format.Default = "Html";
    format.Parameter = "Format";
    ToggleField notify = page.Toggle("Notify", "Email the report");
    notify.Parameter = "Notify";
    TextField email = page.Text("Email", "Recipient");
    email.VisibleWhen = v => v.Get<bool>("Notify");
    email.Required = true;
    email.Placeholder = "ops@example.org";
    email.Parameter = "To";
    email.Validator = (value, values) => value is string s && s.Contains('@') ? null : "Enter an email address";
    ToggleField dryRun = page.Toggle("DryRun", "Dry run");
    dryRun.Parameter = "WhatIf";
    dryRun.Help = "Reports what would change without changing anything.";
    return form;
}

ReviewModel Review(MenuContext context) {
    ReviewModel review = new ReviewModel("Review & run") { Description = "Everything the run will use. The command does the same without the menu." };
    List<Server> servers = targets.GetSelection<Server>().ToList();
    List<MaintenanceTask> selected = tasks.GetSelection<MaintenanceTask>().ToList();
    FormValues values = options.GetValues();
    review.Row("Targets", servers.Count == 0 ? string.Empty : servers.Count + " · " + string.Join(", ", servers.Select(s => s.Name)));
    review.Row("Tasks", selected.Count == 0 ? string.Empty : selected.Count + " · " + string.Join(", ", selected.Select(t => t.Name)));
    review.Row("Servers at a time", options.Find("Parallel")!.Display(values["Parallel"], PromptLabels.Default));
    review.Row("Report", values.Get<string>("Report") + " (" + options.Find("Format")!.Display(values["Format"], PromptLabels.Default) + ")");
    review.Row("Email", values.Get<bool>("Notify") ? values.Get<string>("Email") : "off");
    review.Row("Dry run", values.Get<bool>("DryRun") ? "yes" : "no");
    if (servers.Count == 0) {
        review.Problem("Pick at least one target", targets);
    }
    if (selected.Count == 0) {
        review.Problem("Pick at least one task", tasks);
    }
    if (values.Get<bool>("Notify") && string.IsNullOrWhiteSpace(values.Get<string>("Email"))) {
        review.Problem("Enter the email recipient in Options, or turn off Email the report", optionsItem);
    }
    List<string> arguments = new List<string> {
        CommandLine.Argument("ComputerName", servers.Select(s => s.Name).ToList(), CommandStyle.PowerShell),
        CommandLine.Argument("Task", selected.Select(t => t.Id).ToList(), CommandStyle.PowerShell)
    };
    string optionArguments = options.BuildCommand();
    review.Command = CommandLine.Join("Invoke-ServerToolbox", arguments) + (optionArguments.Length > 0 ? " " + optionArguments : string.Empty);
    review.Run = ctx => Execute(ctx, servers, selected, values);
    return review;
}

void Execute(MenuContext context, List<Server> servers, List<MaintenanceTask> selected, FormValues values) {
    Lanes lanes = new Lanes("lanes");
    foreach (Server server in servers) {
        lanes.Add(server.Name, new RichTextBuilder().Append(server.Name, ThemeToken.Accent).Append("  " + server.Site, ThemeToken.Muted).Build(), selected.Count);
    }
    ProgressBar overall = new ProgressBar("Overall", "overall") { Total = servers.Count * selected.Count, LabelWidth = 8 };
    LogRegion log = new LogRegion { PreferredHeight = 6 };
    Dashboard dashboard = new DashboardBuilder("[bold]Server toolbox[/]")
        .Subtitle(servers.Count + " servers · " + selected.Count + " tasks" + (values.Get<bool>("DryRun") ? " · dry run" : string.Empty))
        .Panel("Servers", lanes)
        .Add(new Padding(overall, 1, 0, 1, 0))
        .Panel("Log", log, fill: true)
        .BuildDashboard();
    int throttle = (int)values.Get<decimal>("Parallel", 4m);
    context.Run(dashboard, run => Work(run, lanes, overall, log, servers, selected, throttle));
}

RunResult Work(RunContext run, Lanes lanesWidget, ProgressBar overallWidget, LogRegion logWidget, List<Server> servers, List<MaintenanceTask> selected, int throttle) {
    LanesHandle lanes = lanesWidget.Handle(run.Session);
    ProgressHandle overall = overallWidget.Handle(run.Session);
    LogHandle log = logWidget.Handle(run.Session);
    Random random = new Random(42);
    int passed = 0, warnings = 0, failed = 0;
    Table failures = new Table { Border = TableBorder.Minimal };
    failures.AddColumn("Server");
    failures.AddColumn("Task");
    failures.AddColumn(new TableColumn("Finding") { Flexible = true });
    Stopwatch watch = Stopwatch.StartNew();
    log.Write("Started " + selected.Count + " tasks on " + servers.Count + " servers, " + throttle + " at a time");
    foreach (MaintenanceTask task in selected) {
        foreach (Server server in servers) {
            run.CancellationToken.ThrowIfCancellationRequested();
            lanes.Start(server.Name, task.Name);
        }
        Thread.Sleep(450);
        foreach (Server server in servers) {
            double roll = random.NextDouble();
            StatusKind outcome = roll < 0.08 ? StatusKind.Fail : roll < 0.2 ? StatusKind.Warn : StatusKind.Pass;
            lanes.Advance(server.Name, outcome, task.Name);
            overall.Advance();
            if (outcome == StatusKind.Fail) {
                failed++;
                string finding = task.Category == "Security" ? "3 updates missing for 41 days" : "volume D: below 5% free";
                failures.AddRow(server.Name, task.Name, finding);
                log.Error(server.Name + " · " + task.Name + ": " + finding);
            } else if (outcome == StatusKind.Warn) {
                warnings++;
                log.Warn(server.Name + " · " + task.Name + ": needs attention");
            } else {
                passed++;
            }
        }
        Thread.Sleep(250);
    }
    foreach (Server server in servers) {
        lanes.Complete(server.Name);
    }
    overall.Complete(failed > 0 ? StatusKind.Fail : warnings > 0 ? StatusKind.Warn : StatusKind.Pass);
    Thread.Sleep(600);
    history.Add(new PastRun(DateTime.Now, servers.Count, selected.Count, passed, warnings, failed, watch.Elapsed));
    RunResult result = new RunResult("Run finished", failed > 0 ? StatusKind.Fail : warnings > 0 ? StatusKind.Warn : StatusKind.Pass) {
        Summary = servers.Count + " servers checked. " + (failed > 0 ? failed + " findings need action." : warnings > 0 ? warnings + " warnings to review; nothing failed." : "Nothing needs action."),
        Details = failed > 0 ? failures : null,
        Elapsed = watch.Elapsed
    };
    result.Figure("passed", passed.ToString(CultureInfo.CurrentCulture));
    if (warnings > 0) {
        result.Figure("warnings", warnings.ToString(CultureInfo.CurrentCulture), StatusKind.Warn);
    } else {
        result.Figure("warnings", "0");
    }
    if (failed > 0) {
        result.Figure("failed", failed.ToString(CultureInfo.CurrentCulture), StatusKind.Fail);
    } else {
        result.Figure("failed", "0");
    }
    result.Next("Open the report", ctx => ctx.Toast("Report opened in the browser", StatusKind.Info));
    if (failed > 0) {
        result.Next("Run the failed tasks again", ctx => ctx.Toast("Queued " + failed + " tasks", StatusKind.Info));
    }
    result.Next("Export findings to CSV", ctx => ctx.Toast("Saved findings.csv"));
    return result;
}

IWidget HistoryPage() {
    Table table = new Table { Border = TableBorder.Minimal };
    table.AddColumn("When");
    table.AddColumn(new TableColumn("Servers") { Alignment = HorizontalAlignment.Right });
    table.AddColumn(new TableColumn("Tasks") { Alignment = HorizontalAlignment.Right });
    table.AddColumn("Result");
    table.AddColumn(new TableColumn("Duration") { Alignment = HorizontalAlignment.Right });
    foreach (PastRun past in Enumerable.Reverse(history)) {
        StatusKind status = past.Failed > 0 ? StatusKind.Fail : past.Warnings > 0 ? StatusKind.Warn : StatusKind.Pass;
        table.AddRow(past.When.ToString("ddd HH:mm", CultureInfo.CurrentCulture), past.Servers.ToString(CultureInfo.CurrentCulture), past.Tasks.ToString(CultureInfo.CurrentCulture),
            TableCell.ForStatus(status, past.Passed + " passed · " + past.Warnings + " warn · " + past.Failed + " failed"), NumberFormat.Duration(past.Elapsed));
    }
    return table;
}
// </more>

record Server(string Name, string Site, string Role, string OperatingSystem, string? Offline = null);
record MaintenanceTask(string Id, string Name, string Category, string Description, int Minutes, bool Changes);
record PastRun(DateTime When, int Servers, int Tasks, int Passed, int Warnings, int Failed, TimeSpan Elapsed);