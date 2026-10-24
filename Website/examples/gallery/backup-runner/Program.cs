// Run a bounded backup wave and retry only jobs that did not return a result.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: synthetic backup jobs; no files, databases or remote services are touched.
var jobs = new[] {
    new { Name = "catalog", Size = "2.4 GiB", Retry = false },
    new { Name = "media", Size = "8.1 GiB", Retry = true },
    new { Name = "settings", Size = "14 MiB", Retry = false },
    new { Name = "audit", Size = "640 MiB", Retry = false }
};

// Layout: queued, first-wave and recovered states use the same result columns.
IWidget Frame(int tick) {
    Table table = new Table { Expand = true };
    table.AddColumn("Job").AddColumn("Size").AddColumn("Attempts")
        .AddColumn("Status", column => column.Presenter = CellPresenters.StatusWord);
    foreach (var job in jobs) {
        string status = tick == 0 ? "Pending" : tick == 1 && job.Retry ? "Warn" : "Pass";
        int attempts = tick == 0 ? 0 : tick == 2 && job.Retry ? 2 : 1;
        table.AddRow(job.Name, job.Size, TableCell.FromValue(attempts), TableCell.FromValue(status));
    }
    string summary = tick == 0 ? "[muted]4 jobs queued · destination ./backups/nightly[/]"
        : tick == 1 ? "[pass]3 passed[/]   [warn]1 queued for retry[/]"
        : "[pass]4 passed[/]   [accent]1 recovered on retry[/]   [muted]retention: 14 days[/]";
    return new Rows(new Rule("[bold]Nightly backup[/] [muted]· throttle 2 · maximum 2 attempts[/]"),
        table, new TextBlock(summary)) { Gap = 1 };
}

// Behaviour: headless snapshots do not start workers. Live jobs emit their result on success.
int at = Array.IndexOf(args, "--step");
int step = at < 0 ? 0 : int.Parse(args[at + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));
if (args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected)) {
    Terminal.Write(Frame(step));
    return;
}
var pending = jobs;
List<string> completed = new List<string>();
List<Exception> attemptErrors = new List<Exception>();
for (int attempt = 1; attempt <= 2 && pending.Length > 0; attempt++) {
    var outcomes = await ConsoleRun.ForEachAsync(pending, async (job, cancel) => {
        await Task.Delay(250, cancel);
        if (job.Retry && attempt == 1) throw new InvalidOperationException("Destination busy; retry this job");
        return "./backups/nightly/" + job.Name + ".zip";
    }, new RunOptions { ThrottleLimit = 2, Title = "Backup attempt " + attempt });
    completed.AddRange(outcomes.Where(result => result.Succeeded).Select(result => result.Value));
    attemptErrors.AddRange(outcomes.Where(result => !result.Succeeded).Select(result => result.Error!));
    pending = outcomes.Where(result => !result.Succeeded).Select(result => result.Item).ToArray();
}
// completed holds archive results; pending and attemptErrors retain unresolved work and diagnostics.
Terminal.Write(Frame(2));
