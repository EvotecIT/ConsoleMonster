---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleFormPreset
## SYNOPSIS
Creates a preset a form comes with: field values under one name, with a description and a preview.

Give the presets to New-ConsoleForm (-Preset, or emit them in its scriptblock next to the fields). They are listed before the presets people save, marked as built in, and cannot be overwritten or deleted; applying one (Read-ConsoleForm -Preset, or the menu's command palette) sets the fields it names the way a saved preset does. -Values maps field names to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice, and a value for a field whose choices load is chosen once they arrive.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFormPreset [-Name] <string> [-Values] <IDictionary> [-Description <string>] [-Preview <Object>] [<CommonParameters>]
```

## DESCRIPTION
Creates a preset a form comes with: field values under one name, with a description and a preview.

Give the presets to New-ConsoleForm (-Preset, or emit them in its scriptblock next to the fields). They are listed before the presets people save, marked as built in, and cannot be overwritten or deleted; applying one (Read-ConsoleForm -Preset, or the menu's command palette) sets the fields it names the way a saved preset does. -Values maps field names to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice, and a value for a field whose choices load is chosen once they arrive.

## EXAMPLES

### EXAMPLE 1
```powershell
$form = New-ConsoleForm 'Report' -Command 'Export-AppReport' {
    New-ConsoleField Period 'Period' -Type Select -Choices 'Day', 'Week', 'Month' -Parameter Period
    New-ConsoleField Path 'Folder' -Type Path -Kind Folder -Parameter Path
    New-ConsoleFormPreset 'Weekly' -Values @{ Period = 'Week'; Path = 'D:\Reports\Weekly' } -Description 'Every Monday.'
    New-ConsoleFormPreset 'Monthly' -Values @{ Period = 'Month'; Path = 'D:\Reports\Monthly' }
}
Read-ConsoleForm $form -Preset Weekly
```

A form with two built-in presets

## PARAMETERS

### -Description
A sentence about the preset, shown with it.

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
Content describing what the preset fills in, for lists that preview a preset while it has focus: a scriptblock run once now, widgets, text (markup) or objects (as a list).

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

### -Values
Field values by field name (ignoring case): a string, a number, a bool, or a list for a multi-choice field.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.FormPreset`

## RELATED LINKS

- None
