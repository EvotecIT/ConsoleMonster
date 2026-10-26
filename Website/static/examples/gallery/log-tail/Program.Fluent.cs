// Reveal a bounded stream of sample log events in a live region.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
string[] messages =
{
    "[muted]09:41:00[/] [accent]INFO[/]  Worker started",
    "[muted]09:41:01[/] [pass]INFO[/]  Loaded 12 queued jobs",
    "[muted]09:41:02[/] [warn]WARN[/]  Cache cold; using local fallback",
    "[muted]09:41:03[/] [pass]INFO[/]  Cache warm; queue draining",
    "[muted]09:41:04[/] [pass]INFO[/]  Batch complete; no jobs lost"
};
// Layout: construct the widgets shown on screen.
IWidget Frame(int tick)
{
    Rows lines = Ui.Rows();
    foreach (string line in messages.Take(tick + 1))
        lines.Add(Ui.Markup(line));
    return Ui.Rows().Gap(1).Content(c2 =>
    {
        c2.Markup("[bold]Application log[/] [muted]· worker events[/]");
        c2.Panel(cmPanel => cmPanel.Title("./logs/worker.log").Content(c =>
        {
            c.Add(lines);
        }));
        c2.Markup("[muted]" + (tick + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + " / 5 events · Ctrl+C stops the live run[/]");
    });
}

// Behaviour: update each frame in a bounded live run.
RegionWidget screen = Ui.Region("demo").Content(c =>
{
    c.Add(Frame(0));
});
using LiveSession session = screen.CreateSession();
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
