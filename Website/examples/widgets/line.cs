using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

LineChart chart = new LineChart().Height(7);
chart.AddSeries("completed", ThemeToken.Accent);
chart.AddSeries("failed", ThemeToken.Fail);
for (int i = 0; i < 48; i++) {
    chart.Add("completed", 20 + (9 * Math.Sin(i / 7.0)) + (i % 3));
    chart.Add("failed", 3 + (2 * Math.Cos(i / 5.0)));
}
Terminal.Write(chart);