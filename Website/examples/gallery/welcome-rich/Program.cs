// Print an offline welcome screen with a logo, host summary, git status and tip.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// Fictional host and git values keep the greeting independent of local probes.
// Replace fixed strings with cached local data to keep profile startup fast.
var hostData = new HostDataRow[]
{
    new("Host", "comet-devbox"),
    new("Platform", "Cross-platform development"),
    new("Runtime", "PowerShell 7.4+ / .NET 8+"),
    new("Workspace", "comet-notes")
};
string git = "[accent]main[/]   [warn]2 modified[/]   [pass]1 staged[/]   [muted]0 ahead / 0 behind[/]";

// Layout: compose the screen from the data above.
KeyValueList hostSummary = new KeyValueList();
foreach (var item in hostData)
    hostSummary.Add(item.Label, item.Value);
Rows layout = new Rows(
    new Logo("HELLO", "[muted]A little context before your next command.[/]"),
    new Panel() { Title = "Host summary", Content = hostSummary, HeaderRight = "local workspace" },
    new Panel() { Title = "Git | comet-notes", Content = new TextBlock(git) },
    new TextBlock("[bold]Tip[/] Get-Help New-ConsoleTable -Examples"),
    new TextBlock("[muted]Ready when you are.[/]"))
{
    Gap = 1
};

// Behaviour: print the screen once and return.
Terminal.Write(layout);

record HostDataRow(string Label, string Value);
