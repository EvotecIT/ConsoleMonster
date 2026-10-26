using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Server[] servers =
{
    new("web01", "IIS", 8L * 1024 * 1024 * 1024, StatusKind.Pass),
    new("app01", "App", 16L * 1024 * 1024 * 1024, StatusKind.Warn),
    new("db01", "SQL", 64L * 1024 * 1024 * 1024, StatusKind.Fail)
};
double[] load = { 12, 18, 31, 64, 47, 38, 55, 70, 72, 41 };

// Objects: each widget is a variable, set with properties and Add calls, then placed in the layout.
Table table = new Table
{
    ShowTotals = true,
    AutoTotals = true
};
table.AddColumn(new TableColumn("Name") { IsKey = true });
table.AddColumn(new TableColumn("Role"));
table.AddColumn(new TableColumn("Ram") { Presenter = CellPresenters.Bytes });
table.AddColumn(new TableColumn("Status"));
foreach (Server server in servers)
{
    table.AddValues(server.Name, server.Role, server.Ram, server.Status);
}

KeyValueList summary = new KeyValueList();
summary.AddValue("Servers", 3);
summary.AddValue("Failed", 1);
Sparkline cpu = new Sparkline
{
    Label = "CPU"
};
cpu.AddRange(load);
Terminal.Write(new Rows(
    new Columns(
        new Panel(table, "Servers"),
        new Panel(new Rows(summary, cpu), "Summary")) { Gap = 1 },
    new TextBlock("[muted]Updated 10:02[/]")) { Gap = 1 });

record Server(string Name, string Role, long Ram, StatusKind Status);
