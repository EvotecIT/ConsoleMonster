// Animate worker progress bars and a small session dashboard.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
int workerCount = 8;
// Layout: construct the widgets shown on screen.
IWidget Frame(int tick)
{
    Rows workers = new Rows();
    for (int worker = 1; worker <= workerCount; worker++)
    {
        string name = "worker-" + worker.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        workers.Add(new ProgressBar(name, name).Value(Math.Min(100, 12 + worker * 5 + tick * 17)).Total(100));
    }

    KeyValueList stats = new KeyValueList().Add("Updates", (800 + tick * 320).ToString(System.Globalization.CultureInfo.InvariantCulture)).Add("Queue", (24 - tick * 5).ToString(System.Globalization.CultureInfo.InvariantCulture)).Add("Workers", "8").Add("Dropped", "[pass]0[/]");
    Sparkline trend = new Sparkline().Label("updates/s");
    trend.AddRange(new double[] { 120, 180, 160, 240, 210, 220 + tick * 15 });
    return new Rows().Gap(1).Content(c4 =>
    {
        c4.Text("[bold]Worker stress dashboard[/] [muted]· worker pool[/]");
        c4.Add(new Columns().Content(c3 =>
        {
            c3.Add(new Panel().Title("Workers").Content(c =>
            {
                c.Add(workers);
            }));
            c3.Add(new Panel().Title("Session").Content(c2 =>
            {
                c2.Add(new Rows().Content(c =>
                {
                    c.Add(stats);
                    c.Add(trend);
                }));
            }));
        }));
        c4.Text("[muted]Ctrl+C stops the live run.[/]");
    });
}

// Behaviour: update each frame in a bounded live run.
RegionWidget screen = new RegionWidget(widgetId: "demo").Content(c =>
{
    c.Add(Frame(0));
});
using LiveSession session = new LiveSession(screen);
try
{
    session.Run(live =>
    {
        for (int tick = 0; tick <= 4 && !live.Cancellation.IsCancellationRequested; tick++)
        {
            screen.Handle(live).Set(Frame(tick));
            live.Delay(TimeSpan.FromMilliseconds(650));
        }
    });
}
catch (OperationCanceledException cancelled)when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}
