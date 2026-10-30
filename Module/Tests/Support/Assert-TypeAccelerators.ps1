param([Parameter(Mandatory)][string] $ModulePath)
$ErrorActionPreference = 'Stop'
# The loader uses native management commands; load their own aliases before measuring this module.
Import-Module Microsoft.PowerShell.Management
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$allowed = @(
    'ConsoleMonster.Terminal', 'ConsoleMonster.Layout.IWidget',
    'ConsoleMonster.Styling.Color', 'ConsoleMonster.Styling.RichColor',
    'ConsoleMonster.Styling.Style', 'ConsoleMonster.Styling.RichStyle',
    'ConsoleMonster.Styling.Theme', 'ConsoleMonster.Styling.ThemeToken',
    'ConsoleMonster.Styling.StatusKind', 'ConsoleMonster.Text.RichText',
    'ConsoleMonster.Interactive.InteractionMode', 'ConsoleMonster.Capabilities.GlyphTier',
    'ConsoleMonster.Capabilities.ColorDepth',
    'ConsoleMonster.Interactive.CommandLine', 'ConsoleMonster.Live.SimulatedClock',
    'ConsoleMonster.Live.LiveEvent'
)
# Another module owns both a generic name and one fully qualified collision.
$accelerators::Add('Table', [string])
$accelerators::Add('ConsoleMonster.Text.RichText', [string])
$before = @($accelerators::Get.Keys)
$beforeAliases = @(Get-Alias).Name
Import-Module $ModulePath -Force -WarningAction SilentlyContinue
if (@(Get-Alias | Where-Object { $_.Name -notin $beforeAliases }).Count) { throw 'Import registered a command alias.' }
$added = @($accelerators::Get.Keys | Where-Object { $_ -notin $before })
if (@($added | Where-Object { $_ -notlike 'ConsoleMonster.*' }).Count) { throw 'Import registered an unprefixed type accelerator.' }
if ($added.Count -ne 15 -or @($added | Where-Object { $_ -notin $allowed }).Count) { throw 'The loader must add only the 15 unclaimed allow-list entries.' }
if ($accelerators::Get['Table'] -ne [string] -or $accelerators::Get['ConsoleMonster.Text.RichText'] -ne [string]) { throw 'The loader replaced another module type.' }
$table = New-ConsoleTable -InputObject ([pscustomobject] @{ Label = 'first'; Count = 2 })
if ($table.GetType().FullName -ne 'ConsoleMonster.Widgets.Table') { throw 'Cmdlet construction requires an unlisted accelerator.' }
# A later module may replace an entry we originally registered.
$accelerators::Remove('ConsoleMonster.Styling.Color')
$accelerators::Add('ConsoleMonster.Styling.Color', [string])
Remove-Module ConsoleMonster -Force
if (@($added | Where-Object { $_ -ne 'ConsoleMonster.Styling.Color' -and $accelerators::Get.ContainsKey($_) }).Count) { throw 'Module removal retained owned accelerators.' }
if ($accelerators::Get['Table'] -ne [string] -or $accelerators::Get['ConsoleMonster.Text.RichText'] -ne [string]) { throw 'Module removal changed foreign accelerators.' }
if ($accelerators::Get['ConsoleMonster.Styling.Color'] -ne [string]) { throw 'Module removal deleted a later foreign replacement.' }
$accelerators::Remove('ConsoleMonster.Styling.Color')
$accelerators::Remove('ConsoleMonster.Text.RichText')
Import-Module $ModulePath -Force
$registered = @($accelerators::Get.Keys | Where-Object { $_ -in $allowed })
if ($registered.Count -ne 16) { throw 'A fresh reimport must register all 16 allowed types.' }
foreach ($name in 'Table', 'Panel', 'Color', 'Style', 'Terminal', 'Theme', 'Tree', 'Grid', 'Menu', 'Form', 'Rule', 'Size', 'Rect', 'CommandLine', 'SimulatedClock', 'LiveEvent') {
    if ($name -eq 'Table') { continue } # This entry belongs to the foreign module above.
    if ($name -notin $before -and $accelerators::Get.ContainsKey($name)) { throw "Import registered the generic accelerator '$name'." }
}
$widget = New-ConsoleText 'reimported'
if ($widget.GetType().FullName -ne 'ConsoleMonster.Text.RichText') { throw 'Reimport changed the module type identity.' }
Remove-Module ConsoleMonster -Force
$accelerators::Remove('Table')
'ACCELERATORS-OK'
