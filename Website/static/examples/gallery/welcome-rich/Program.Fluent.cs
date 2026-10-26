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
KeyValueList hostSummary = Ui.KeyValue();
foreach (var item in hostData)
    hostSummary.Add(item.Label, item.Value);
Rows layout = Ui.Rows().Gap(1).Content(c2 =>
{
    c2.Logo(cmLogo => cmLogo.Text("HELLO").Tagline("[muted]A little context before your next command.[/]"));
    c2.Panel(cmPanel => cmPanel.Title("Host summary").Content(c =>
    {
        c.Add(hostSummary);
    }).HeaderRight("local workspace"));
    c2.Panel(cmPanel => cmPanel.Title("Git | comet-notes").Content(c =>
    {
        c.Markup(git);
    }));
    c2.Markup("[bold]Tip[/] Get-Help New-ConsoleTable -Examples");
    c2.Markup("[muted]Ready when you are.[/]");
});
// Behaviour: print the screen once and return.
layout.Show();
record HostDataRow(string Label, string Value);
