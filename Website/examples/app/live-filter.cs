using System.Globalization;
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

// <more>
var hosts = new[] {
    new { Name = "WEB01", Role = "Web", Site = "North", Status = "Pass" },
    new { Name = "WEB02", Role = "Web", Site = "West", Status = "Warn" },
    new { Name = "SQL01", Role = "Database", Site = "North", Status = "Pass" },
    new { Name = "SQL02", Role = "Database", Site = "West", Status = "Fail" },
    new { Name = "FS01", Role = "Files", Site = "North", Status = "Pass" },
    new { Name = "JOB01", Role = "Worker", Site = "West", Status = "Running" }
};
// </more>

IWidget Results(string query) {
    var matches = hosts.Where(host =>
        host.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
        host.Role.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
        host.Site.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
        host.Status.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
    TextBlock count = new TextBlock("[bold]" + matches.Length.ToString(CultureInfo.InvariantCulture)
        +
            "[/] of " + hosts.Length.ToString(CultureInfo.InvariantCulture) + " hosts");
    Table table = Table.For(matches)
        .Column("Host", host => host.Name)
        .Column("Role", host => host.Role)
        .Column("Site", host => host.Site)
        .Column(
            "Status",
            host => host.Status,
            column => column.Presenter = CellPresenters.StatusWord);
    table.HeaderSeparator = false;
    if (matches.Length == 0) {
        return new Rows(
            count,
            table,
            new TextBlock("[muted]No matching hosts. Backspace to broaden the query.[/]"));
    }
    return new Rows(count, table);
}

Rows layout = new Rows(
    new TextBlock("[bold]Host inventory[/] [muted]· synthetic data[/]"),
    new TextInput("Query") {
        Label = "Query",
        Placeholder = "Host, role, site or status"
    },
    new AppBinding(state => Results(state.Get<string>("Query", string.Empty)))) { Gap = 1 };

ConsoleApp.Run(layout);
