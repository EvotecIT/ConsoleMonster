// Print a compact offline greeting suitable for a PowerShell profile.
// All data is synthetic and inline; no services are contacted or changed.
using ConsoleMonster;
using ConsoleMonster.Widgets;

// Data: fictional inputs kept in memory.
// Fixed sample values keep the greeting independent of local probes.
// Replace these strings with cached local values for personal context.
string greeting = "[bold accent]Welcome, explorer.[/] [muted]Workspace profile[/]";
string workspace = "[muted]Workspace[/] comet-notes   [muted]Branch[/] main   [pass]clean[/]";
string tip = "[muted]Tip[/] Get-Help Show-Console -Examples";
// Layout: compose the greeting from the data above.
TextBlock layout = Ui.Markup($"{greeting}\n{workspace}\n{tip}");
// Behaviour: print the screen once and return.
layout.Show();
