// Fold help sections and resize the pane beside them.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Layout: construct the widgets shown on screen.
Splitter layout = Ui.Splitter().First(Ui.Accordion().Content(c2 =>
{
    c2.Collapsible(collapsible => collapsible.Key("Synopsis")
        .Content(Ui.Markup("Lists servers and their roles from an inline inventory."))
        .Expanded());
    c2.Collapsible(collapsible => collapsible.Key("Parameters").Content(Ui.Rows().Content(c =>
    {
        c.Markup("-Name    server name or wildcard");
        c.Markup("-Role    web, db or cache");
        c.Markup("-Online  only servers that answer");
    })).Summary("(3)"));
    c2.Collapsible(collapsible => collapsible.Key("Examples").Content(Ui.Rows().Content(c =>
    {
        c.Markup("Get-Inventory -Role web");
        c.Markup("Get-Inventory -Name db* -Online");
    })).Summary("(2)"));
})).Second(Ui.Panel().Title("Related").Content(c2 =>
{
    c2.Rows(r => r.Content(c =>
    {
        c.Markup("Get-Service");
        c.Markup("Test-Connection");
    }));
})).Key("Panes").Ratio(0.6);

// Behaviour: Enter folds a section, + expands all, Ctrl+Shift+Left/Right resizes; Escape exits.
ConsoleApp.Run(layout);
