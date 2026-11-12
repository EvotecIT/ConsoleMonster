// Print an offline welcome screen with a logo, host summary, git status and tip.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Capabilities;
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
ViewFacts git = Ui.Facts()
    .Add("main", default, ThemeToken.Accent)
    .Add(2, "modified", ThemeToken.Warn)
    .Add(1, "staged", ThemeToken.Pass)
    .Add("0 ahead / 0 behind");

string separator = Terminal.GetCurrentProfile().Glyphs == GlyphTier.Ascii
    ? "|" : "·";

// Layout: compose the screen from the data above.
KeyValueList hostSummary = Ui.KeyValue();
foreach (var item in hostData)
    hostSummary.Add(item.Label, item.Value);
Rows layout = Ui.Rows().Gap(1).Content(rowsContent =>
{
    rowsContent.Logo(logo => logo.Text("HELLO")
        .Tagline("[muted]A little context before your next command.[/]"));
    rowsContent.Panel(p => p.Title("Host summary").Content(panelContent =>
    {
        panelContent.Add(hostSummary);
    }).HeaderRight("local workspace"));
    rowsContent.Panel(p => p.Title($"Git {separator} comet-notes").Content(panelContent =>
    {
        panelContent.Add(git);
    }));
    rowsContent.Markup("[bold]Tip[/] Get-Help New-ConsoleTable -Examples");
    rowsContent.Markup("[muted]Ready when you are.[/]");
});

// Behaviour: print the screen once and return.
layout.Show();

record HostDataRow(string Label, string Value);
