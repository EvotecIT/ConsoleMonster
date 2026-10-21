using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Phases()
    .Set("Discover", StatusKind.Pass, null, "2.1s")
    .Set("Collect", StatusKind.Pass, null, "14s")
    .Set("Evaluate", StatusKind.Running, null, "64%")
    .Set("Report", StatusKind.Pending));