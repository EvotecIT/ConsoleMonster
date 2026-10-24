using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));
// A parallel dry run: the delay and one failure are synthetic. No server is contacted.
string[] servers = { "web-01", "web-02", "web-03", "job-01", "sql-01", "sql-02" };
IWidget Frame(int tick) {
    Table table = new Table().AddColumn("Host").AddColumn("Stage").AddColumn("Result", column => column.Presenter = CellPresenters.StatusWord);
    foreach (string server in servers) {
        string stage = new[] { "Queued", "Patching", "Finished" }[tick];
        string result = tick < 2 ? "-" : server == "web-03" ? "Retry needed" : "Pass";
        table.AddRow(server, stage, TableCell.FromValue(result));
    }
    string summary = tick == 2 ? "[pass]5 passed[/]  [warn]1 retry needed[/]  [muted]0 real hosts changed[/]"
        : "[muted]Synthetic hosts only. Ctrl+C cancels active work.[/]";
    return new Rows(new TextBlock("[bold]Parallel patch run[/] [muted]· dry run, throttle 3[/]"),
        table, new TextBlock(summary)) { Gap = 1 };
}
if (snapshot) {
    Terminal.Write(Frame(step));
    return;
}
IReadOnlyList<RunResult<string, string>> results = await ConsoleRun.ForEachAsync(servers, async (server, cancel) => {
    await Task.Delay(350, cancel);
    if (server == "web-03") throw new InvalidOperationException("Synthetic restart check failed");
    return server + " preview passed";
}, new RunOptions { ThrottleLimit = 3, Timeout = TimeSpan.FromSeconds(3), Title = "Patch preview" });
// results retains each outcome for retry/reporting; no patch command runs here.
Terminal.Write(Frame(2));
