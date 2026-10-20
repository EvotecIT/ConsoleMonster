---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Set-ConsoleFormValue
## SYNOPSIS
Sets field values of a form, whether or not it is shown.

Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Value maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Value also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.

## SYNTAX
### __AllParameterSets
```powershell
Set-ConsoleFormValue [-Form] <Form> [-Value] <Object> [<CommonParameters>]
```

## DESCRIPTION
Sets field values of a form, whether or not it is shown.

Each named field takes its value and shows it at once, text and number boxes included; fields not named keep theirs. -Value maps field names (ignoring case) to values: a string, a number, a bool, or a list for a multi-choice field; a value that names a choice (the object, its title or its text) takes that choice. Choice fields whose options load from a field that changed load again, and a value given for such a field is chosen once its options arrive. Unknown names and values a field cannot take are ignored. -Value also takes the result of an earlier run of the form (Read-ConsoleForm -PassThru, or $FormResult in -OnSubmit) to fill in every field again. Call it from a menu action or handler while the menu runs, or before the form is shown.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleMenuItem 'Use the lab' -Action { Set-ConsoleFormValue -Form $form -Value @{ Domain = 'lab.example.test'; Parallel = 2 }; $MenuContext.Toast('Lab values applied') }
```

A menu action that fills in the form opened from another item

### EXAMPLE 2
```powershell
$last = Read-ConsoleForm $form -PassThru
Set-ConsoleFormValue $form $last
Read-ConsoleForm $form
```

Start a form from the answers of its last run

## PARAMETERS

### -Form
The form from New-ConsoleForm.

```yaml
Type: Form
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
Values by field name (a hashtable), or the result of an earlier run of the form (a form result or its Values).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
