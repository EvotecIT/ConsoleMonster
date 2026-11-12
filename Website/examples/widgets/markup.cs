using ConsoleMonster;
using ConsoleMonster.Widgets;

Terminal.Write(TextBlock.FromMarkup("[bold]Deployed[/] in [accent]48 s[/]: "
    + "[pass]√ 12 passed[/] [warn]▲ 2 warned[/] [fail]✗ 1 failed[/]"));
Terminal.Write(
    TextBlock.FromMarkup(
        "[italic]Decorations:[/] [bold]bold[/] [dim]dim[/] [underline]underline[/] "
            + "[strikethrough]struck[/] [invert] invert [/]"));
