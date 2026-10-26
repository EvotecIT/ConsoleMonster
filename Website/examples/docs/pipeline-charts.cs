using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

var cpu = new PipelineChart()
{
    Sort = PipelineChartSort.Descending,
    MaxRows = 3,
    Unit = " s"
}.Add("web", 34).Add("jobs", 72).Add("api", 18).Add("helper", 5);
var latency = new PipelineChart()
{
    Type = ChartType.Histogram,
    Bins = 6,
    Unit = " ms"
};
foreach (double value in new double[] { 12, 18, 25, 33, 34, 38, 55, 68, 76, 84, 95, 96 }

)
{
    latency.Add(null, value);
}

var trends = new PipelineChart()
{
    Type = ChartType.Sparkline
};
double[] west = { 12, 18, 16, 22, 19, 25, 24, 28 };
double[] east = { 8, 9, 12, 10, 15, 14, 13, 18 };
for (int i = 0; i < west.Length; i++)
{
    trends.Add("api-west", west[i]).Add("api-east", east[i]);
}

Terminal.Write(new Rows(
    new TextBlock("[bold]CPU time - highest first[/]"),
    cpu,
    TextBlock.Plain(string.Empty),
    new TextBlock("[bold]Response-time distribution[/]"),
    latency,
    TextBlock.Plain(string.Empty),
    new TextBlock("[bold]Latency over time (ms) - one line per host[/]"),
    trends));
