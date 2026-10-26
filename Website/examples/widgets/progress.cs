using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Rows()
    .Add(new ProgressBar("Build") { Value = 64, Total = 100 })
    .Add(new ProgressBar("Tests") { Value = 412, Total = 530 })
    .Add(new ProgressBar("Docs") { Value = 100, Total = 100, Status = StatusKind.Pass })
    .Add(new ProgressBar("Upload") { Value = 3, Total = 8, Shape = BarShape.Block }));
