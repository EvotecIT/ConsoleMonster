using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new Rows()
    .Add(new Spinner("Resolving DNS records").WithDetail("zone 4 of 9"))
    .Add(new Spinner("Schema snapshot").WithStatus(StatusKind.Pass).WithDetail("cached"))
    .Add(new Spinner("Certificate chain").WithStatus(StatusKind.Fail).WithDetail("expired 3 days ago")));