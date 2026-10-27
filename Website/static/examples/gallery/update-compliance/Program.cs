// Check monthly patch compliance across a server fleet and call out overdue hosts.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

// Data: fictional servers and a fixed report date; no update agent or WSUS server is queried.
var servers = new Server[]
{
    new("dc-01", "Pilot", "Compliant", 0, "2026-10-15", 5, "Pass"),
    new("dc-02", "Broad", "Compliant", 0, "2026-10-16", 4, "Pass"),
    new("fs-01", "Broad", "Pending reboot", 0, "2026-10-14", 6, "Warn"),
    new("fs-02", "Broad", "Compliant", 0, "2026-10-16", 4, "Pass"),
    new("sql-01", "Broad", "Missing updates", 3, "2026-09-12", 38, "Fail"),
    new("sql-02", "Broad", "Compliant", 0, "2026-10-17", 3, "Pass"),
    new("web-01", "Pilot", "Compliant", 0, "2026-10-15", 5, "Pass"),
    new("web-02", "Broad", "Pending reboot", 0, "2026-10-15", 5, "Warn"),
    new("app-01", "Broad", "Compliant", 0, "2026-10-16", 4, "Pass"),
    new("app-02", "Broad", "Failed", 5, "2026-08-19", 62, "Fail"),
    new("print-01", "Broad", "Missing updates", 2, "2026-10-02", 18, "Warn"),
    new("hv-01", "Pilot", "Compliant", 0, "2026-10-15", 5, "Pass")
};
var states = new State[]
{
    new("Compliant", ThemeToken.Pass),
    new("Pending reboot", ThemeToken.Info),
    new("Missing updates", ThemeToken.Warn),
    new("Failed", ThemeToken.Fail)
};

// Behaviour: count each state, keep the hosts that need work and flag anything unpatched for 30+ days.
var byState = new StackedBar();
foreach (var state in states)
    byState.Add(state.Label, servers.Count(server => server.State == state.Label), state.Role);
var attention = servers.Where(server => server.State != "Compliant").ToArray();
var overdue = new StatusList();
foreach (var server in servers.Where(server => server.Days > 30))
    overdue.Add(server.Host, StatusKind.Fail, $"{server.Days} days · {server.State.ToLowerInvariant()} · {server.Missing} missing");

// Layout: the fleet split first, then the hosts to fix, then the overdue call-out.
var table = Table.For(attention)
    .Column(row => row.Host)
    .Column(row => row.Ring)
    .Column(row => row.State)
    .Column(row => row.Missing)
    .Column(row => row.LastPatched)
    .Column(row => row.Days)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);
table.Expand = true;

var report = new Rows(
    new Rule("[bold]Windows Update compliance[/] [muted]· 2026-10 cumulative · as of 2026-10-20[/]"),
    new Panel(byState, $"Fleet · {servers.Length} servers"),
    table,
    new Panel(overdue, "Overdue · unpatched for more than 30 days"))
{
    Gap = 1
};

// Behaviour: print once; schedule the overdue hosts into the next maintenance window.
Terminal.Write(report);

record Server(string Host, string Ring, string State, int Missing, string LastPatched, int Days, string Status);
record State(string Label, ThemeToken Role);
