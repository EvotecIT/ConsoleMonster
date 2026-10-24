using ConsoleMonster;
using ConsoleMonster.Widgets;

// Fixed sample values; --snapshot and --step 0 need no special handling.
// Replace these strings with cached local values for personal context.
string greeting = "[bold accent]Welcome, explorer.[/] [muted]Sample profile[/]";
string workspace = "[muted]Workspace[/] comet-notes   [muted]Branch[/] main   [pass]clean[/]";
string tip = "[muted]Tip[/] Get-Help Write-ConsoleWidget -Examples";

Terminal.Write(new TextBlock($"{greeting}\n{workspace}\n{tip}"));
