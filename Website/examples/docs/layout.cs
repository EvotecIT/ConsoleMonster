using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Invented results; nothing is queried.
Panel results = new Panel(
    new TextBlock("[pass]√[/] 412 passed\n[warn]▲[/] 51 warnings\n[fail]✗[/] 23 failed"),
    "Results");
Panel owner = new Panel(new TextBlock("Platform team\n[muted]ops@example.com[/]"), "Owner");

// A grid: cells fill three equal columns, row by row.
LayoutGrid hosts = new LayoutGrid(
    3,
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC01"),
    new Panel(new TextBlock("[warn]▲ lagging[/]"), "DC02"),
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC03"),
    new Panel(new TextBlock("[fail]✗ offline[/]"), "DC04"),
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC05"),
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC06"));

// Side by side: Fill(2) gives the first panel twice the width of the second.
Rows layout = new Rows(new Columns(results.Fill(2), owner), hosts)
{
    Gap = 1
};

Terminal.Write(layout);
