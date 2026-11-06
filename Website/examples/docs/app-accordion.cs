using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Accordion help = new Accordion(
    new Collapsible("Synopsis", new TextBlock("Lists servers and their roles."))
    {
        Expanded = true
    },
    new Collapsible("Parameters", new Rows(
        new TextBlock("-Name    server name or wildcard"),
        new TextBlock("-Role    web, db or cache")))
    {
        Summary = "(2)"
    },
    new Collapsible("Examples", new TextBlock("Get-Inventory -Role web"))
    {
        Summary = "(1)"
    });
ConsoleApp.Run(help);
