using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

var estate = new
{
    Warsaw = new[]
    {
        new Server("WEB01", "Web", "2026-10-28"),
        new Server("SQL01", "Database", "2026-10-12"),
        new Server("FS01", "File", "2026-09-30")
    },
    Berlin = new[]
    {
        new Server("WEB11", "Web", "2026-10-28"),
        new Server("SQL11", "Database", "2026-08-19")
    }
};

TreeSelect tree = new TreeSelect("Tree", new ObjectExplorer(estate, default, "$estate")) {
    Label = "Servers",
    Height = 10
};
AppBinding picked = new AppBinding(state =>
    TextBlock.Plain("Selected: " + (state["Tree"] is ExplorerNode node ? node.Path : "nothing")));
ConsoleApp.Run(new Rows(new HeaderBar("Inventory"), tree, picked));

record Server(string Name, string Role, string Patched);
