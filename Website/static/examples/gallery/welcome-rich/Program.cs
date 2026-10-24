// Print an offline welcome screen with a logo, host summary, git status and tip.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// Fictional host and git values; --snapshot and --step 0 need no special handling.
// Replace fixed strings with cached local data to keep profile startup fast.
var hostData = new[] {
    (Label: "Host", Value: "comet-devbox"),
    (Label: "Platform", Value: "Unix-like / sample environment"),
    (Label: "Runtime", Value: "PowerShell 7.4+ / .NET 8+"),
    (Label: "Workspace", Value: "comet-notes")
};
string git = "[accent]main[/]   [warn]2 modified[/]   [pass]1 staged[/]   [muted]0 ahead / 0 behind[/]";

// Layout: compose the screen from the data above.
KeyValueList hostSummary = new KeyValueList();
foreach (var item in hostData) hostSummary.Add(item.Label, item.Value);

Rows layout = new Rows(
    new Logo("HELLO", "[muted]A little context before your next command. Sample profile.[/]"),
    new Panel(hostSummary, "Host summary") { Caption = "synthetic values" },
    new Panel(new TextBlock(git), "Git | comet-notes"),
    new TextBlock("[bold]Tip[/] Get-Help New-ConsoleTable -Examples"),
    new TextBlock("[muted]Ready when you are. Everything here is fixed sample data.[/]")) { Gap = 1 };

// Live updates: this static screen prints once and returns immediately.
Terminal.Write(layout);
