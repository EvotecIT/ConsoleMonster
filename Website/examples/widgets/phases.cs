using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Phases()
    .Set("Discover", StatusKind.Pass, detail: "2.1s")
    .Set("Collect", StatusKind.Pass, detail: "14s")
    .Set("Evaluate", StatusKind.Running, detail: "64%")
    .Set("Report", StatusKind.Pending));