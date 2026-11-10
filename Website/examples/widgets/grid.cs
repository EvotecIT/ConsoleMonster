using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Terminal.Write(new LayoutGrid(
    3,
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC01"),
    new Panel(TextBlock.FromMarkup("[warn]▲ lagging[/]"), "DC02"),
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC03"),
    new Panel(TextBlock.FromMarkup("[fail]✗ offline[/]"), "DC04"),
    new Panel(TextBlock.FromMarkup("[pass]√ healthy[/]"), "DC05")));
