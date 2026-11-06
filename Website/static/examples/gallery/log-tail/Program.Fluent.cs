// Reveal a bounded stream of sample log events, then search the run with the find bar.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
(string Time, string Level, string Message)[] events =
{
    ("09:41:00", "info", "Worker started"),
    ("09:41:01", "info", "Loaded 12 queued jobs"),
    ("09:41:02", "warn", "Cache cold; using local fallback"),
    ("09:41:03", "info", "Cache warm; queue draining"),
    ("09:41:04", "pass", "Batch complete; no jobs lost")
};
Dictionary<string, string> labels = new Dictionary<string, string>
{
    ["info"] = "INFO",
    ["warn"] = "WARN",
    ["pass"] = "OK"
};
Dictionary<string, string> roles = new Dictionary<string, string>
{
    ["info"] = "accent",
    ["warn"] = "warn",
    ["pass"] = "pass"
};

// Layout: construct the widgets shown on screen.
IWidget Frame(int tick)
{
    Rows lines = Ui.Rows();
    foreach ((string time, string level, string message)in events.Take(tick + 1))
        lines.Add(Ui.Markup("[muted]" + time + "[/] [" + roles[level] + "]" + labels[level]
            .PadRight(4) + "[/]  " + message));
    return Ui.Rows().Gap(1).Content(c2 =>
    {
        c2.Markup("[bold]Application log[/] [muted]· worker events[/]");
        c2.Panel(p => p.Title("./logs/worker.log").Content(c =>
        {
            c.Add(lines);
        }));
        c2.Markup("[muted]" + (tick + 1).ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " / 5 events · Ctrl+C stops the live run[/]");
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
catch (OperationCanceledException cancelled)
    when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}

// Then read the run back: / finds text, n moves to the next match, Escape quits.
LogViewport log = Ui.LogViewport("worker.log").ShowElapsed(false).Height(7);
foreach ((_, string level, string message) in events)
    log.Write(message, level);
ConsoleApp.Run(Ui.Rows().Content(c =>
{
    c.Markup("[bold]Application log[/] [muted]· read the run back[/]");
    c.Add(log);
}));
