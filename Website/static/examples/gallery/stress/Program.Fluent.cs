// Stress test: thirty-two workers, fast handles, session telemetry and captured tool output.
using System.Globalization;
using ConsoleMonster.Layout;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Data: synthetic telemetry samples; no machine counters, services or files are read.
const int Workers = 32;
string[] actions =
{
    "parsing",
    "resolving",
    "evaluating",
    "scoring",
    "writing"
};
double[] sampleRates =
{
    18000,
    25919,
    22838,
    19757,
    27676,
    24595,
    21514,
    18433,
    26352,
    23271,
    20190,
    28109,
    25028,
    21947,
    18866,
    26785,
    23704,
    20623,
    28542,
    25461,
    22380,
    19299,
    27218,
    24137,
    21056,
    28975,
    25894,
    22813,
    19732,
    27651,
    24570,
    21489,
    18408,
    26327,
    23246,
    20165,
    28084,
    25003,
    21922,
    18841,
    26760,
    23679,
    20598,
    28517,
    25436,
    22355,
    19274,
    27193,
};
double[] sampleCpu =
{
    42,
    59,
    76,
    93,
    110,
    53,
    70,
    87,
    104,
    47,
    64,
    81,
    98,
    115,
    58,
    75,
    92,
    109,
    52,
    69,
    86,
    103,
    46,
    63,
    80,
    97,
    114,
    57,
    74,
    91,
    108,
    51,
    68,
    85,
    102,
    45,
    62,
    79,
    96,
    113,
    56,
    73,
    90,
    107,
    50,
    67,
    84,
    101,
};
string[] messages =
{
    "cache miss for key 412",
    "retrying request 28 (attempt 2)",
    "connection pool at 76% capacity",
    "GC pause 8 ms",
    "flushed 960 records"
};
string Number(double value) => value.ToString("#,0", CultureInfo.InvariantCulture);
// Layout: retain every worker row, the session counters, update sparkline, CPU chart and captured output.
IWidget Frame(int tick)
{
    Lanes lanes = Ui.Lanes("workers");
    for (int id = 0; id < Workers; id++)
    {
        Lane lane = lanes.Add("w" + id, "worker " + (id + 1).ToString("00", CultureInfo.InvariantCulture), 100000);
        if (tick > 0)
        {
            lane.Value(tick == 6 ? 100000 : tick * 3200 + id * 97 % 400);
            lane.Failed((int)lane.Value / 97);
            lane.Warned((int)lane.Value / 17);
            lane.Passed((int)lane.Value - lane.Failed - lane.Warned);
            lane.Status(tick == 6 ? StatusKind.Pass : StatusKind.Running);
            lane.Detail(actions[(id + tick) % actions.Length] + " item " + Number(lane.Value));
        }
    }

    int samples = tick * 8;
    long posted = tick * 128000L;
    KeyValueList stats = Ui.KeyValue("stats").Add("Updates posted", Number(posted)).Add("Updates applied", Number(posted)).Add("Update rate", samples == 0 ? "-" : Number(sampleRates[samples - 1]) + "/s").Add("Frames", Number(tick * 15)).Add("Frame rate", tick == 0 ? "-" : "15.0 fps").Add("CPU", samples == 0 ? "-" : Number(sampleCpu[samples - 1]) + "%").Add("CPU share", samples == 0 ? "-" : (sampleCpu[samples - 1] / 8).ToString("0.0", CultureInfo.InvariantCulture) + "% of 8 cores").Add("Stray writes", (tick * 4).ToString(CultureInfo.InvariantCulture)).Add("Dropped / errors", "0 / 0");
    Sparkline rate = Ui.Chart().Type(ConsoleMonster.Widgets.ChartType.Sparkline).WidgetId("rate").Height(3).Unit("/s").Build<Sparkline>();
    rate.AddRange(sampleRates.Take(samples));
    LineChart cpu = Ui.Chart().Type(ConsoleMonster.Widgets.ChartType.Line).WidgetId("cpu").Height(6).Minimum(0).Unit("%").Build<LineChart>();
    cpu.AddSeries("cpu", ThemeToken.Accent2);
    foreach (double value in sampleCpu.Take(samples))
        cpu.Add("cpu", value);
    LogRegion output = Ui.LogRegion().PreferredHeight(6).ShowElapsed(false);
    for (int i = 0; i < tick * 4; i++)
        output.Write("stray-" + i % 4 + "-" + i + ": FakeLib: " + messages[i % 5], i % 7 == 0 ? "err" : "info");
    Rows body = Ui.Rows().Content(c3 =>
    {
        c3.Columns(cmColumns => cmColumns.Add(Ui.Panel().Title("Workers").Content(c =>
        {
            c.Add(lanes);
        }), LayoutSize.Fraction(2.2)).Add(Ui.Panel().Title("Session").Content(c2 =>
        {
            c2.Rows(cmRows => cmRows.Content(c =>
            {
                c.Add(stats);
                c.Rule("updates/s");
                c.Add(rate);
                c.Rule("CPU % (100% = one core)");
                c.Add(cpu);
            }));
        })));
    }).Add(Ui.Panel().Title("Captured output").Content(c =>
    {
        c.Add(output);
    }), LayoutSize.Fraction());
    return Ui.Dashboard().Content(c =>
    {
        c.Add(body);
    }).Title("[bold]Stress test[/]").Subtitle("32 workers · captured tool output · 6s").ShowElapsed(false).Footer("[muted]32 workers    15 frames/s    Ctrl+C stop[/]");
}

// Behaviour: show seven samples through the ordinary live session.
RegionWidget screen = Ui.Region("stress").Content(c =>
{
    c.Add(Frame(0));
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
catch (OperationCanceledException cancelled)when (cancelled.CancellationToken == session.Cancellation)
{
    // Ctrl+C is a normal way to leave the demo, after the session restores the terminal.
    return;
}
