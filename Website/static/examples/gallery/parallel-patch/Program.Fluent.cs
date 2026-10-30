// Patch a forty-host wave with bounded concurrency, timeouts and retained failures.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: all hosts, outcomes and delays are synthetic. No remote command runs.
Host[] servers = new Host[]
{
    new("web01", 637, false, false),
    new("web02", 774, false, false),
    new("web03", 911, false, false),
    new("web04", 1048, false, false),
    new("web05", 1185, false, false),
    new("web06", 1322, false, false),
    new("web07", 559, false, false),
    new("web08", 696, false, false),
    new("web09", 833, false, false),
    new("web10", 970, false, false),
    new("web11", 1107, false, false),
    new("web12", 1244, false, false),
    new("web13", 1381, true, false),
    new("web14", 618, false, false),
    new("web15", 755, false, false),
    new("web16", 892, false, false),
    new("web17", 1029, false, false),
    new("web18", 1166, false, false),
    new("web19", 1303, false, false),
    new("web20", 540, false, false),
    new("web21", 677, false, false),
    new("web22", 814, false, true),
    new("web23", 951, false, false),
    new("web24", 1088, false, false),
    new("web25", 1225, false, false),
    new("web26", 1362, true, false),
    new("web27", 599, false, false),
    new("web28", 736, false, false),
    new("web29", 873, false, false),
    new("web30", 1010, false, false),
    new("web31", 1147, false, false),
    new("web32", 1284, false, false),
    new("web33", 521, false, false),
    new("web34", 658, false, false),
    new("web35", 795, false, false),
    new("web36", 932, false, false),
    new("web37", 1069, false, false),
    new("web38", 1206, false, false),
    new("web39", 1343, true, false),
    new("web40", 580, false, false)
};
const int throttle = 8;
const int timeoutSeconds = 4;

// Layout: a wave summary, every host, eight lane details and retained failures.
IWidget Summary(IReadOnlyList<RunResult<Host, string>> outcomes)
{
    var byHost = outcomes.ToDictionary(result => result.Item.Name);
    int done = outcomes.Count;
    int failed = outcomes.Count(result => !result.Succeeded);
    bool finished = done == servers.Length;
    StatusKind State(string host) => byHost.TryGetValue(host, out var result)
        ? result.Succeeded ? StatusKind.Pass : StatusKind.Fail
        : StatusKind.Pending;
    CellMap map = Ui.CellMap().Columns(10).LegendLines(2);
    foreach (Host server in servers)
        map.Add(State(server.Name));
    foreach (string label in new[] { "web01–10", "web11–20", "web21–30", "web31–40" }

    )
        map.RowLabels.Add(label);
    SummaryBar summary = Ui.SummaryBar()
        .SetProgress(done, servers.Length)
        .SetCounter("passed", done - failed, StatusKind.Pass)
        .SetCounter("failed", failed, StatusKind.Fail)
        .SetCounter("running", 0, StatusKind.Running)
        .SetCounter("pending", servers.Length - done, StatusKind.Pending);
    Lanes lanes = Ui.Lanes();
    foreach (Host host in finished ? servers.TakeLast(throttle) : servers.Take(throttle))
    {
        bool hasResult = byHost.TryGetValue(host.Name, out var result);
        Lane lane = lanes.Add(host.Name, host.Name, 4)
            .Value(hasResult ? 4 : 0)
            .Status(State(host.Name))
            .Passed(hasResult ? result!.Succeeded ? 4 : 3 : 0)
            .Failed(hasResult && !result!.Succeeded ? 1 : 0)
            .Detail(!hasResult
                ? "Waiting for a worker"
                : result!.Succeeded
                    ? "Health verified"
                    : result.TimedOut
                        ? "4s timeout · retry separately"
                        : "Restart verification failed");
    }

    StatusList failures = Ui.StatusList();
    foreach (var result in outcomes.Where(result => !result.Succeeded))
    {
        failures.Add(result.Item.Name, StatusKind.Fail,
            result.TimedOut ? "4s timeout · retry separately" : "Restart verification failed");
    }

    IWidget failureContent = failures.Items.Count == 0
        ? Ui.Markup("[muted]No failures recorded[/]")
        : failures;
    return Ui.Rows().Gap(1).Content(c2 =>
    {
        c2.Rule("[bold]Patching the web farm[/]");
        c2.Markup("40 hosts · throttle 8 · per-host timeout 4s");
        c2.Add(summary);
        c2.Panel(p => p.Title("Host outcomes").Content(c =>
        {
            c.Add(map);
        }));
        c2.Panel(p => p.Title(finished ? "Last eight hosts" : "Worker lanes").Content(c =>
        {
            c.Add(lanes);
        }));
        c2.Panel(p => p.Title("Retained failures").Content(c =>
        {
            c.Add(failureContent);
        }));
        c2.Markup(finished
            ? "[bold]Wave finished[/] · failed hosts remain available for a separate retry"
            : "[muted]Ctrl+C cancels active work and skips queued hosts[/]");
    });
}

// Behaviour: the native run monitor owns scheduling, cancellation and the live UI.
Summary(Array.Empty<RunResult<Host, string>>()).Show();
IReadOnlyList<RunResult<Host, string>> results = await ConsoleRun.ForEachAsync(
    servers,
    async (server, cancel) =>
    {
        await Task.Delay(server.DelayMilliseconds, cancel);
        if (server.RestartFailure)
            throw new InvalidOperationException(server.Name + " restart verification failed");
        if (server.TimesOut)
            await Task.Delay(TimeSpan.FromSeconds(10), cancel);
        return server.Name + " patched";
    },
    options =>
    {
        options.ThrottleLimit(throttle)
            .Timeout(TimeSpan.FromSeconds(timeoutSeconds))
            .Title("Patching the web farm")
            .Live(true)
            .Configure(configured => configured.Label = item => ((Host)item!).Name);
    });

// Keep the structured outcomes; never retry automatically after a failure.
Summary(results).Show();

record Host(string Name, int DelayMilliseconds, bool RestartFailure, bool TimesOut);
