using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Sparkline load = new Sparkline()
{
    Height = 3,
    ShowLatest = false
}.AddRange(new double[] { 12, 14, 13, 18, 22, 26, 25, 31, 36, 34, 40, 44 }).AddRange(new double[] { 41, 47, 52, 49, 55, 61, 58, 64, 70, 66, 73, 79 });
Align badge = new Align(new TextBlock("[pass] LIVE [/]"), HorizontalAlignment.Right);
Terminal.Write(new Panel(new Stack(load, badge), "Stack") { HeaderRight = "badge over a chart" });
