---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleMenuItem
## SYNOPSIS
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -Detail can be scriptblocks, recomputed every time the menu draws; -DisabledReason returns why the item is unavailable.

## SYNTAX
### Action (Default)
```powershell
New-ConsoleMenuItem [[-Label] <string>] [[-Action] <scriptblock>] [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Submenu
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Submenu <Object> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Form
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Form <Form> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-OnSubmit <scriptblock>] [<CommonParameters>]
```

### Select
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Select <Object[]> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### MultiSelect
```powershell
New-ConsoleMenuItem [[-Label] <string>] -MultiSelect <Object[]> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-Minimum <Int32>] [-Maximum <Int32>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### LoadChoices
```powershell
New-ConsoleMenuItem [[-Label] <string>] -LoadChoices <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [-ArgumentList <Object[]>] [-Single] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-ChoiceBadge <Object>] [-ChoiceDisabledReason <Object>] [-Preview <scriptblock>] [-Selected <Object>] [-Minimum <Int32>] [-Maximum <Int32>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### Page
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Page <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Review
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Review <scriptblock> [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Presets
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Presets [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Separator
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Separator [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

### Exit
```powershell
New-ConsoleMenuItem [[-Label] <string>] -Exit [-Hotkey <Char>] [-Description <string>] [-Icon <string>] [-IconName <GlyphIcon>] [-Badge <Object>] [-Detail <Object>] [-DisabledReason <Object>] [<CommonParameters>]
```

## DESCRIPTION
Creates a menu item: an action, a submenu, a form, a selection, a page, a review, the presets list, a separator or the exit.

What the item does follows from the parameter you give: -Action (a scriptblock, run on the pipeline thread with $MenuContext), -Submenu (a menu or a scriptblock of items), -Form (with -OnSubmit, which gets $Values and $FormResult), -Select or -MultiSelect (the chosen objects stay on the item: $item.Selection), -LoadChoices (loaded in its own runspace with a spinner), -Page (content built each time it shows), -Review (a summary with the equivalent command and Run), -Presets, -Separator or -Exit. -Badge and -Detail can be scriptblocks, recomputed every time the menu draws; -DisabledReason returns why the item is unavailable.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleMenuItem 'Tasks' -Hotkey k -MultiSelect $tasks -Display Name -GroupBy Category -Preview { $_.Description }
```

A multi-selection with a computed badge

### EXAMPLE 2
```powershell
New-ConsoleMenuItem 'Clear cache' -Action { if ($MenuContext.Confirm('Clear the cache?')) { Clear-AppCache; $MenuContext.Toast('Cache cleared') } }
```

An action that asks first and shows a toast

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
Default value: None
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
A short mark before the title, as free text. Prefer -IconName, which follows the glyph tier.

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

### -IconName
A semantic icon that follows the glyph tier: Item, Targets, Tasks, Settings, Favorite, Run, Back, List, Help, Warning, Done or Exit (for example ● in modern fonts, o in ASCII). Takes precedence over -Icon.

```yaml
Type: GlyphIcon
Parameter Sets: Action, Submenu, Form, Select, MultiSelect, LoadChoices, Page, Review, Presets, Separator, Exit
Aliases: None
Possible values: Item, Targets, Tasks, Settings, Favorite, Run, Back, List, Help, Warning, Done, Exit

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The title.

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
Default value: None
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
Default value: None
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
Default value: None
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

- None
