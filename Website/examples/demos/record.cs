using ConsoleMonster.Building;
using ConsoleMonster.Export;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Record a run, replay it and save it as a page. The dashboard is defined once: the run and the replay both draw
// into its widgets by name.
string[] servers = { "web01", "web02", "web03", "web04" };
Phases phases = new Phases().Set("Stage").Set("Swap").Set("Verify");
StatusList list = new StatusList("servers");
foreach (string server in servers) {
    list.Set(server, server, StatusKind.Pending);
}
Dashboard dashboard = new DashboardBuilder("[bold]Deploy[/]")
    .Subtitle("web farm · 4 servers")
    .Add(phases)
    .Panel("Servers", list)
    .Panel("Log", new LogRegion(), fill: true)
    .BuildDashboard();
string recording = Path.Combine(Path.GetTempPath(), "deploy.jsonl");

// 1. Run it, writing every update to the recording.
using (LiveSession session = new LiveSession(
    dashboard,
    new LiveSessionOptions { RecordPath = recording }
)) {
    session.Run(s => {
        PhasesHandle phase = phases.Handle(s);
        StatusListHandle status = list.Handle(s);
        foreach (string step in new[] { "Stage", "Swap", "Verify" }) {
            phase.Start(step);
            foreach (string server in servers) {
                status.Set(server, StatusKind.Running);
                status.Detail(server, step);
                Thread.Sleep(250);
                status.Set(server, StatusKind.Pass);
            }
            s.Post(new LiveEvent("log", "write", text: step + " done on 4 servers", status: "info"));
            phase.Complete(step, StatusKind.Pass);
        }
    });
}

// 2. Play it back at twice the speed, into the same dashboard.
using (StreamReader reader = File.OpenText(recording)) {
    LiveSession.Replay(dashboard, reader, new LiveSessionOptions(), speed: 2);
}

// 3. Save it as a page with a player.
using (StreamReader reader = File.OpenText(recording)) {
    File.WriteAllText(
        Path.Combine(
            Path.GetTempPath(),
            "deploy.html"
        ),
        Recording.ExportHtml(
            dashboard,
            reader,
            new RecordingExportOptions { Width = 100, Height = 16 }
        )
    );
}
