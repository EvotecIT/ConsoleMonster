using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Fictional host and git values; --snapshot and --step 0 need no special handling.
// Replace fixed strings with cached local data to keep profile startup fast.
KeyValueList hostSummary = new KeyValueList()
    .Add("Host", "comet-devbox")
    .Add("Platform", "Unix-like / sample environment")
    .Add("Runtime", "PowerShell 7.4+ / .NET 8+")
    .Add("Workspace", "comet-notes");
string git = "[accent]main[/]   [warn]2 modified[/]   [pass]1 staged[/]   [muted]0 ahead / 0 behind[/]";

Terminal.Write(new Rows(
    new Logo("HELLO", "[muted]A little context before your next command. Sample profile.[/]"),
    new Panel(hostSummary, "Host summary") { Caption = "synthetic values" },
    new Panel(new TextBlock(git), "Git | comet-notes"),
    new TextBlock("[bold]Tip[/] Get-Help New-ConsoleTable -Examples"),
    new TextBlock("[muted]Ready when you are. Everything here is fixed sample data.[/]")) { Gap = 1 });
