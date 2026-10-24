param(
    [switch]$StaticSnapshot,
    [ValidateRange(0, 0)][int]$Step = 0
)

Import-Module ConsoleMonster

# A three-line $PROFILE greeting with fixed sample values; no startup probes.
# Replace these strings with cached local values if you want personal context.
$greeting = '[bold accent]Welcome, explorer.[/] [muted]Sample profile[/]'
$workspace = '[muted]Workspace[/] comet-notes   [muted]Branch[/] main   [pass]clean[/]'
$tip = '[muted]Tip[/] Get-Help Write-ConsoleWidget -Examples'

New-ConsoleText -Markup "$greeting`n$workspace`n$tip" | Write-ConsoleWidget
