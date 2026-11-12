// Stress test: thirty-two workers, fast handles, session telemetry and captured tool output.
using System.Globalization;
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: synthetic telemetry samples; no machine counters, services or files are read.
const int Workers = 32;
string[] actions = { "parsing", "resolving", "evaluating", "scoring", "writing" };
double[] sampleRates =
{
    18000, 25919, 22838, 19757, 27676, 24595, 21514, 18433, 26352, 23271, 20190, 28109, 25028,
    21947, 18866, 26785, 23704, 20623, 28542, 25461, 22380, 19299, 27218, 24137, 21056, 28975,
    25894, 22813, 19732, 27651, 24570, 21489, 18408, 26327, 23246, 20165, 28084, 25003, 21922,
    18841, 26760, 23679, 20598, 28517, 25436, 22355, 19274, 27193
};
double[] sampleCpu =
{
    42, 59, 76, 93, 110, 53, 70, 87, 104, 47, 64, 81, 98, 115, 58, 75, 92, 109, 52, 69, 86, 103, 46,
    63, 80, 97, 114, 57, 74, 91, 108, 51, 68, 85, 102, 45, 62, 79, 96, 113, 56, 73, 90, 107, 50, 67,
    84, 101
};
string[] messages =
{
    "cache miss for key 412", "retrying request 28 (attempt 2)", "connection pool at 76% capacity",
    "GC pause 8 ms", "flushed 960 records"
};
string Number(double value) => value.ToString("#,0", CultureInfo.InvariantCulture);

// Layout: retain every worker row, the session counters, update sparkline, CPU chart and captured output.
IWidget Frame(int tick)
{
    Lanes lanes = Ui.Lanes("workers");
    for (int id = 0; id < Workers; id++)
    {
        Lane lane = lanes.Add("w" + id,
            "worker " + (id + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
        if (tick > 0)
        {
            lane.Value(tick == 6 ? 100000 : tick * 3200 + id * 97 % 400);
            lane.Failed((int)lane.Value / 97);
            lane.Warned((int)lane.Value / 17);
            lane.Passed((int)lane.Value - lane.Failed - lane.Warned)
                .Status(tick == 6 ? StatusKind.Pass : StatusKind.Running);
            lane.Detail(actions[(id + tick) % actions.Length] + " item " + Number(lane.Value));
        }
    }

    int samples = tick * 8;
    long posted = tick * 128000L;
    KeyValueList stats = Ui.KeyValue("stats")
        .Add("Updates posted", Number(posted))
        .Add("Updates applied", Number(posted))
        .Add("Update rate", samples == 0 ? "-" : Number(sampleRates[samples - 1]) + "/s")
        .Add("Frames", Number(tick * 15))
        .Add("Frame rate", tick == 0 ? "-" : "15.0 fps")
        .Add("CPU", samples == 0 ? "-" : Number(sampleCpu[samples - 1]) + "%")
        .Add("CPU share",
            samples == 0
                ? "-"
                : (sampleCpu[samples - 1] / 8).ToString("0.0",
                    CultureInfo.InvariantCulture) + "% of 8 cores")
        .Add("Stray writes", (tick * 4).ToString(CultureInfo.InvariantCulture))
        .Add("Dropped / errors", "0 / 0");
    Sparkline rate = Ui.Sparkline("rate").Height(3).Unit("/s");
    rate.AddRange(sampleRates.Take(samples));
    LineChart cpu = Ui.LineChart("cpu").Height(6).Minimum(0).Unit("%");
    cpu.AddSeries("cpu", ThemeToken.Accent2);
    foreach (double value in sampleCpu.Take(samples))
        cpu.Add("cpu", value);
    LogRegion output = Ui.LogRegion().PreferredHeight(12).ShowElapsed(false).Wrap();
    for (int i = 0; i < tick * 4; i++)
        output.Write("stray-" + i % 4 + "-" + i + ": FakeLib: " + messages[i % 5],
            i % 7 == 0 ? "err" : "info");
    Columns body = Ui.Columns().Add(Ui.Panel().Title("Workers").Content(panelContent =>
    {
        panelContent.Add(lanes);
    }), LayoutSize.Fraction(2.2)).Add(Ui.Rows().Content(rowsContent2 =>
    {
        rowsContent2.Panel(p => p.Title("Session").Content(panelContent =>
        {
            panelContent.Rows(r => r.Content(rowsContent =>
            {
                rowsContent.Add(stats);
                rowsContent.Rule("updates/s");
                rowsContent.Add(rate);
                rowsContent.Rule("CPU % (100% = one core)");
                rowsContent.Add(cpu);
            }));
        }));
    }).Add(Ui.Panel().Title("Captured output").Content(panelContent =>
    {
        panelContent.Add(output);
    }), LayoutSize.Fraction()));
    Dashboard dashboard = Ui.Dashboard()
        .Content(dashboardContent =>
        {
            dashboardContent.Rows(r => r.Add(body, LayoutSize.Fraction())
                .Add(Ui.Facts().Add(32, "workers").Add(15, "frames/s").Add("Ctrl+C stop")));
        })
        .Title("[accent bold]Stress test[/]")
        .Subtitle("32 workers · captured tool output · 6s")
        .ShowElapsed(false);
    dashboard.FooterHints.Clear();
    return dashboard;
}

// Behaviour: show seven samples through the ordinary live session.
RegionWidget screen = Ui.Region("stress").Content(regionWidgetContent =>
{
    regionWidgetContent.Add(Frame(0));
});
using LiveSession session = screen.CreateSession();
try
{
    session.Run(live =>
    {
        for (int tick = 0; tick <= 6; tick++)
        {
            live.Cancellation.ThrowIfCancellationRequested();
            screen.Handle(live).Set(Frame(tick));
            live.Delay(TimeSpan.FromSeconds(1));
        }
    });
}
catch (OperationCanceledException cancelled)
    when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}
