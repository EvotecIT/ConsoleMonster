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
LineChart chart = new LineChart { Unit = " ms" };
for (int i = 0; i < p50.Length; i++) { chart.Add("p50", p50[i]); }
for (int i = 0; i < p95.Length; i++) { chart.Add("p95", p95[i]); }
Grid samples = new Grid("Samples")
    .AddColumn("Time")
    .AddColumn("P50")
    .AddColumn("P95")
    .AddColumn("Errors");
for (int i = 0; i < p50.Length; i++)
{
    samples.AddRow(times[i], times[i], p50[i], p95[i], errors[i]);
}
ChartView latencyChart = new ChartView("Latency", chart) { LinkKey = "Sample" };
foreach (string time in times) { latencyChart.PointLabels.Add(time); }
DataGrid samplesGrid = new DataGrid("Samples", samples) { LinkKey = "Sample" };
IWidget latency = new Splitter(samplesGrid, latencyChart) { Key = "Panes", Ratio = 0.4 }.Fill();
Collapsible check = new Collapsible("Check", new Rows(
    new Toggle("Dashboards", true)
    {
        Label = "Error rate confirmed on the dashboard", OnText = "Done", OffText = "To do"
    },
    new Toggle("Deploys", true)
    {
        Label = "Deploy at 13:40 matches the first spike", OnText = "Done", OffText = "To do"
    }))
{
    Expanded = true
};
Collapsible mitigate = new Collapsible("Mitigate", new Rows(
    new Toggle("Rollback", false)
    {
        Label = "Roll back api-gateway to 4.12.1", OnText = "Done", OffText = "To do"
    },
    new Toggle("Drain", false)
    {
        Label = "Drain the eu-west-2 pool", OnText = "Done", OffText = "To do"
    }));
Collapsible verify = new Collapsible("Verify", new Toggle("P95Normal", false)
{
    Label = "p95 back under 150 ms for 15 minutes", OnText = "Done", OffText = "To do"
});
Outline steps = new Outline("Steps")
{
    Items =
    {
        new OutlineItem("Check") { Target = check },
        new OutlineItem("Mitigate") { Target = mitigate },
        new OutlineItem("Verify") { Target = verify }
    },
    SelectedIndex = 0
};
IWidget runbook = new Splitter(steps, new Accordion(check, mitigate, verify)) { Ratio = 0.22 }
    .Fill();
Rows layout = new Rows(
    new HeaderBar("Incident 4127 · api-gateway latency"),
    new Tabs("View").Add("Latency", latency).Add("Runbook", runbook));

// Behaviour: click or arrow through samples and the chart follows; drag the divider, scroll the
// wheel over the chart to zoom, click a legend entry to hide its series, right-click for the
// keys that work there, and click a tab, an outline entry or a toggle. Escape exits.
ConsoleApp.Run(layout);
