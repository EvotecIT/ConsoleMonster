// See which application ports are healthy across a group of hosts.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: synthetic observations; no DNS, ping, socket or network operation runs here.
string[] hosts =
{
    "web-01",
    "web-02",
    "api-01",
    "sql-01",
    "cache-01"
};
string[] ports =
{
    "HTTPS 443",
    "SQL 5432",
    "Cache 6379"
};
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
CellMap map = new CellMap().Columns(3).LegendLines(2);
foreach (string host in hosts)
    map.RowLabels.Add(host);
foreach (StatusKind state in states)
    map.Add(state);
StatusList exceptions = new StatusList().Add("api-01 → SQL 5432", StatusKind.Fail, "Connection refused · check listener and access policy").Add("api-01 → Cache 6379", StatusKind.Warn, "180 ms · expected under 50 ms");
Rows report = new Rows().Gap(1).Content(c2 =>
{
    c2.Rule("[bold]Application connectivity[/] [muted]· core services[/]");
    c2.Add(new Panel().Title(string.Join("   |   ", ports)).Content(c =>
    {
        c.Add(map);
    }));
    c2.Add(new Panel().Title("Exceptions to investigate").Content(c =>
    {
        c.Add(exceptions);
    }));
    c2.Text("[muted]Skipped cells are ports that host does not need.[/]");
});
// Behaviour: print the collected observations once; collectors belong before the layout.
report.Show();
