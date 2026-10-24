using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Terminal.Write(new Panel(new TextBlock("Four controllers answered. [warn]DC02[/] lags by 42 minutes."), "[bold]Forest[/]")
    .WithHeaderRight("ad.adatum.com")
    .WithCaption("[muted]checked 14:30[/]"));