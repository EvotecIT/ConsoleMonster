// An incident workbench: samples linked to their chart, a resizable split and a runbook.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

double[] p50 =
{
    41, 43, 40, 44, 42, 45, 47, 46, 52, 88, 131, 164,
    158, 120, 84, 61, 52, 48, 46, 44, 45, 43, 42, 44
};
double[] p95 =
{
    96, 98, 95, 101, 99, 104, 108, 112, 140, 262, 410, 486,
    472, 355, 230, 160, 128, 117, 110, 104, 103, 100, 98, 101
};
int[] errors = { 0, 0, 0, 1, 0, 0, 1, 0, 3, 18, 47, 62, 58, 31, 12, 4, 1, 0, 0, 1, 0, 0, 0, 0 };
string[] times = new string[p50.Length];
for (int i = 0; i < p50.Length; i++) { times[i] = $"{13 + (i * 5 / 60):D2}:{i * 5 % 60:D2}"; }

// Layout: construct the widgets shown on screen.
LineChart chart = (LineChart)Ui.Chart().Type(ChartType.Line).Unit(" ms").Build();
for (int i = 0; i < p50.Length; i++) { chart.Add("p50", p50[i]); }
for (int i = 0; i < p95.Length; i++) { chart.Add("p95", p95[i]); }
Grid samples = Ui.Grid()
    .Title("Samples")
    .AddColumn("Time")
    .AddColumn("P50")
    .AddColumn("P95")
    .AddColumn("Errors");
for (int i = 0; i < p50.Length; i++)
{
    samples.AddRow(times[i], times[i], p50[i], p95[i], errors[i]);
}
IWidget latency = Ui.Splitter()
    .First(c => c.DataGrid(grid => grid.Key("Samples").Grid(samples).LinkKey("Sample")))
    .Second(c => c.ChartView(view => view.Key("Latency")
        .Chart(chart)
        .LinkKey("Sample")
        .PointLabels(times)))
    .Key("Panes")
    .Ratio(0.4)
    .Fill();
Collapsible check = ((Collapsible)Ui.Collapsible().Key("Check").Content(Ui.Rows().Content(c =>
{
    c.Toggle(toggle => toggle.Key("Dashboards")
        .Value(true)
        .Label("Error rate confirmed on the dashboard")
        .OnText("Done")
        .OffText("To do"));
    c.Toggle(toggle => toggle.Key("Deploys")
        .Value(true)
        .Label("Deploy at 13:40 matches the first spike")
        .OnText("Done")
        .OffText("To do"));
})).Expanded().Build());
Collapsible mitigate = ((Collapsible)Ui.Collapsible().Key("Mitigate").Content(Ui.Rows().Content(c =>
{
    c.Toggle(toggle => toggle.Key("Rollback")
        .Value(false)
        .Label("Roll back api-gateway to 4.12.1")
        .OnText("Done")
        .OffText("To do"));
    c.Toggle(toggle => toggle.Key("Drain")
        .Value(false)
        .Label("Drain the eu-west-2 pool")
        .OnText("Done")
        .OffText("To do"));
})).Build());
Collapsible verify = ((Collapsible)Ui.Collapsible()
    .Key("Verify")
    .Content(((Toggle)Ui.Toggle()
        .Key("P95Normal")
        .Value(false)
        .Label("p95 back under 150 ms for 15 minutes")
        .OnText("Done")
        .OffText("To do")
        .Build()))
    .Build());
Outline steps = ((Outline)Ui.Outline()
    .Key("Steps")
    .Build()).AddItem(new OutlineItem("Check").Target(check))
    .AddItem(new OutlineItem("Mitigate").Target(mitigate))
    .AddItem(new OutlineItem("Verify").Target(verify))
    .SelectedIndex(0);
IWidget runbook = Ui.Splitter().First(steps).Second(Ui.Accordion().Content(c =>
{
    c.Add(check);
    c.Add(mitigate);
    c.Add(verify);
})).Ratio(0.22).Fill();
Rows layout = Ui.Rows().Content(c =>
{
    c.HeaderBar(headerBar => headerBar.Title("Incident 4127 · api-gateway latency"));
    c.Tabs(tabs => tabs.Key("View").Add("Latency", latency).Add("Runbook", runbook));
});

// Behaviour: click or arrow through samples and the chart follows; drag the divider, scroll the
// wheel over the chart to zoom, click a legend entry to hide its series, right-click for the
// keys that work there, and click a tab, an outline entry or a toggle. Escape exits.
ConsoleApp.Run(layout);
