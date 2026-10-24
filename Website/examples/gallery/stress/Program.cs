// Animate worker progress bars and a small session dashboard.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// All values are synthetic. This example never connects to a host.

// Data: fictional inputs kept in memory.
int workerCount = 8;

// Run options: redirected input selects a deterministic snapshot.
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 4) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: construct the widgets shown on screen.
IWidget Frame(int tick) {
    Rows workers = new Rows();
    for (int worker = 1; worker <= workerCount; worker++) {
        string name = "worker-" + worker.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        workers.Add(new ProgressBar(name, name)
            .WithValue(Math.Min(100, 12 + worker * 5 + tick * 17)).WithTotal(100));
    }
    KeyValueList stats = new KeyValueList()
        .Add("Updates", (800 + tick * 320).ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Add("Queue", (24 - tick * 5).ToString(System.Globalization.CultureInfo.InvariantCulture))
        .Add("Workers", "8").Add("Dropped", "[pass]0[/]");
    Sparkline trend = new Sparkline { Label = "updates/s" };
    trend.AddRange(new double[] { 120,180,160,240,210,220 + tick * 15 });
    return new Rows(new TextBlock("[bold]Worker stress dashboard[/] [muted]· simulated load[/]"),
        new Columns(new Panel(workers, "Workers"), new Panel(new Rows(stats, trend), "Session")),
        new TextBlock("[muted]Fixed sample values; no benchmark measurements. Ctrl+C stops the live run.[/]")) { Gap = 1 };
}

// Live updates: print one state headlessly, or run the bounded simulation.
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
