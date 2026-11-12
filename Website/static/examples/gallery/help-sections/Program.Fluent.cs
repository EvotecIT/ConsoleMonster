// Fold help sections and resize the pane beside them.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster;

// Layout: construct the widgets shown on screen.
Splitter layout = Ui.Splitter().First(Ui.Accordion().Content(accordionContent =>
{
    accordionContent.Collapsible(collapsible => collapsible.Key("Synopsis")
        .Content(Ui.Text("Lists servers and their roles from an inline inventory."))
        .Expanded());
    accordionContent.Collapsible(collapsible => collapsible.Key("Parameters")
        .Content(Ui.Rows().Content(rowsContent =>
        {
            rowsContent.Text("-Name    server name or wildcard");
            rowsContent.Text("-Role    web, db or cache");
            rowsContent.Text("-Online  only servers that answer");
        }))
        .Summary("(3)"));
    accordionContent.Collapsible(collapsible => collapsible.Key("Examples")
        .Content(Ui.Rows().Content(rowsContent =>
        {
            rowsContent.Text("Get-Inventory -Role web");
            rowsContent.Text("Get-Inventory -Name db* -Online");
        }))
        .Summary("(2)"));
})).Second(Ui.Panel().Title("Related").Content(panelContent =>
{
    panelContent.Rows(r => r.Content(rowsContent =>
    {
        rowsContent.Text("Get-Service");
        rowsContent.Text("Test-Connection");
    }));
})).Key("Panes").Ratio(0.6);

// Behaviour: Enter folds a section, + expands all, Ctrl+Shift+Left/Right resizes; Escape exits.
ConsoleApp.Run(layout);
