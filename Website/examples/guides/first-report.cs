using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// 1. Data: invented results for four servers; nothing is queried or changed.
var servers = new Server[]
{
    new("fs01.contoso.example", "File", 62, "Pass"),
    new("fs02.contoso.example", "File", 81, "Warn"),
    new("sql01.contoso.example", "Database", 93, "Fail"),
    new("web01.contoso.example", "Web", 40, "Pass")
};
int passed = servers.Count(server => server.Status == "Pass");
int warnings = servers.Count(server => server.Status == "Warn");
int failed = servers.Count(server => server.Status == "Fail");

// 2. Layout: a title, a facts line, the table and the disk bars, one below another.
var table = Table.For(servers)
    .Column(row => row.Name)
    .Column(row => row.Role)
    .Column(row => row.Disk)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);

BarChart disk = new BarChart
{
    Unit = "%",
    Maximum = 100,
    Warning = 80,
    Critical = 90
};
foreach (var server in servers)
    disk.Add(server.Name, server.Disk);

Rows report = new Rows(
    new Rule("[accent bold]Nightly server report[/] [muted]· 2026-11-09[/]"),
    new ViewFacts().Add(passed, "healthy", ThemeToken.Pass)
        .Add(warnings, "warning", ThemeToken.Warn)
        .Add(failed, "failed", ThemeToken.Fail)
        .Add(servers.Length, "servers"),
    table,
    new Panel(disk, "Disk used") { HeaderRight = "% of capacity" })
{
    Gap = 1
};

// 3. Print it once.
Terminal.Write(report);

record Server(string Name, string Role, int Disk, string Status);
