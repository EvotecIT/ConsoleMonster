using ConsoleMonster.Interactive;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Tabs server = new Tabs("Page")
    .Add("Overview", new Rows(
        new TextBlock("SQL01 · Database · Warsaw"),
        new TextBlock("Patched 2026-10-12")))
    .Add("Disks", new Rows(
        new TextBlock("C:  41% used"),
        new TextBlock("D:  97% used")))
    .Add("Services", new Rows(
        new TextBlock("MSSQLSERVER  Running"),
        new TextBlock("SQLAgent     Running")));
ConsoleApp.Run(server);
