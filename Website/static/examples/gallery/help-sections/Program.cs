// Fold help sections and resize the pane beside them.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Layout: construct the widgets shown on screen.
Splitter layout = new Splitter(
    new Accordion(
        new Collapsible("Synopsis", new TextBlock("Lists servers and their roles from an inline inventory."))
        {
            Expanded = true
        },
        new Collapsible("Parameters", new Rows(
            new TextBlock("-Name    server name or wildcard"),
            new TextBlock("-Role    web, db or cache"),
            new TextBlock("-Online  only servers that answer")))
        {
            Summary = "(3)"
        },
        new Collapsible("Examples", new Rows(
            new TextBlock("Get-Inventory -Role web"),
            new TextBlock("Get-Inventory -Name db* -Online")))
        {
            Summary = "(2)"
        }),
    new Panel(new Rows(new TextBlock("Get-Service"), new TextBlock("Test-Connection"))) { Title = "Related" })
{
    Key = "Panes",
    Ratio = 0.6
};

// Behaviour: Enter folds a section, + expands all, Ctrl+Shift+Left/Right resizes; Escape exits.
ConsoleApp.Run(layout);