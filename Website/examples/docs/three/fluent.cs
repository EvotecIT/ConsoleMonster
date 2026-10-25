using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Server[] servers = {
    new("web01", "IIS", 8L * 1024 * 1024 * 1024, StatusKind.Pass),
    new("app01", "App", 16L * 1024 * 1024 * 1024, StatusKind.Warn),
    new("db01", "SQL", 64L * 1024 * 1024 * 1024, StatusKind.Fail)
};
double[] load = { 12, 18, 31, 64, 47, 38, 55, 70, 72, 41 };

// Fluent: each widget is created and configured in one expression, so the layout reads top to bottom.
Terminal.Write(new Rows()
    .Gap(1)
    .Add(new Columns()
        .Gap(1)
        .Add(new Panel(Table.For(servers)
            .Column(s => s.Name, c => c.IsKey())
            .Column(s => s.Role)
            .Column(s => s.Ram, c => c.Presenter(CellPresenters.Bytes))
            .Column(s => s.Status)
            .Totals(), "Servers"))
        .Add(new Panel(new Rows()
            .Add(new KeyValueList().AddValue("Servers", 3).AddValue("Failed", 1))
            .Add(new Sparkline().Label("CPU").AddRange(load)), "Summary")))
    .Add(new TextBlock("[muted]Updated 10:02[/]")));

record Server(string Name, string Role, long Ram, StatusKind Status);