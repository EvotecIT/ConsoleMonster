using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Terminal.Write(new Panel(new TextBlock("Four controllers answered. [warn]DC02[/] lags by 42 minutes."), "[bold]Forest[/]")
    .WithCaption("ad.adatum.com")
    .WithFooter("[muted]checked 14:30[/]"));