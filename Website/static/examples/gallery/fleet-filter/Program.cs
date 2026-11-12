// Explore a small fleet dashboard with an interactive inventory filter.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;
using ConsoleMonster.Styling;

// Data: fictional inputs kept in memory.
var hosts = new FleetHost[]
{
    new("web-01", "Web", "North", "Pass"),
    new("web-02", "Web", "West", "Warn"),
    new("sql-01", "Database", "North", "Pass"),
    new("sql-02", "Database", "West", "Fail"),
    new("job-01", "Worker", "West", "Running")
};

// Layout: construct the widgets shown on screen.
IWidget Results(string query)
{
    var rows = hosts.Where(host => string.Join(" ", host.Host, host.Role, host.Site, host.Status)
        .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        .ToArray();
    var table = Table.For(rows)
    .Column(row => row.Host)
    .Column(row => row.Role)
    .Column(row => row.Site)
    .Column(row => row.Status, column => column.Presenter = CellPresenters.StatusWord);

    Rows result = new Rows(
        TextBlock.FromMarkup(rows.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " of " + hosts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " hosts"),
        table);
    if (rows.Length == 0)
        result.Add(
            TextBlock.FromMarkup("[muted]No matching hosts. Backspace to broaden the filter.[/]"));
    return result;
}

Rows layout = new Rows(
    TextBlock.FromMarkup("[accent bold]Fleet health[/] [muted]· production inventory[/]"),
    new Panel()
    {
        Title = "Fleet summary",
        Content = new ViewFacts().Add(2, "healthy", ThemeToken.Pass)
            .Add(1, "warning", ThemeToken.Warn).Add(1, "offline", ThemeToken.Fail)
            .Add(1, "running", ThemeToken.Running)
    },
    new TextInput("Query", string.Empty)
    {
        Label = "Filter",
        Placeholder = "Host, role, site or status"
    },
    new AppBinding(state => Results(state.Get<string>("Query", string.Empty))))
{
    Gap = 1
};

// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);

record FleetHost(string Host, string Role, string Site, string Status);
