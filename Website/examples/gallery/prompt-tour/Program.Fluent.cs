// A guided tour of typed prompts, task previews and a dashboard paused for confirmation.
using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: fictional inventory and tasks. Answers never trigger the operations described.
// The path points into an absent sample folder; no directories or presets are created.
Server[] servers = new Server[]
{
    new(Name: "FS01", Site: "Warsaw", Role: "File", OS: "Windows Server 2022"),
    new(Name: "FS02", Site: "Warsaw", Role: "File", OS: "Windows Server 2022"),
    new(Name: "WEB01", Site: "Warsaw", Role: "Web", OS: "Linux"),
    new(Name: "SQL01", Site: "Warsaw", Role: "Database", OS: "Linux"),
    new(Name: "FS11", Site: "Berlin", Role: "File", OS: "Windows Server 2022"),
    new(Name: "WEB11", Site: "Berlin", Role: "Web", OS: "Linux"),
    new(Name: "PRN11", Site: "Berlin", Role: "Print", OS: "Windows Server 2022", Offline: "offline since 09:12"),
    new(Name: "BKP21", Site: "Dublin", Role: "Backup", OS: "Linux"),
    new(Name: "SQL21", Site: "Dublin", Role: "Database", OS: "Linux"),
    new(Name: "LAB01", Site: "Lab", Role: "Test", OS: "Linux"),
    new(Name: "LAB02", Site: "Lab", Role: "Test", OS: "Linux", Offline: "in maintenance until Friday")
};
Job[] tasks = new Job[]
{
    new(Id: "DiskCleanup", Name: "Disk cleanup", Category: "Maintenance", Description: "Removes temporary files, old update caches and crash dumps older than 30 days.", Minutes: 3, Changes: true),
    new(Id: "LogRotation", Name: "Log rotation", Category: "Maintenance", Description: "Compresses logs older than 7 days and removes archives older than 90 days.", Minutes: 2, Changes: true),
    new(Id: "RestartPending", Name: "Pending restart check", Category: "Maintenance", Description: "Reports servers waiting for a restart after updates or role changes.", Minutes: 1, Changes: false),
    new(Id: "PatchStatus", Name: "Patch status", Category: "Security", Description: "Lists missing security updates and the last successful install.", Minutes: 2, Changes: false),
    new(Id: "Certificates", Name: "Certificate expiry", Category: "Security", Description: "Finds certificates expiring within 30 days and the services using them.", Minutes: 1, Changes: false),
    new(Id: "LocalAdmins", Name: "Local administrators audit", Category: "Security", Description: "Compares local administrator membership with the approved list.", Minutes: 1, Changes: false),
    new(Id: "Firewall", Name: "Firewall rules review", Category: "Security", Description: "Flags inbound allow rules open to any address.", Minutes: 2, Changes: false),
    new(Id: "EventErrors", Name: "Event log errors", Category: "Health", Description: "Summarizes errors from the last 24 hours.", Minutes: 1, Changes: false),
    new(Id: "Services", Name: "Service health", Category: "Health", Description: "Checks automatic services and restarts stopped ones.", Minutes: 1, Changes: true),
    new(Id: "DiskSpace", Name: "Disk space", Category: "Health", Description: "Warns below 15% free and fails below 5% on each volume.", Minutes: 1, Changes: false),
    new(Id: "Uptime", Name: "Uptime and reboots", Category: "Health", Description: "Reports uptime and unexpected reboots in the last 30 days.", Minutes: 1, Changes: false)
};
string reportFolder = "./toolbox-reports/weekly";
// Layout: the prompts themselves draw choices, validation, masks and editing hints.
IWidget Preview(Job job) => Ui.Rows().Gap(1).Content(c =>
{
    c.Text(cText => cText.Content(Markup.Escape(job.Description)).IsMarkup());
    c.KeyValue(cmKeyValue => cmKeyValue.Add("Group", Markup.Escape(job.Category)).Add("Duration", $"about {job.Minutes} min per server").Add("Changes", job.Changes ? "[warn]yes[/], makes changes" : "[pass]no[/], read only").Add("Parameter", "[accent]" + job.Id + "[/]"));
});
SelectPrompt<Server> ServerPrompt()
{
    SelectPrompt<Server> prompt = Ui.SelectPrompt<Server>().Title("Target server").AddChoices(ConsoleMonster.Interactive.Choice.From(servers, s => s.Name, s => s.Site)).Description("Type to filter; offline servers show why they cannot be picked.").Default(servers[0]);
    foreach (Choice choice in prompt.Choices)
    {
        Server server = (Server)choice.Value!;
        choice.Badge(RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted)));
        choice.DisabledReason(server.Offline);
        choice.Description(server.Role + " server · " + server.OS);
    }

    return prompt;
}

MultiSelectPrompt<Job> TaskPrompt()
{
    Choice[] choices = tasks.Select(t => new Choice(t, t.Name).Group(t.Category).Description(t.Description).Badge(Ui.RichText().Append(t.Changes ? "changes " : "", ThemeToken.Warn).Append($"~{t.Minutes} min", ThemeToken.Muted).Build()).Selected(t.Id is "PatchStatus" or "DiskSpace")).ToArray();
    return Ui.MultiSelectPrompt<Job>().Title("Tasks").AddChoices(choices).Minimum(1).Configure(configured => configured.Preview = Preview).Default(tasks.Where(t => t.Id is "PatchStatus" or "DiskSpace").ToArray());
}

// Behaviour: each native prompt returns an answer; the tour performs no external action.
Ui.Rule().Title("[bold]1 / 9 · Typed selection[/]").Show();
Server server = ServerPrompt().Show();
Terminal.WriteMarkupLine("[muted]returned:[/] " + Markup.Escape(server.Name + " (Server)"));
Ui.Rule().Title("[bold]2 / 9 · Multiple selection[/]").Show();
IReadOnlyList<Job> selected = TaskPrompt().Show();
Terminal.WriteMarkupLine("[muted]returned:[/] " + string.Join(", ", selected.Select(t => t.Id)));
Ui.Rule().Title("[bold]3 / 9 · Confirmation[/]").Show();
Ui.ConfirmPrompt().Title("Restart the report agent on FS01?").Default(true).Show();
Ui.Rule().Title("[bold]4 / 9 · Text[/]").Show();
Ui.TextPrompt().Title("Share name").Default("Reports").Pattern("[A-Za-z][A-Za-z0-9_-]{1,23}").PatternMessage("Start with a letter; letters, digits, - and _ only").Description("Shown to users when they browse the server.").Show();
Ui.Rule().Title("[bold]5 / 9 · Secret[/]").Show();
Ui.SecretPrompt().Title("Password for svc-backup").MinLength(8).Description("Try example-passphrase; at least 8 characters.").Show();
Ui.Rule().Title("[bold]6 / 9 · Number[/]").Show();
Ui.NumberPrompt().Title("Port").Integer().Minimum(1).Maximum(65535).Default(443).Show();
Ui.Rule().Title("[bold]7 / 9 · Path[/]").Show();
Ui.PathPrompt().Title("Report folder").Kind(PathKind.Folder).Default(reportFolder).Show();
Ui.Rule().Title("[bold]8 / 9 · Typed confirmation[/]").Show();
Ui.TypedConfirmPrompt().Title("Remove FS01 from the inventory?").Expected("FS01").Default(false).Warning("Its history and reports would be removed too. Type FS01 to confirm.").Show();
Ui.Rule().Title("[bold]9 / 9 · Dashboard pause[/]").Show();
Lanes lanes = Ui.Lanes();
foreach (Server host in servers.Take(5))
    lanes.Add(host.Name, host.Name, 6);
LogRegion log = Ui.LogRegion().PreferredHeight(6);
Dashboard dashboard = Ui.DashboardLayout().Title("[bold]Patching[/]").Subtitle("5 servers").Panel("Servers", lanes).Panel("Log", log, fill: true).BuildDashboard();
using (LiveSession session = dashboard.CreateSession())
{
    try
    {
        session.Run(live =>
        {
            for (int tick = 1; tick <= 6; tick++)
            {
                live.Cancellation.ThrowIfCancellationRequested();
                foreach (Server host in servers.Take(5))
                {
                    lanes.Handle(live).Advance(host.Name, host.Name == "WEB01" && tick == 3 ? StatusKind.Fail : StatusKind.Pass, "step " + tick);
                }

                log.Handle(live).Write("step " + tick + " done");
                live.Delay(TimeSpan.FromMilliseconds(350));
                if (tick == 3)
                {
                    log.Handle(live).Error("WEB01: update verification failed");
                    bool go = Ui.ConfirmPrompt().Title("WEB01 failed. Continue with the other servers?").Default(true).Description("The patch dashboard pauses for your answer, then resumes.").Show();
                    log.Handle(live).Write(go ? "continuing" : "stopping after this step");
                    if (!go)
                        break;
                }
            }
        });
    }
    catch (OperationCanceledException cancelled)when (cancelled.CancellationToken == session.Cancellation)
    {
        // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
        return;
    }
}

record Server(string Name, string Site, string Role, string OS, string? Offline = null);
record Job(string Id, string Name, string Category, string Description, int Minutes, bool Changes);
