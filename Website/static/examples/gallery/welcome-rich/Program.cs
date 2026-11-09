// Print an offline welcome screen with a logo, host summary, git status and tip.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Layout;
using ConsoleMonster.Widgets;
using ConsoleMonster.Styling;

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
ViewFacts git = new ViewFacts()
    .Add("main", default, ThemeToken.Accent)
    .Add(2, "modified", ThemeToken.Warn)
    .Add(1, "staged", ThemeToken.Pass)
    .Add("0 ahead / 0 behind");

string separator = Terminal.GetCurrentProfile().Glyphs == ConsoleMonster.Capabilities.GlyphTier.Ascii
    ? "|" : "·";

// Layout: compose the screen from the data above.
KeyValueList hostSummary = new KeyValueList();
foreach (var item in hostData)
    hostSummary.Add(item.Label, item.Value);
Rows layout = new Rows(
    new Logo("HELLO", "[muted]A little context before your next command.[/]"),
    new Panel() { Title = "Host summary", Content = hostSummary, HeaderRight = "local workspace" },
    new Panel() { Title = $"Git {separator} comet-notes", Content = git },
    new TextBlock("[bold]Tip[/] Get-Help New-ConsoleTable -Examples"),
    new TextBlock("[muted]Ready when you are.[/]"))
{
    Gap = 1
};

// Behaviour: print the screen once and return.
Terminal.Write(layout);

record HostDataRow(string Label, string Value);
