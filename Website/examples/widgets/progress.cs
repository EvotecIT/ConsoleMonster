using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Rows()
    .Add(new ProgressBar("Build").WithValue(64).WithTotal(100))
    .Add(new ProgressBar("Tests").WithValue(412).WithTotal(530))
    .Add(new ProgressBar("Docs").WithValue(100).WithTotal(100).WithStatus(StatusKind.Pass))
    .Add(new ProgressBar("Upload").WithValue(3).WithTotal(8).WithShape(BarShape.Block)));