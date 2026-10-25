using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

LineChart chart = new LineChart()
{
    Height = 7,
    Scatter = true,
    Unit = "ms"
};
chart.AddSeries("latency", ThemeToken.Accent2);
for (int i = 0; i < 60; i++)
{
    chart.Add("latency", 40 + ((i * 37) % 23) + (i % 7 == 0 ? 30 : 0));
}

Terminal.Write(chart);
