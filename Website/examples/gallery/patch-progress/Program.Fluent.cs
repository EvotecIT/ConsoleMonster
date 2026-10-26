// Run synthetic work with a concurrency limit and retain failures for retry.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// A parallel dry run: the delay and one failure are synthetic. No server is contacted.
// Data: fictional inputs kept in memory.
string[] servers = { "web-01", "web-02", "web-03", "job-01", "sql-01", "sql-02" };

// Layout: construct the widgets shown on screen.
IWidget Summary(IReadOnlyList<RunResult<string, string>> outcomes)
{
    var byHost = outcomes.ToDictionary(result => result.Item);
    var rows = servers.Select(server =>
    {
        bool finished = byHost.TryGetValue(server, out var outcome);
        string stage = finished ? "Finished" : "Queued";
        string result = !finished ? "-" : outcome!.Succeeded ? "Pass" : "Retry needed";
        return new PatchRow(server, stage, result);
    }).ToArray();
    var table = Ui.Table(rows)
        .Column(row => row.Host)
        .Column(row => row.Stage)
        .Column(row => row.Result, column => column.Presenter(CellPresenters.StatusWord));
    string summary = outcomes.Count > 0
        ? ($"[pass]{outcomes.Count(result => result.Succeeded)} passed[/]  [warn]"
            + $"{outcomes.Count(result => !result.Succeeded)} retry needed[/]")
        : "[muted]Ctrl+C cancels active work.[/]";
    return Ui.Rows().Gap(1).Content(c =>
    {
        c.Markup("[bold]Parallel patch run[/] [muted]· throttle 3[/]");
        c.Add(table);
        c.Markup(summary);
    });
}

// Behaviour: run the actual parallel workload; keep every outcome for retry/reporting.
Summary(Array.Empty<RunResult<string, string>>()).Show();
IReadOnlyList<RunResult<string, string>> results = await ConsoleRun.ForEachAsync(
    servers,
    async (server, cancel) =>
    {
        await Task.Delay(350, cancel);
        if (server == "web-03")
            throw new InvalidOperationException("Restart check failed");
        return server + " preview passed";
    },
    options =>
    {
        options.ThrottleLimit(3).Timeout(TimeSpan.FromSeconds(3)).Title("Patch run");
    });

// results retains each outcome for retry/reporting; no patch command runs here.
Summary(results).Show();

record PatchRow(string Host, string Stage, string Result);
