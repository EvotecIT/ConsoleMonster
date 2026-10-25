// Run a bounded backup wave and retry only jobs that did not return a result.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: synthetic backup jobs; no files, databases or remote services are touched.
var jobs = new[]
{
    new
    {
        Name = "catalog",
        Size = "2.4 GiB",
        Retry = false
    },
    new
    {
        Name = "media",
        Size = "8.1 GiB",
        Retry = true
    },
    new
    {
        Name = "settings",
        Size = "14 MiB",
        Retry = false
    },
    new
    {
        Name = "audit",
        Size = "640 MiB",
        Retry = false
    }
};
Dictionary<string, int> attempts = new Dictionary<string, int>();
Dictionary<string, string> completed = new Dictionary<string, string>();
List<Exception> attemptErrors = new List<Exception>();
// Layout: show the actual attempt counts and returned archive results.
IWidget Summary()
{
    Table table = new Table().Expand(true);
    table.AddColumn("Job").AddColumn("Size").AddColumn("Attempts").AddColumn("Status", column => column.Presenter(CellPresenters.StatusWord));
    foreach (var job in jobs)
    {
        int count = attempts.GetValueOrDefault(job.Name);
        string status = completed.ContainsKey(job.Name) ? "Pass" : count == 0 ? "Pending" : "Warn";
        table.AddRow(job.Name, job.Size, TableCell.FromValue(count), TableCell.FromValue(status));
    }

    int recovered = completed.Keys.Count(name => attempts[name] > 1);
    string summary = attempts.Count == 0 ? "[muted]4 jobs queued · destination ./backups/nightly[/]" : completed.Count < jobs.Length ? $"[pass]{completed.Count} passed[/]   [warn]{jobs.Length - completed.Count} queued for retry[/]" : $"[pass]{completed.Count} passed[/]   [accent]{recovered} recovered on retry[/]   [muted]retention: 14 days[/]";
    return new Rows(new Rule("[bold]Nightly backup[/] [muted]· throttle 2 · maximum 2 attempts[/]"), table, new TextBlock(summary)).Gap(1);
}

// Behaviour: run the queued jobs, retain errors, then retry only missing results.
Summary().Show();
var pending = jobs;
for (int attempt = 1; attempt <= 2 && pending.Length > 0; attempt++)
{
    foreach (var job in pending)
        attempts[job.Name] = attempt;
    var outcomes = await ConsoleRun.ForEachAsync(pending, async (job, cancel) =>
    {
        await Task.Delay(250, cancel);
        if (job.Retry && attempt == 1)
            throw new InvalidOperationException("Destination busy; retry this job");
        return "./backups/nightly/" + job.Name + ".zip";
    }, new RunOptions().ThrottleLimit(2).Title("Backup attempt " + attempt));
    foreach (var result in outcomes.Where(result => result.Succeeded))
        completed[result.Item.Name] = result.Value;
    attemptErrors.AddRange(outcomes.Where(result => !result.Succeeded).Select(result => result.Error!));
    pending = outcomes.Where(result => !result.Succeeded).Select(result => result.Item).ToArray();
    Summary().Show();
} // completed holds archive results; pending and attemptErrors retain unresolved work and diagnostics.

