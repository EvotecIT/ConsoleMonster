using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;
using static ConsoleMonster.Ui;

Server[] servers = {
    new("web01", "IIS", 8L * 1024 * 1024 * 1024, StatusKind.Pass),
    new("app01", "App", 16L * 1024 * 1024 * 1024, StatusKind.Warn),
    new("db01", "SQL", 64L * 1024 * 1024 * 1024, StatusKind.Fail)
};
double[] load = { 12, 18, 31, 64, 47, 38, 55, 70, 72, 41 };

// Ui factories: short functions build each widget, so the code has the shape of the screen.
Terminal.Write(Rows(
    Columns(
        Panel("Servers", Table(servers)
            .Column(s => s.Name, c => c.IsKey())
            .Column(s => s.Role)
            .Column(s => s.Ram, c => c.Presenter(CellPresenters.Bytes))
            .Column(s => s.Status)
            .Totals()),
        Panel("Summary", Rows(
            KeyValue(("Servers", 3), ("Failed", 1)),
            Sparkline(load).Label("CPU")))
    ).Gap(1),
    Text("[muted]Updated 10:02[/]")
).Gap(1));

record Server(string Name, string Role, long Ram, StatusKind Status);