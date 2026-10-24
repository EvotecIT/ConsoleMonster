using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 4) throw new ArgumentOutOfRangeException(nameof(step));
// All values are synthetic. This example never connects to a host.

string[] messages = {
    "[muted]09:41:00[/] [accent]INFO[/]  Worker started",
    "[muted]09:41:01[/] [pass]INFO[/]  Loaded 12 queued jobs",
    "[muted]09:41:02[/] [warn]WARN[/]  Cache cold; using local fallback",
    "[muted]09:41:03[/] [pass]INFO[/]  Cache warm; queue draining",
    "[muted]09:41:04[/] [pass]INFO[/]  Batch complete; no jobs lost"
};
IWidget Frame(int tick) {
    Rows lines = new Rows();
    foreach (string line in messages.Take(tick + 1)) lines.Add(new TextBlock(line));
    return new Rows(new TextBlock("[bold]Application log[/] [muted]· synthetic events[/]"),
        new Panel(lines, "./logs/worker.log"), new TextBlock("[muted]" +
            (tick + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) +
            " / 5 events · bounded sample · Ctrl+C stops the live run[/]")) { Gap = 1 };
}

if (snapshot) {
    Terminal.Write(Frame(step));
    return;
}
Slot screen = new Slot("demo", Frame(0));
using LiveSession session = new LiveSession(screen);
session.Run(live => {
    for (int tick = 0; tick <= 4 && !live.Cancellation.IsCancellationRequested; tick++) {
        screen.Handle(live).Set(Frame(tick));
        if (live.Cancellation.WaitHandle.WaitOne(650)) break;
    }
});
