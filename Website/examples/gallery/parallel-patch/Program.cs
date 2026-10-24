// Patch a forty-host wave with bounded concurrency, timeouts and retained failures.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: all hosts, outcomes and delays are synthetic. No remote command runs.
string[] servers = Enumerable.Range(1, 40).Select(n => $"web{n:00}").ToArray();
string[] operations = { "Stage update", "Apply update", "Restart", "Verify health" };
int[] restartFailures = { 13, 26, 39 };
int timeoutHost = 22;
const int throttle = 8;
const int timeoutSeconds = 4;
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 3) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: a wave summary, every host, eight lane details and retained failures.
IWidget Frame(int phase, IReadOnlyList<RunResult<string, string>>? actual = null) {
    int done = new[] { 0, 8, 24, 40 }[phase];
    int active = phase is 0 or 3 ? 0 : 8;
    StatusKind State(int n) => n <= done
        ? restartFailures.Contains(n) || n == timeoutHost ? StatusKind.Fail : StatusKind.Pass
        : n <= done + active ? StatusKind.Running : StatusKind.Pending;
    if (actual != null) done = actual.Count;
    CellMap map = new CellMap { Columns = 10, LegendLines = 2 };
    for (int i = 0; i < servers.Length; i++) {
        map.Add(actual == null ? State(i + 1) : actual[i].Succeeded ? StatusKind.Pass : StatusKind.Fail);
    }
    foreach (string label in new[] { "web01–10", "web11–20", "web21–30", "web31–40" }) map.RowLabels.Add(label);
    int failed = actual?.Count(r => !r.Succeeded) ?? Enumerable.Range(1, done).Count(n => State(n) == StatusKind.Fail);
    SummaryBar summary = new SummaryBar();
    summary.SetProgress(done, 40);
    summary.SetCounter("passed", done - failed, StatusKind.Pass);
    summary.SetCounter("failed", failed, StatusKind.Fail);
    summary.SetCounter("running", active, StatusKind.Running);
    summary.SetCounter("pending", 40 - done - active, StatusKind.Pending);
    Lanes lanes = new Lanes();
    for (int lane = 0; lane < throttle; lane++) {
        int n = phase == 3 ? 33 + lane : done + lane + 1;
        StatusKind status = actual == null ? State(n) : actual[n - 1].Succeeded ? StatusKind.Pass : StatusKind.Fail;
        bool finished = phase == 3;
        int progress = finished ? 4 : phase == 0 ? 0 : 1 + lane % 3;
        string detail = finished ? (status == StatusKind.Fail ? "Restart verification failed" : "Health verified")
            : phase == 0 ? "Waiting for a worker" : operations[progress - 1];
        Lane item = lanes.Add(servers[n - 1], servers[n - 1], 4);
        item.Done = progress;
        item.Status = status;
        item.Passed = finished ? status == StatusKind.Pass ? 4 : 3 : progress;
        item.Failed = finished && status == StatusKind.Fail ? 1 : 0;
        item.Detail = detail;
    }
    StatusList failures = new StatusList();
    if (actual != null) {
        foreach (var result in actual.Where(r => !r.Succeeded)) {
            failures.Add(result.Item, StatusKind.Fail, result.TimedOut ? "4s timeout · retry separately" : "Restart verification failed");
        }
    } else {
        foreach (int n in Enumerable.Range(1, done).Where(n => State(n) == StatusKind.Fail)) {
            failures.Add(servers[n - 1], StatusKind.Fail, n == timeoutHost ? "4s timeout · retry separately" : "Restart verification failed");
        }
    }
    IWidget failureContent = failures.Items.Count == 0 ? new TextBlock("[muted]No failures recorded[/]") : failures;
    return new Rows(new Rule("[bold]Patching the web farm[/]"),
        new TextBlock("40 hosts · throttle 8 · per-host timeout 4s"), summary,
        new Panel(map, "Host outcomes"),
        new Panel(lanes, phase == 3 ? "Last eight hosts" : "Worker lanes"),
        new Panel(failureContent, "Retained failures"),
        new TextBlock(phase == 3 ? "[bold]Wave finished[/] · failed hosts remain available for a separate retry"
            : "[muted]Ctrl+C cancels active work and skips queued hosts[/]")) { Gap = 1 };
}

// Behaviour: the native run monitor owns scheduling, cancellation and the live UI.
if (snapshot) {
    Terminal.Write(Frame(step));
    return;
}
IReadOnlyList<RunResult<string, string>> results = await ConsoleRun.ForEachAsync(servers, async (server, cancel) => {
    int n = int.Parse(server.AsSpan(3), System.Globalization.CultureInfo.InvariantCulture);
    await Task.Delay(500 + n * 137 % 900, cancel);
    if (restartFailures.Contains(n)) throw new InvalidOperationException(server + " restart verification failed");
    if (n == timeoutHost) await Task.Delay(TimeSpan.FromSeconds(10), cancel);
    return server + " patched";
}, new RunOptions {
    ThrottleLimit = throttle, Timeout = TimeSpan.FromSeconds(timeoutSeconds),
    Title = "Patching the web farm", Interactive = true
});
// Keep the structured outcomes; never retry automatically after a failure.
Terminal.Write(Frame(3, results));
