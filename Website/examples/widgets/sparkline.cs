using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

double[] rate = { 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57, 54, 59, 62, 60 };
Terminal.Write(new Rows()
    .WithGap(1)
    .Add(new Sparkline().WithLabel("rules/s").WithUnit("/s").AddRange(rate))
    .Add(new Sparkline().WithHeight(3).WithUnit("/s").AddRange(rate)));