# Print a compact offline greeting suitable for a PowerShell profile.

Import-Module -Name ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
# A three-line $PROFILE greeting with fixed sample values; no startup probes.
# Replace these strings with cached local values if you want personal context.
$greeting = '[bold accent]Welcome, explorer.[/] [muted]Workspace profile[/]'
$workspace = '[muted]Workspace[/] comet-notes   [muted]Branch[/] main   [pass]clean[/]'
$tip = '[muted]Tip[/] Get-Help Show-Console -Examples'

# Layout: compose the screen from the data above.
$screen = New-ConsoleText -Markup "$greeting`n$workspace`n$tip"

# Behaviour: print the screen once and return.
$screen | Show-Console
