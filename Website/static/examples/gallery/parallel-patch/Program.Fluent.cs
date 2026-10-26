// Patch a forty-host wave with bounded concurrency, timeouts and retained failures.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: all hosts, outcomes and delays are synthetic. No remote command runs.
Host[] servers = new Host[]
{
    new(Name: "web01", DelayMilliseconds: 637, RestartFailure: false, TimesOut: false),
    new(Name: "web02", DelayMilliseconds: 774, RestartFailure: false, TimesOut: false),
    new(Name: "web03", DelayMilliseconds: 911, RestartFailure: false, TimesOut: false),
    new(Name: "web04", DelayMilliseconds: 1048, RestartFailure: false, TimesOut: false),
    new(Name: "web05", DelayMilliseconds: 1185, RestartFailure: false, TimesOut: false),
    new(Name: "web06", DelayMilliseconds: 1322, RestartFailure: false, TimesOut: false),
    new(Name: "web07", DelayMilliseconds: 559, RestartFailure: false, TimesOut: false),
    new(Name: "web08", DelayMilliseconds: 696, RestartFailure: false, TimesOut: false),
    new(Name: "web09", DelayMilliseconds: 833, RestartFailure: false, TimesOut: false),
    new(Name: "web10", DelayMilliseconds: 970, RestartFailure: false, TimesOut: false),
    new(Name: "web11", DelayMilliseconds: 1107, RestartFailure: false, TimesOut: false),
    new(Name: "web12", DelayMilliseconds: 1244, RestartFailure: false, TimesOut: false),
    new(Name: "web13", DelayMilliseconds: 1381, RestartFailure: true, TimesOut: false),
    new(Name: "web14", DelayMilliseconds: 618, RestartFailure: false, TimesOut: false),
    new(Name: "web15", DelayMilliseconds: 755, RestartFailure: false, TimesOut: false),
    new(Name: "web16", DelayMilliseconds: 892, RestartFailure: false, TimesOut: false),
    new(Name: "web17", DelayMilliseconds: 1029, RestartFailure: false, TimesOut: false),
    new(Name: "web18", DelayMilliseconds: 1166, RestartFailure: false, TimesOut: false),
    new(Name: "web19", DelayMilliseconds: 1303, RestartFailure: false, TimesOut: false),
    new(Name: "web20", DelayMilliseconds: 540, RestartFailure: false, TimesOut: false),
    new(Name: "web21", DelayMilliseconds: 677, RestartFailure: false, TimesOut: false),
    new(Name: "web22", DelayMilliseconds: 814, RestartFailure: false, TimesOut: true),
    new(Name: "web23", DelayMilliseconds: 951, RestartFailure: false, TimesOut: false),
    new(Name: "web24", DelayMilliseconds: 1088, RestartFailure: false, TimesOut: false),
    new(Name: "web25", DelayMilliseconds: 1225, RestartFailure: false, TimesOut: false),
    new(Name: "web26", DelayMilliseconds: 1362, RestartFailure: true, TimesOut: false),
    new(Name: "web27", DelayMilliseconds: 599, RestartFailure: false, TimesOut: false),
    new(Name: "web28", DelayMilliseconds: 736, RestartFailure: false, TimesOut: false),
    new(Name: "web29", DelayMilliseconds: 873, RestartFailure: false, TimesOut: false),
    new(Name: "web30", DelayMilliseconds: 1010, RestartFailure: false, TimesOut: false),
    new(Name: "web31", DelayMilliseconds: 1147, RestartFailure: false, TimesOut: false),
    new(Name: "web32", DelayMilliseconds: 1284, RestartFailure: false, TimesOut: false),
    new(Name: "web33", DelayMilliseconds: 521, RestartFailure: false, TimesOut: false),
    new(Name: "web34", DelayMilliseconds: 658, RestartFailure: false, TimesOut: false),
    new(Name: "web35", DelayMilliseconds: 795, RestartFailure: false, TimesOut: false),
    new(Name: "web36", DelayMilliseconds: 932, RestartFailure: false, TimesOut: false),
    new(Name: "web37", DelayMilliseconds: 1069, RestartFailure: false, TimesOut: false),
    new(Name: "web38", DelayMilliseconds: 1206, RestartFailure: false, TimesOut: false),
    new(Name: "web39", DelayMilliseconds: 1343, RestartFailure: true, TimesOut: false),
    new(Name: "web40", DelayMilliseconds: 580, RestartFailure: false, TimesOut: false)
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
    StatusKind State(string host) => byHost.TryGetValue(host, out var result) ? result.Succeeded ? StatusKind.Pass : StatusKind.Fail : StatusKind.Pending;
    CellMap map = Ui.CellMap().Columns(10).LegendLines(2);
    foreach (Host server in servers)
        map.Add(State(server.Name));
    foreach (string label in new[]
    {
        "web01–10",
        "web11–20",
        "web21–30",
        "web31–40"
    }

    )
        map.RowLabels.Add(label);
    SummaryBar summary = Ui.SummaryBar().SetProgress(done, servers.Length).SetCounter("passed", done - failed, StatusKind.Pass).SetCounter("failed", failed, StatusKind.Fail).SetCounter("running", 0, StatusKind.Running).SetCounter("pending", servers.Length - done, StatusKind.Pending);
    Lanes lanes = Ui.Lanes();
    foreach (Host host in finished ? servers.TakeLast(throttle) : servers.Take(throttle))
    {
        bool hasResult = byHost.TryGetValue(host.Name, out var result);
        Lane lane = lanes.Add(host.Name, host.Name, 4);
        lane.Value(hasResult ? 4 : 0);
        lane.Status(State(host.Name));
        lane.Passed(hasResult ? result!.Succeeded ? 4 : 3 : 0);
        lane.Failed(hasResult && !result!.Succeeded ? 1 : 0);
        lane.Detail(!hasResult ? "Waiting for a worker" : result!.Succeeded ? "Health verified" : result.TimedOut ? "4s timeout · retry separately" : "Restart verification failed");
    }

    StatusList failures = Ui.StatusList();
    foreach (var result in outcomes.Where(result => !result.Succeeded))
    {
        failures.Add(result.Item.Name, StatusKind.Fail, result.TimedOut ? "4s timeout · retry separately" : "Restart verification failed");
    }

    IWidget failureContent = failures.Items.Count == 0 ? Ui.Text().Content("[muted]No failures recorded[/]").IsMarkup() : failures;
    return Ui.Rows().Gap(1).Content(c2 =>
    {
        c2.Rule("[bold]Patching the web farm[/]");
        c2.Text(c2Text => c2Text.Content("40 hosts · throttle 8 · per-host timeout 4s").IsMarkup());
        c2.Add(summary);
        c2.Panel(cmPanel => cmPanel.Title("Host outcomes").Content(c =>
        {
            c.Add(map);
        }));
        c2.Panel(cmPanel => cmPanel.Title(finished ? "Last eight hosts" : "Worker lanes").Content(c =>
        {
            c.Add(lanes);
        }));
        c2.Panel(cmPanel => cmPanel.Title("Retained failures").Content(c =>
        {
            c.Add(failureContent);
        }));
        c2.Text(c2Text => c2Text.Content(finished ? "[bold]Wave finished[/] · failed hosts remain available for a separate retry" : "[muted]Ctrl+C cancels active work and skips queued hosts[/]").IsMarkup());
    });
}

// Behaviour: the native run monitor owns scheduling, cancellation and the live UI.
Summary(Array.Empty<RunResult<Host, string>>()).Show();
IReadOnlyList<RunResult<Host, string>> results = await ConsoleRun.ForEachAsync(servers, async (server, cancel) =>
{
    await Task.Delay(server.DelayMilliseconds, cancel);
    if (server.RestartFailure)
        throw new InvalidOperationException(server.Name + " restart verification failed");
    if (server.TimesOut)
        await Task.Delay(TimeSpan.FromSeconds(10), cancel);
    return server.Name + " patched";
}, Ui.Options<RunOptions>().ThrottleLimit(throttle).Timeout(TimeSpan.FromSeconds(timeoutSeconds)).Title("Patching the web farm").Live(true).Configure(configured => configured.Label = item => ((Host)item!).Name));
// Keep the structured outcomes; never retry automatically after a failure.
Summary(results).Show();
record Host(string Name, int DelayMilliseconds, bool RestartFailure, bool TimesOut);
