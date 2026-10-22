using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Terminal.Write(new Grid(3,
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC01"),
    new Panel(new TextBlock("[warn]▲ lagging[/]"), "DC02"),
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC03"),
    new Panel(new TextBlock("[fail]✗ offline[/]"), "DC04"),
    new Panel(new TextBlock("[pass]√ healthy[/]"), "DC05")));