using ConsoleMonster;
using ConsoleMonster.Widgets;

Terminal.Write(new TextBlock(
    "[bold]Deployed[/] in [accent]48 s[/]: " +
    "[pass]√ 12 passed[/] [warn]▲ 2 warned[/] [fail]✗ 1 failed[/]"));
Terminal.Write(new TextBlock(
    "[italic]Decorations:[/] [bold]bold[/] [dim]dim[/] [underline]underline[/] " +
    "[strikethrough]struck[/] [invert] invert [/]"));
