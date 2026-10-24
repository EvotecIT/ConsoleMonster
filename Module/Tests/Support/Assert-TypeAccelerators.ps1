param([Parameter(Mandatory)][string] $ModulePath)
$ErrorActionPreference = 'Stop'
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
$allowed = @(
    'ConsoleMonster.Terminal', 'ConsoleMonster.Layout.IWidget',
    'ConsoleMonster.Styling.Color', 'ConsoleMonster.Styling.RichColor',
    'ConsoleMonster.Styling.Style', 'ConsoleMonster.Styling.RichStyle',
    'ConsoleMonster.Styling.Theme', 'ConsoleMonster.Styling.ThemeToken',
    'ConsoleMonster.Styling.StatusKind', 'ConsoleMonster.Text.RichText',
    'ConsoleMonster.Interactive.InteractionMode', 'ConsoleMonster.Capabilities.GlyphTier',
    'ConsoleMonster.Capabilities.ColorDepth'
)
# Another module owns both a generic name and one fully qualified collision.
$accelerators::Add('Table', [string])
$accelerators::Add('ConsoleMonster.Text.RichText', [string])
$before = @($accelerators::Get.Keys)
Import-Module $ModulePath -Force -WarningAction SilentlyContinue
$added = @($accelerators::Get.Keys | Where-Object { $_ -notin $before })
if ($added.Count -ne 12 -or @($added | Where-Object { $_ -notin $allowed }).Count) { throw 'The loader must add only the 12 unclaimed allow-list entries.' }
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
if ($registered.Count -ne 13) { throw 'A fresh reimport must register all 13 allowed types.' }
$widget = New-ConsoleText 'reimported'
if ($widget.GetType().FullName -ne 'ConsoleMonster.Text.RichText') { throw 'Reimport changed the module type identity.' }
Remove-Module ConsoleMonster -Force
$accelerators::Remove('Table')
'ACCELERATORS-OK'
