using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Splitter panes = new Splitter(
    new Panel(new Select("Server", new[] {
        new Choice("WEB01", "WEB01  Web"),
        new Choice("SQL01", "SQL01  Database"),
        new Choice("FS01", "FS01   File")
    }) { PageSize = 3 }) { Title = "Servers" },
    new Panel(new Rows(
        new TextBlock("WEB01 serves the public site."),
        new TextBlock("SQL01 holds the orders database."),
        new TextBlock("FS01 keeps the shared folders."))) { Title = "Notes" })
{
    Key = "Panes",
    Ratio = 0.3
};
ConsoleApp.Run(panes);
