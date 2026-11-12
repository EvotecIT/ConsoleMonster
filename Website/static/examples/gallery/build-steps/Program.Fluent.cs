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
        checklist.Add(Ui.Markup("[" + status + "]" + stages[index] + "[/]  " + label));
    }

    return Ui.Rows().Gap(1).Content(rowsContent =>
    {
        rowsContent.Markup("[accent bold]Build pipeline[/] [muted]· Orbit.Tools[/]");
        rowsContent.Panel(p => p.Title("Release checklist").Content(panelContent =>
        {
            panelContent.Add(checklist);
        }));
        rowsContent.ProgressBar(progressBar => progressBar.Label("Overall").Value(tick).Total(4));
        rowsContent.KeyValue(kv => kv.Add("Project", "Orbit.Tools")
            .Add("Version", "1.2.0")
            .Add("Artifacts", "./artifacts/")
            .Add("Publish", "manual approval"));
        rowsContent.Markup("[muted]Ctrl+C stops the live run.[/]");
    });
}

// Behaviour: update each frame in a bounded live run.
RegionWidget screen = Ui.Region("demo").Content(regionWidgetContent =>
{
    regionWidgetContent.Add(Frame(0));
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
