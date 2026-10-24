// A guided tour of typed prompts, task previews and a dashboard paused for confirmation.
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Text;
using ConsoleMonster.Widgets;

// Data: fictional inventory and tasks. Answers never trigger the operations described.
// The path points into an absent sample folder; no directories or presets are created.
var servers = new[] {
    new Server("FS01", "Warsaw", "File", "Windows Server 2022"),
    new Server("FS02", "Warsaw", "File", "Windows Server 2022"),
    new Server("WEB01", "Warsaw", "Web", "Linux"),
    new Server("SQL01", "Warsaw", "Database", "Linux"),
    new Server("FS11", "Berlin", "File", "Windows Server 2022"),
    new Server("WEB11", "Berlin", "Web", "Linux"),
    new Server("PRN11", "Berlin", "Print", "Windows Server 2022", "offline since 09:12"),
    new Server("BKP21", "Dublin", "Backup", "Linux"),
    new Server("SQL21", "Dublin", "Database", "Linux"),
    new Server("LAB01", "Lab", "Test", "Linux"),
    new Server("LAB02", "Lab", "Test", "Linux", "in maintenance until Friday")
};
var tasks = new[] {
    new Job("DiskCleanup", "Disk cleanup", "Maintenance", "Removes temporary files, old update caches and crash dumps older than 30 days.", 3, true),
    new Job("LogRotation", "Log rotation", "Maintenance", "Compresses logs older than 7 days and removes archives older than 90 days.", 2, true),
    new Job("RestartPending", "Pending restart check", "Maintenance", "Reports servers waiting for a restart after updates or role changes.", 1, false),
    new Job("PatchStatus", "Patch status", "Security", "Lists missing security updates and the last successful install.", 2, false),
    new Job("Certificates", "Certificate expiry", "Security", "Finds certificates expiring within 30 days and the services using them.", 1, false),
    new Job("LocalAdmins", "Local administrators audit", "Security", "Compares local administrator membership with the approved list.", 1, false),
    new Job("Firewall", "Firewall rules review", "Security", "Flags inbound allow rules open to any address.", 2, false),
    new Job("EventErrors", "Event log errors", "Health", "Summarizes errors from the last 24 hours.", 1, false),
    new Job("Services", "Service health", "Health", "Checks automatic services and restarts stopped ones.", 1, true),
    new Job("DiskSpace", "Disk space", "Health", "Warns below 15% free and fails below 5% on each volume.", 1, false),
    new Job("Uptime", "Uptime and reboots", "Health", "Reports uptime and unexpected reboots in the last 30 days.", 1, false)
};
string[] titles = { "Typed selection", "Multiple selection", "Confirmation", "Text", "Secret", "Number", "Path", "Typed confirmation", "Dashboard pause" };
string reportFolder = "./toolbox-reports/weekly";
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 8) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: the prompts themselves draw choices, validation, masks and editing hints.
IWidget Preview(Job job) => new Rows(new TextBlock(Markup.Escape(job.Description)),
    new KeyValueList().Add("Group", Markup.Escape(job.Category)).Add("Duration", $"about {job.Minutes} min per server")
        .Add("Changes", job.Changes ? "[warn]yes[/], makes changes" : "[pass]no[/], read only")
        .Add("Parameter", "[accent]" + job.Id + "[/]")) { Gap = 1 };
SelectPrompt<Server> ServerPrompt() {
    SelectPrompt<Server> prompt = new SelectPrompt<Server>("Target server", servers, s => s.Name, s => s.Site) {
        Description = "Type to filter; offline servers show why they cannot be picked.", Default = servers[0]
    };
    foreach (Choice choice in prompt.Choices) {
        Server server = (Server)choice.Value!;
        choice.Badge = RichText.Plain(server.Role, new RichStyle(ThemeToken.Muted));
        choice.DisabledReason = server.Offline;
        choice.Description = server.Role + " server · " + server.OS;
    }
    return prompt;
}
MultiSelectPrompt<Job> TaskPrompt() {
    Choice[] choices = tasks.Select(t => new Choice(t, t.Name) {
        Group = t.Category, Description = t.Description,
        Badge = new RichTextBuilder().Append(t.Changes ? "changes " : "", ThemeToken.Warn)
            .Append($"~{t.Minutes} min", ThemeToken.Muted).Build(),
        Selected = t.Id is "PatchStatus" or "DiskSpace"
    }).ToArray();
    return new MultiSelectPrompt<Job>("Tasks", choices) {
        Minimum = 1, Preview = Preview,
        Default = tasks.Where(t => t.Id is "PatchStatus" or "DiskSpace").ToArray()
    };
}

// Render the initial native prompt without reading input or returning an answer.
void Draw<T>(Prompt<T> prompt) {
    Terminal.Write(prompt.Snapshot());
}
T Ask<T>(Prompt<T> prompt) {
    if (snapshot) { Draw(prompt); return default!; }
    return prompt.Show();
}

// Behaviour: each answer is returned as its original type; the tour performs no action.
for (int page = snapshot ? step : 0; page <= (snapshot ? step : 8); page++) {
    if (!snapshot) Terminal.Write(new Rule($"[bold]{page + 1} / 9 · {titles[page]}[/]"));
    switch (page) {
        case 0:
            Server server = Ask(ServerPrompt());
            if (!snapshot) Terminal.WriteMarkupLine("[muted]returned:[/] " + Markup.Escape(server.Name + " (Server)"));
            break;
        case 1:
            IReadOnlyList<Job> selected = Ask(TaskPrompt());
            if (!snapshot) Terminal.WriteMarkupLine("[muted]returned:[/] " + string.Join(", ", selected.Select(t => t.Id)));
            break;
        case 2:
            Ask(new ConfirmPrompt("Restart the report agent on FS01?", true));
            break;
        case 3:
            Ask(new TextPrompt("Share name") {
                Default = "Reports", Pattern = "[A-Za-z][A-Za-z0-9_-]{1,23}",
                PatternMessage = "Start with a letter; letters, digits, - and _ only",
                Description = "Shown to users when they browse the server."
            });
            break;
        case 4:
            Ask(new SecretPrompt("Password for svc-backup") {
                MinLength = 8, Description = "Try example-passphrase; at least 8 characters."
            });
            break;
        case 5:
            Ask(new NumberPrompt("Port") { Integer = true, Minimum = 1, Maximum = 65535, Default = 443 });
            break;
        case 6:
            Ask(new PathPrompt("Report folder") { Kind = PathKind.Folder, Default = reportFolder });
            break;
        case 7:
            Ask(new TypedConfirmPrompt("Remove FS01 from the inventory?", "FS01") {
                Default = false, Warning = "Its history and reports would be removed too. Type FS01 to confirm."
            });
            break;
        case 8:
            if (snapshot) {
                Draw(new ConfirmPrompt("WEB01 failed. Continue with the other servers?", true) {
                    Description = "The patch dashboard pauses for your answer, then resumes."
                });
                break;
            }
            Lanes lanes = new Lanes();
            foreach (Server host in servers.Take(5)) lanes.Add(host.Name, host.Name, 6);
            LogRegion log = new LogRegion { PreferredHeight = 6 };
            Dashboard dashboard = new DashboardBuilder("[bold]Patching[/]").Subtitle("5 servers")
                .Panel("Servers", lanes).Panel("Log", log, fill: true).BuildDashboard();
            using (LiveSession live = new LiveSession(dashboard)) {
                live.Run(s => {
                    for (int tick = 1; tick <= 6; tick++) {
                        s.Cancellation.ThrowIfCancellationRequested();
                        foreach (Server host in servers.Take(5)) lanes.Handle(s).Advance(host.Name,
                            host.Name == "WEB01" && tick == 3 ? StatusKind.Fail : StatusKind.Pass, "step " + tick);
                        log.Handle(s).Write("step " + tick + " done");
                        Thread.Sleep(350);
                        if (tick == 3) {
                            log.Handle(s).Error("WEB01: update verification failed");
                            bool go = new ConfirmPrompt("WEB01 failed. Continue with the other servers?", true) {
                                Description = "The patch dashboard pauses for your answer, then resumes."
                            }.Show();
                            log.Handle(s).Write(go ? "continuing" : "stopping after this step");
                            if (!go) break;
                        }
                    }
                });
            }
            break;
    }
}

record Server(string Name, string Site, string Role, string OS, string? Offline = null);
record Job(string Id, string Name, string Category, string Description, int Minutes, bool Changes);
