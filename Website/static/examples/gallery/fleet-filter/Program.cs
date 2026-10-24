// Explore a small fleet dashboard with an interactive inventory filter.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
var hosts = new[] {
    new { Host = "web-01", Role = "Web", Site = "North", Status = "Pass" },
    new { Host = "web-02", Role = "Web", Site = "West", Status = "Warn" },
    new { Host = "sql-01", Role = "Database", Site = "North", Status = "Pass" },
    new { Host = "sql-02", Role = "Database", Site = "West", Status = "Fail" },
    new { Host = "job-01", Role = "Worker", Site = "West", Status = "Running" }
};
// Run options: redirected input selects a deterministic snapshot.
bool snapshot = args.Contains("--snapshot") || (!args.Contains("--interactive") && Console.IsInputRedirected);
int stepAt = Array.IndexOf(args, "--step");
int step = stepAt < 0 ? 0 : int.Parse(args[stepAt + 1], System.Globalization.CultureInfo.InvariantCulture);
if (step < 0 || step > 2) throw new ArgumentOutOfRangeException(nameof(step));

// Layout: construct the widgets shown on screen.
IWidget Results(string query) {
    var rows = hosts.Where(host => string.Join(" ", host.Host, host.Role, host.Site, host.Status)
        .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
    Table table = new Table().AddColumn("Host").AddColumn("Role").AddColumn("Site").AddColumn("Status", column => column.Presenter = CellPresenters.StatusWord);
    foreach (var host in rows) table.AddRow(host.Host, host.Role, host.Site, TableCell.FromValue(host.Status));
    Rows result = new Rows(new TextBlock(rows.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) +
        " of " + hosts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " hosts"), table);
    if (rows.Length == 0) result.Add(new TextBlock("[muted]No matching hosts. Backspace to broaden the filter.[/]"));
    return result;
}
Rows layout = new Rows(
    new TextBlock("[bold]Fleet health[/] [muted]· production inventory[/]"),
    new Panel(new TextBlock("[pass]2 healthy[/]  [warn]1 warning[/]  [fail]1 offline[/]  [accent]1 running[/]"), "Fleet summary"),

    new TextInput("Query", new[] { "", "sql", "missing" }[step]) { Label = "Filter", Placeholder = "Host, role, site or status" },
    new AppBinding(state => Results(state.Get<string>("Query", string.Empty)))) { Gap = 1 };
// Live updates: ConsoleApp handles input and reevaluates AppBinding as state changes.
// Escape exits; snapshot mode prints the selected state without reading keys.
ConsoleApp.Run(layout, options: new ConsoleAppOptions { StaticSnapshot = snapshot });
