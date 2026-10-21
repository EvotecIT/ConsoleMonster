using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Panel results = new Panel(new TextBlock("[pass]√[/] 412 passed\n[warn]▲[/] 51 warnings"), "Results");
Panel problems = new Panel(new TextBlock("[fail]✗[/] 23 failed\n[muted]-[/] 14 skipped"), "Problems");
Terminal.Write(new Rows()
    .Add(new Columns(results, problems))
    .Add(new Rule("[muted]2 domains, 10 controllers[/]")));