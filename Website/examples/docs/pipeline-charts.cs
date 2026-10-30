using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

var cpuOptions = new ChartOptions { Unit = " s", Signed = true };
(string Name, double Value)[] samples = { ("web", 34.0), ("jobs", 72.0),
    ("api", 18.0), ("helper", 5.0) };
foreach (var sample in samples.OrderByDescending(sample => sample.Value).Take(3))
    cpuOptions.AddItem(sample.Name, sample.Value);
IWidget cpu = Chart.Create(cpuOptions);
IWidget latency = Chart.Create(new ChartOptions
{
    Type = ChartType.Histogram,
    Edges = new double[] { 12, 26, 40, 54, 68, 82, 96 },
    Unit = " ms",
    Values = new double[] { 12, 18, 25, 33, 34, 38, 55, 68, 76, 84, 95, 96 }
});
double[] west = { 12, 18, 16, 22, 19, 25, 24, 28 };
double[] east = { 8, 9, 12, 10, 15, 14, 13, 18 };
var trends = new Rows(
    Chart.Create(new ChartOptions
    {
        Type = ChartType.Sparkline,
        Title = "api-west",
        Values = west
    }),
    Chart.Create(new ChartOptions
    {
        Type = ChartType.Sparkline,
        Title = "api-east",
        Values = east
    }));

Terminal.Write(new Rows(
    new TextBlock("[bold]CPU time - highest first[/]"),
    cpu,
    TextBlock.Plain(string.Empty),
    new TextBlock("[bold]Response-time distribution[/]"),
    latency,
    TextBlock.Plain(string.Empty),
    new TextBlock("[bold]Latency over time (ms) - one line per host[/]"),
    trends));
