using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new KeyValueList()
    .Add("Module", "[bold]ConsoleMonster[/]")
    .Add("Version", "0.0.4 [muted]→[/] [bold]2.0.0[/]")
    .Add("Frameworks", "netstandard2.0, net472, net8.0")
    .Add("Signing", "skipped", StatusKind.Skip)
    .Add("Tests", "1 skipped on PowerShell 7", StatusKind.Warn));