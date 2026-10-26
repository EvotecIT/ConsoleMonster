// Animate a build checklist and overall progress without running a build.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
string[] stages =
{
    "Restore",
    "Compile",
    "Test",
    "Package"
};
// Layout: construct the widgets shown on screen.
IWidget Frame(int tick)
{
    Rows checklist = new Rows();
    for (int index = 0; index < stages.Length; index++)
    {
        string status = index < tick ? "pass" : index == tick ? "accent" : "muted";
        string label = index < tick ? "complete" : index == tick ? "running" : "pending";
        checklist.Add(new TextBlock("[" + status + "]" + stages[index] + "[/]  " + label));
    }

    return new Rows(new TextBlock("[bold]Build pipeline[/] [muted]· Orbit.Tools[/]"), new Panel() { Title = "Release checklist", Content = checklist }, new ProgressBar("Overall") { Value = tick, Total = 4 }, new KeyValueList().Add("Project", "Orbit.Tools").Add("Version", "1.2.0").Add("Artifacts", "./artifacts/").Add("Publish", "manual approval"), new TextBlock("[muted]Ctrl+C stops the live run.[/]"))
    {
        Gap = 1
    };
}

// Behaviour: update each frame in a bounded live run.
RegionWidget screen = new RegionWidget(widgetId: "demo", content: Frame(0));
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
