using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Invented results; nothing is queried.
Panel results = new Panel(
    TextBlock.FromMarkup("[pass]√[/] 412 passed\n[warn]▲[/] 51 warnings\n[fail]✗[/] 23 failed"),
    "Results");
Panel owner = new Panel(TextBlock.FromMarkup("Platform team\n[muted]ops@example.com[/]"), "Owner");

// A grid: cells fill three equal columns, row by row.
LayoutGrid hosts = new LayoutGrid(
    3,
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC01"),
    new Panel(TextBlock.FromMarkup("[warn]▲ lagging[/]"), "DC02"),
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC03"),
    new Panel(TextBlock.FromMarkup("[fail]✗ offline[/]"), "DC04"),
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC05"),
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC06"));

// Side by side: Fill(2) gives the first panel twice the width of the second.
Rows layout = new Rows(new Columns(results.Fill(2), owner), hosts)
{
    Gap = 1
};

Terminal.Write(layout);
