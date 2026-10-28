using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

Panel left = new Panel(new Align(new TextBlock("[bold]centred[/]")), "Align");
Panel right = new Panel(new Padding(new TextBlock("two cells of padding"), 2, 1, 2, 1), "Padding");
Terminal.Write(new Columns(left, right));
