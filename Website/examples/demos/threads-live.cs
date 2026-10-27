using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// The same twelve servers, each worker updating its own lane through a handle; Console.WriteLine from any of them
// goes to the log panel.
string[] servers = Enumerable.Range(1, 12).Select(n => $"web{n:00}").ToArray();
string[] steps = { "copying", "stopping", "swapping", "starting" };
Dashboard dashboard = new DashboardBuilder("Patching the web farm")
    .Panel("Servers", new Lanes(), out Lanes lanes, headerRight: "12 at once")
    .Panel("Log", new LogRegion(), fill: true)
    .BuildDashboard();
foreach (string server in servers) {
    lanes.Add(server, server, steps.Length);
}

using (LiveSession session = new LiveSession(dashboard)) {
    await session.RunAsync(async (s, cancel) => {
        LanesHandle handle = lanes.Handle(s);
        try {
            await Parallel.ForEachAsync(
                servers,
                new ParallelOptions { MaxDegreeOfParallelism = 12, CancellationToken = cancel },
                async (server, token) => {
                    foreach (string step in steps) {
                        handle.Start(server, step);
                        await Task.Delay(Random.Shared.Next(40, 240), token);
                        handle.Advance(server, StatusKind.Pass);
                    }
                    Console.WriteLine(server + " done");
                    handle.Complete(server);
                });
        } catch (OperationCanceledException) {
            // Ctrl+C: the session has already stopped the view; the workers just stop.
        }
    });
}
