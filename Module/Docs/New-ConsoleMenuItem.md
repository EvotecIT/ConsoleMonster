---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolemenuitem/
schema: 2.0.0
---
# New-ConsoleMenuItem
## SYNOPSIS
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

## SYNTAX
### Action (Default)
```powershell
New-ConsoleMenuItem [[-Label] <string>] [[-Action] <scriptblock>] [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Submenu
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Submenu <Object> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Form
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Form <Form> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-OnSubmit <scriptblock>] [<CommonParameters>]
```

### Select
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Select <Object[]> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### MultiSelect
```powershell
New-ConsoleMenuItem [[-Label] <string>] -MultiSelect <Object[]> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-Minimum <Int32>] [-Maximum <Int32>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### LoadChoices
```powershell
New-ConsoleMenuItem [[-Label] <string>] -LoadChoices <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-ArgumentList <Object[]>] [-Single] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-Minimum <Int32>] [-Maximum <Int32>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### Page
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Page <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Review
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Review <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Presets
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Presets [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Separator
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Separator [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Exit
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Exit [-Hotkey <Char>] [-Description <string>] [-Mark <string>] [-Icon <Icon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

## DESCRIPTION
What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -Detail can be scriptblocks, recomputed every time the menu draws; -DisabledReason returns why the item is unavailable.

## EXAMPLES

### EXAMPLE 1
```powershell
$tasks = @(
    [pscustomobject]@{ Name = 'Backup'; Category = 'Files'; Description = 'Preview a backup'; Enabled = $true }
    [pscustomobject]@{ Name = 'Verify'; Category = 'Files'; Description = 'Verify a backup'; Enabled = $false }
)
$menuOptions2 = @{
    Title = 'Tasks'
    Content = {
        $menuItemOptions = @{
            Label = 'Tasks'
            Hotkey = 'k'
            MultiSelect = $tasks
            Display = 'Name'
            GroupBy = 'Category'
            Preview = { $_.Description }
            Selected = { $_.Enabled }
        }
        New-ConsoleMenuItem @menuItemOptions
        New-ConsoleMenuItem -Exit
    }
}
$menu = New-ConsoleMenu @menuOptions2
$menuOptions = @{
    Menu = $menu
}
Show-ConsoleMenu @menuOptions
```

Build a multiple-choice item with an initial selection

### EXAMPLE 2
```powershell
$menuOptions2 = @{
    Title = 'Inventory'
    Content = {
        $menuItemOptions = @{
            Label = 'Preview refresh'
            Action = {
                if ($MenuContext.Confirm('Preview an inventory refresh?')) {
                    $MenuContext.Toast('Preview ready')
                    $MenuContext.ShowResult(@{ Title = 'Refresh preview'; Status = 'Pass'; Summary = 'No inventory changes were made.' })
                }
            }
        }
        New-ConsoleMenuItem @menuItemOptions
        New-ConsoleMenuItem -Exit
    }
}
$menu = New-ConsoleMenu @menuOptions2
$menuOptions = @{
    Menu = $menu
}
Show-ConsoleMenu @menuOptions
```

Confirm an action and return a result record

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'toolbox' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'fleet' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleMenuItem in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Action
The action, run on the pipeline thread; $MenuContext (also $_) offers Toast, Confirm, Ask, Run (a live dashboard) and ShowResult.

```yaml
Type: ScriptBlock
Parameter Sets: Action
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ArgumentList
Arguments for the -LoadChoices script.

```yaml
Type: Object[]
Parameter Sets: LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Badge
Right-aligned text: a string, rich text, or a scriptblock recomputed whenever the menu draws.

```yaml
Type: Object
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChoiceBadge
Badge for each selection choice: a property name or scriptblock. With -LoadChoices, use a property name from the loader's output. The item's own badge remains -Badge.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChoiceDisabledReason
Disabled reason for each selection choice: a property name or scriptblock; null or empty enables the choice. With -LoadChoices, use a property name. The item's own disabled reason remains -DisabledReason.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Describe
A sentence for the focused choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
A sentence shown with the item.

```yaml
Type: String
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
State text ("24 selected"): a string or a scriptblock recomputed whenever the menu draws.

```yaml
Type: Object
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DisabledReason
Why the item is unavailable: a string, or a scriptblock returning the reason (nothing when available).

```yaml
Type: Object
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Display
What to show for a choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exit
Makes the exit item (-Label defaults to Exit).

```yaml
Type: SwitchParameter
Parameter Sets: Exit
Aliases: None
Possible values:

Required: True
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Form
A form from New-ConsoleForm.

```yaml
Type: Form
Parameter Sets: Form
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
The group of a choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Hotkey
The key that opens the item directly.

```yaml
Type: Char
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Icon
An icon catalog key that follows the glyph tier: item, targets, tasks, settings, favorite, run, back, list, help, warning, done or exit (for example ● in modern fonts, o in ASCII). Takes precedence over -Mark.

```yaml
Type: Icon
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: IconName
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
Literal text shown for this selectable menu item; use Description for its supporting detail.

```yaml
Type: String
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LoadChoices
Loads the choices in its own runspace (a spinner shows meanwhile). Pair with -Single for one value.

```yaml
Type: ScriptBlock
Parameter Sets: LoadChoices
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mark
A short mark before the title, as free text. Prefer -Icon, which follows the glyph tier.

```yaml
Type: String
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Maximum
The most values of a multi-selection.

```yaml
Type: Int32
Parameter Sets: MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Minimum
The fewest values of a multi-selection.

```yaml
Type: Int32
Parameter Sets: MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MultiSelect
Choices for picking several values (any objects).

```yaml
Type: Object[]
Parameter Sets: MultiSelect
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnChange
Runs when the selection changes ($MenuContext and $_).

```yaml
Type: ScriptBlock
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnSubmit
Runs when the form is submitted; $Values is a hashtable of the values, $FormResult the full result.

```yaml
Type: ScriptBlock
Parameter Sets: Form
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Page
Page content, built each time it shows: text, rich text, widgets, or objects shown as a list.

```yaml
Type: ScriptBlock
Parameter Sets: Page
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Presets
Makes the item list the menu's presets: the ones it comes with (New-ConsoleMenu -Preset) first, marked as built in, with their previews, then the saved ones. Enter applies one; Delete removes a saved one (Shell and Classic layouts; the Inline layout applies them only).

```yaml
Type: SwitchParameter
Parameter Sets: Presets
Aliases: None
Possible values:

Required: True
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Preview
Preview content for the focused choice ($_ is the choice).

```yaml
Type: ScriptBlock
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Review
A review built each time it shows: a hashtable with Rows (an ordered hashtable of label = value), Command (the
equivalent command line), Problems (strings that block running), Run (a scriptblock) and optionally Title,
Description and RunText.

```yaml
Type: ScriptBlock
Parameter Sets: Review
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Select
Choices for picking one value (any objects).

```yaml
Type: Object[]
Parameter Sets: Select
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Selected
Pre-selected choices: the objects or their texts, or a scriptblock returning $true for static choices. With -LoadChoices, pass initial values; they are matched when the choices arrive.

```yaml
Type: Object
Parameter Sets: Select, MultiSelect, LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Separator
Makes a divider (with -Label as its heading, if given).

```yaml
Type: SwitchParameter
Parameter Sets: Separator
Aliases: None
Possible values:

Required: True
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Single
With -LoadChoices: pick one value instead of several.

```yaml
Type: SwitchParameter
Parameter Sets: LoadChoices
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Submenu
A submenu: a menu from New-ConsoleMenu, or a scriptblock of New-ConsoleMenuItem calls.

```yaml
Type: Object
Parameter Sets: Submenu
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.MenuItem`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolemenuitem/](https://consolemonster.dev/powershell/commands/new-consolemenuitem/)
