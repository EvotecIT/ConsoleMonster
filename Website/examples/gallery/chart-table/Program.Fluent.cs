// Explore a load chart linked to the table of samples it was drawn from.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

double[] cpu =
{
    32, 35, 41, 38, 44, 52, 61, 58, 66, 72, 69, 63,
    57, 54, 60, 68, 75, 81, 77, 70, 62, 55, 49, 45
};
double[] memory =
{
    48, 49, 50, 51, 51, 52, 54, 55, 57, 58, 59, 60,
    61, 61, 62, 63, 65, 66, 67, 68, 68, 69, 70, 71
};
string[] times = new string[cpu.Length];
for (int i = 0; i < cpu.Length; i++) { times[i] = $"{9 + (i * 5 / 60):D2}:{i * 5 % 60:D2}"; }

// Layout: construct the widgets shown on screen.
LineChart chart = (LineChart)Ui.Chart().Type(ChartType.Line).Unit("%").Build();
for (int i = 0; i < cpu.Length; i++) { chart.Add("cpu", cpu[i]); }
for (int i = 0; i < memory.Length; i++) { chart.Add("memory", memory[i]); }
Grid samples = Ui.Grid().Title("Samples").AddColumn("Time").AddColumn("Cpu").AddColumn("Memory");
for (int i = 0; i < cpu.Length; i++) { samples.AddRow(times[i], times[i], cpu[i], memory[i]); }
Rows layout = Ui.Rows().Content(c =>
{
    c.DataGrid(grid => grid.Key("Samples").Grid(samples).Height(7).LinkKey("Sample"));
    c.ChartView(view => view.Key("Load").Chart(chart).LinkKey("Sample").PointLabels(times));
});

// Behaviour: Up/Down picks a sample and the crosshair follows; in the chart, Left/Right picks a
// point and the row follows, + and - zoom, 1 and 2 hide a series; Escape exits.
ConsoleApp.Run(layout);
