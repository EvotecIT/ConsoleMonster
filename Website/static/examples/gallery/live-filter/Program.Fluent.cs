// Filter a synthetic host table as you type.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;
using ConsoleMonster;

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
    var rows = hosts.Where(host => string.Join(
            " ",
            host.Host,
            host.Role,
            host.Site,
            host.Status
        )
            .IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
        .ToArray();
    var table = Ui.Table(rows)
        .Column(row => row.Host)
        .Column(row => row.Role)
        .Column(row => row.Site)
        .Column(row => row.Status, column => column.Presenter(CellPresenters.StatusWord));
    Rows result = Ui.Rows()
        .Content(c =>
        {
            c.Markup(rows.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " of "
                    + hosts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + " hosts");
            c.Add(table);
        });
    if (rows.Length == 0)
        result.Add(Ui.Markup("[muted]No matching hosts. Backspace to broaden the filter.[/]"));
    return result;
}

Rows layout = Ui.Rows()
    .Gap(1)
    .Content(c =>
    {
        c.Markup("[bold]Live filter[/] [muted]· host inventory[/]");
        c.TextInput(textInput => textInput.Key("Query").Value(string.Empty).Label("Filter").Placeholder("Host, role, site or status"));
        c.Binding(binding => binding.Configure(o => o.Content = state => Results(state.Get<string>(
                        "Query",
                        string.Empty
                    ))));
    });
// Behaviour: edit controls to update the view; Escape exits the app.
ConsoleApp.Run(layout);
record FleetHost(string Host, string Role, string Site, string Status);
