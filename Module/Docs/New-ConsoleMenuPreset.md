---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleMenuPreset
## SYNOPSIS
Creates a preset a menu comes with: form values and selections under one name, with a description and a preview.

Give the presets to New-ConsoleMenu (-Preset, or emit them in its scriptblock) and add a New-ConsoleMenuItem -Presets item: it lists them first, marked as built in, before the presets people save; they cannot be overwritten or deleted. Applying one sets the values it names and keeps the rest. -Form with -Values fills in a form opened from the menu (a list for a multi-choice field; a value for a field whose choices load is chosen once they arrive). -Item with -Selection selects values in a selection item. -Preview is shown beside the list while the preset has focus: a scriptblock (run once, now), widgets, text or objects.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleMenuPreset [-Name] <string> [-Description <string>] [-Preview <Object>] [-Form <Form>] [-Values <IDictionary>] [-Item <MenuItem>] [-Selection <Object[]>] [-Selections <IDictionary>] [<CommonParameters>]
```

## DESCRIPTION
Creates a preset a menu comes with: form values and selections under one name, with a description and a preview.

Give the presets to New-ConsoleMenu (-Preset, or emit them in its scriptblock) and add a New-ConsoleMenuItem -Presets item: it lists them first, marked as built in, before the presets people save; they cannot be overwritten or deleted. Applying one sets the values it names and keeps the rest. -Form with -Values fills in a form opened from the menu (a list for a multi-choice field; a value for a field whose choices load is chosen once they arrive). -Item with -Selection selects values in a selection item. -Preview is shown beside the list while the preset has focus: a scriptblock (run once, now), widgets, text or objects.

## EXAMPLES

### EXAMPLE 1
```powershell
$form = New-ConsoleForm 'Backup' -Command 'Start-AppBackup' {
    New-ConsoleField Target 'Target' -Type Select -Choices 'Daily', 'Weekly' -Parameter Target
    New-ConsoleField Compress 'Compress' -Type Toggle -Parameter Compress
}
$menu = New-ConsoleMenu 'Backup helper' {
    New-ConsoleMenuPreset 'Weekly · compressed' -Description 'The Sunday backup.' -Form $form -Values @{ Target = 'Weekly'; Compress = $true } -Preview {
        New-ConsoleKeyValue -Data ([ordered] @{ Target = 'Weekly'; Compress = 'yes' })
    }
    New-ConsoleMenuItem 'Presets' -Presets
    New-ConsoleMenuItem 'Back up' -Form $form
    New-ConsoleMenuItem -Exit
}
```

A preset that fills in a form, with what it sets as the preview

### EXAMPLE 2
```powershell
$targets = New-ConsoleMenuItem 'Targets' -MultiSelect 'SRV01', 'SRV02', 'SRV03'
$menu = New-ConsoleMenu 'Patch' -Item $targets -Preset (New-ConsoleMenuPreset 'First wave' -Item $targets -Selection 'SRV01', 'SRV02') {
    New-ConsoleMenuItem 'Presets' -Presets
}
```

A preset that selects servers in a selection item

## PARAMETERS

### -Description
A sentence shown while the preset has focus in the list.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Form
A form opened from the menu whose fields the preset fills in (with -Values).

```yaml
Type: Form
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Item
A selection item of the menu (New-ConsoleMenuItem -Select or -MultiSelect) whose values the preset selects (with -Selection).

```yaml
Type: MenuItem
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The name; a preset people save cannot take it.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Preview
Content shown beside the list (below it when narrow) while the preset has focus: a scriptblock run once now, widgets, text (markup) or objects (as a list).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Selection
The values to select in -Item: the choice objects or their texts; values the item does not offer are skipped.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Selections
Selections for several menu items in one preset: a hashtable whose keys are the MenuItem objects and whose values are lists of choices. May be combined with -Form/-Values.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Values
The values for -Form by field name: a string, a number, a bool, or a list for a multi-choice field. Values that name a choice (the object, its title or its text) take that choice.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `ConsoleMonster.Interactive.MenuPreset`

## RELATED LINKS

- None
