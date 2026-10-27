using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Phases()
    .Set("Discover", StatusKind.Pass, default, "2.1s")
    .Set("Collect", StatusKind.Pass, default, "14s")
    .Set("Evaluate", StatusKind.Running, default, "64%")
    .Set("Report", StatusKind.Pending));
