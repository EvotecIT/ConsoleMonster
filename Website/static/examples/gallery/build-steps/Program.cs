// Animate a build checklist and overall progress without running a build.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Widgets;

// All values are synthetic. This example never connects to a host.

// Data: fictional inputs kept in memory.
string[] stages = { "Restore", "Compile", "Test", "Package" };

// Layout: construct the widgets shown on screen.
IWidget Frame(int tick) {
    Rows checklist = new Rows();
    for (int index = 0; index < stages.Length; index++) {
        string status = index < tick ? "pass" : index == tick ? "accent" : "muted";
        string label = index < tick ? "complete" : index == tick ? "running" : "pending";
        checklist.Add(new TextBlock("[" + status + "]" + stages[index] + "[/]  " + label));
    }
    return new Rows(new TextBlock("[bold]Build pipeline[/] [muted]· Orbit.Tools[/]"),
        new Panel(checklist, "Release checklist"), new ProgressBar("Overall").WithValue(tick).WithTotal(4),
        new KeyValueList().Add("Project", "Orbit.Tools").Add("Version", "1.2.0")
            .Add("Artifacts", "./artifacts/").Add("Publish", "manual approval"),
        new TextBlock("[muted]Ctrl+C stops the live run.[/]")) { Gap = 1 };
}

// Behaviour: update each frame in a bounded live run.
Slot screen = new Slot("demo", Frame(0));
using LiveSession session = new LiveSession(screen);
session.Run(live => {
    for (int tick = 0; tick <= 4 && !live.Cancellation.IsCancellationRequested; tick++) {
        screen.Handle(live).Set(Frame(tick));
        live.Delay(TimeSpan.FromMilliseconds(650));
    }
});
