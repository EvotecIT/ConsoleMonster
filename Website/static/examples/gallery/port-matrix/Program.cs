// See which application ports are healthy across a group of hosts.
using ConsoleMonster.Presenters;
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: synthetic observations; no DNS, ping, socket or network operation runs here.
string[] hosts = { "web-01", "web-02", "api-01", "sql-01", "cache-01" };
string[] ports = { "HTTPS 443", "SQL 5432", "Cache 6379" };
StatusKind[] states =
{
    StatusKind.Pass,
    StatusKind.Skip,
    StatusKind.Skip,
    StatusKind.Pass,
    StatusKind.Skip,
    StatusKind.Skip,
    StatusKind.Pass,
    StatusKind.Fail,
    StatusKind.Warn,
    StatusKind.Skip,
    StatusKind.Pass,
    StatusKind.Skip,
    StatusKind.Skip,
    StatusKind.Skip,
    StatusKind.Pass
};

// Layout: a compact map provides the overview; a table explains exceptions.
Table map = new Table() { Border = TableBorder.None };
map.AddColumn(" ");
foreach (string port in ports)
{
    map.AddColumn(port);
    map.Columns.Last().Presenter = CellPresenters.Status;
}
for (int row = 0; row < hosts.Length; row++)
    map.AddRow(hosts[row],
        TableCell.FromValue(states[row * ports.Length]),
        TableCell.FromValue(states[row * ports.Length + 1]),
        TableCell.FromValue(states[row * ports.Length + 2]));
ViewFacts legend = new ViewFacts()
    .Add(states.Count(state => state == StatusKind.Pass), "passed", ThemeToken.Pass)
    .Add(states.Count(state => state == StatusKind.Fail), "failed", ThemeToken.Fail)
    .Add(states.Count(state => state == StatusKind.Warn), "warning", ThemeToken.Warn)
    .Add(states.Count(state => state == StatusKind.Skip), "skipped", ThemeToken.Skip);
StatusList exceptions = new StatusList()
    .Add("api-01 → SQL 5432", StatusKind.Fail,
        "Connection refused · check listener and access policy")
    .Add("api-01 → Cache 6379", StatusKind.Warn, "180 ms · expected under 50 ms");
Rows report = new Rows(
    new Rule("[accent bold]Application connectivity[/] [muted]· core services[/]"),
    map,
    legend,
    new Panel() { Title = "Exceptions to investigate", Content = exceptions },
    TextBlock.FromMarkup("[muted]Skipped cells are ports that host does not need.[/]"))
{
    Gap = 1
};

// Behaviour: print the collected observations once; collectors belong before the layout.
Terminal.Write(report);
