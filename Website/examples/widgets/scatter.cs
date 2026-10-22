using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

LineChart chart = new LineChart().WithHeight(7).WithScatter().WithUnit("ms");
chart.AddSeries("latency", ThemeToken.Accent2);
for (int i = 0; i < 60; i++) {
    chart.Add("latency", 40 + ((i * 37) % 23) + (i % 7 == 0 ? 30 : 0));
}
Terminal.Write(chart);