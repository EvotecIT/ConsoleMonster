# Print a compact offline greeting suitable for a PowerShell profile.
param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 0)][int]$Step = 0
)

Import-Module ConsoleMonster
# All data is synthetic and inline; no services are contacted or changed.

# Data: fictional inputs kept in memory.
# A three-line $PROFILE greeting with fixed sample values; no startup probes.
# Replace these strings with cached local values if you want personal context.
$greeting = '[bold accent]Welcome, explorer.[/] [muted]Workspace profile[/]'
$workspace = '[muted]Workspace[/] comet-notes   [muted]Branch[/] main   [pass]clean[/]'
$tip = '[muted]Tip[/] Get-Help Write-ConsoleWidget -Examples'

# Layout: compose the screen from the data above.
$screen = New-ConsoleText -Markup "$greeting`n$workspace`n$tip"

# Live updates: this static screen prints once and returns immediately.
$screen | Write-ConsoleWidget
