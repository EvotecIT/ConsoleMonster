using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// One assessment summary: phases, results as a stacked bar and the rule rate as a sparkline, in a panel.
double[] rate = { 18, 21, 19, 24, 27, 26, 31, 29, 34, 38, 36, 41, 39, 44, 47, 45, 50, 48, 53, 57 };
Terminal.Write(new Panel(
    new Rows()
        .Add(new Phases()
            .Set("Discover", StatusKind.Pass, default, "2.1s")
            .Set("Collect", StatusKind.Pass, default, "14s")
            .Set("Evaluate", StatusKind.Running, default, "64%"))
        .Add(new StackedBar()
            .Add("Passed", 412, ThemeToken.Pass)
            .Add("Warnings", 51, ThemeToken.Warn)
            .Add("Failed", 23, ThemeToken.Fail)
            .Add("Skipped", 14, ThemeToken.Skip))
        .Add(new Sparkline() { Label = "rules/s", Unit = "/s" }.AddRange(rate)),
    "[bold]Nightly checks[/]") { HeaderRight = "lab01.contoso.com" });
