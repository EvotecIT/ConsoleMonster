using ConsoleMonster.Interactive;
using ConsoleMonster.Widgets;

string[] times = { "09:00", "09:05", "09:10", "09:15", "09:20", "09:25", "09:30", "09:35" };
double[] cpu = { 22, 25, 31, 64, 88, 71, 40, 28 };

LineChart chart = new LineChart { Unit = "%" };
foreach (double value in cpu) { chart.Add("cpu", value); }
ChartView view = new ChartView("Cpu", chart) { LinkKey = "Sample", Height = 9 };
foreach (string time in times) { view.PointLabels.Add(time); }
Grid samples = new Grid("Samples").AddColumn("Time").AddColumn("Cpu");
for (int i = 0; i < times.Length; i++) { samples.AddRow(times[i], times[i], cpu[i]); }
ConsoleApp.Run(new Splitter(new DataGrid("Samples", samples) { LinkKey = "Sample" }, view)
{
    Ratio = 0.3
});
