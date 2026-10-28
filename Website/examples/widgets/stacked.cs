using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new StackedBar()
    .Add("Passed", 412, ThemeToken.Pass)
    .Add("Warnings", 51, ThemeToken.Warn)
    .Add("Failed", 23, ThemeToken.Fail)
    .Add("Skipped", 14, ThemeToken.Skip));
