// Animate a build checklist and overall progress without running a build.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// All values are synthetic. This example never connects to a host.
// Data: fictional inputs kept in memory.
string[] stages = { "Restore", "Compile", "Test", "Package" };

// Layout: construct the widgets shown on screen.
IWidget Frame(int tick)
{
    Rows checklist = Ui.Rows();
    for (int index = 0; index < stages.Length; index++)
    {
        string status = index < tick ? "pass" : index == tick ? "accent" : "muted";
        string label = index < tick ? "complete" : index == tick ? "running" : "pending";
        checklist.Add(Ui.Text()
            .Content("[" + status + "]" + stages[index] + "[/]  " + label)
            .IsMarkup());
    }

    return Ui.Rows().Gap(1).Content(c2 =>
    {
        c2.Text(c2Text => c2Text.Content("[bold]Build pipeline[/] [muted]· Orbit.Tools[/]")
            .IsMarkup());
        c2.Panel(cmPanel => cmPanel.Title("Release checklist").Content(c =>
        {
            c.Add(checklist);
        }));
        c2.Progress(cmProgress => cmProgress.Label("Overall").Value(tick).Total(4));
        c2.KeyValue(cmKeyValue => cmKeyValue.Add("Project", "Orbit.Tools")
            .Add("Version", "1.2.0")
            .Add("Artifacts", "./artifacts/")
            .Add("Publish", "manual approval"));
        c2.Text(c2Text => c2Text.Content("[muted]Ctrl+C stops the live run.[/]").IsMarkup());
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
