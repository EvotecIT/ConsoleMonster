using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Three servers deployed in parallel. Handles post updates from any thread; Console.WriteLine from any of them lands
// in the log panel instead of writing over the screen.
Dashboard dashboard = new DashboardBuilder("Deploy")
    .Panel("Servers", new Lanes(), out Lanes lanes, false, "3 workers")
    .Panel("Log", new LogRegion(), true)
    .BuildDashboard();
string[] steps = { "copying files", "stopping the site", "swapping folders", "starting the site" };
foreach (string server in new[] { "SRV1", "SRV2", "SRV3" }

)
{
    lanes.Add(server, server, steps.Length);
}

using (LiveSession session = new LiveSession(dashboard))
{
    session.Run(s =>
    {
        LanesHandle servers = lanes.Handle(s);
        Parallel.ForEach(new[] { "SRV1", "SRV2", "SRV3" }, server =>
        {
            Random random = new Random(server[^1]);
            servers.Start(server);
            foreach (string step in steps)
            {
                servers.Start(server, step);
                Thread.Sleep(400 + random.Next(500));
                servers.Advance(server, StatusKind.Pass);
            }

            Console.WriteLine(server + " done");
            servers.Complete(server);
        });
    });
}
