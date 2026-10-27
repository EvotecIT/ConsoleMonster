---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleChanges
## SYNOPSIS
Shows the changes an action would make and asks before making them: the what-if pattern.

Compares -ReferenceObject with -DifferenceObject (as Compare-ConsoleObject does), shows the preview, then asks for confirmation and returns $true or $false; act only on $true. Changes that remove objects need the number of removals typed (or the -TypedConfirmation text). It honours the caller's -WhatIf: the preview is shown and $false returned without asking. -Force (or -Confirm:$false) skips the question and returns $true when there are changes. Use it inside your own functions: if (Show-ConsoleChanges -ReferenceObject $current -DifferenceObject $planned -Property Name) { apply }.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleChanges [-ReferenceObject] <Object[]> [-DifferenceObject] <Object[]> [-Property <string[]>] [-Ignore <string[]>] [-View <ChangeViewMode>] [-Title <Object>] [-Question <string>] [-TypedConfirmation <string>] [-Force] [-NoTypedWhenDestructive] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Shows the changes an action would make and asks before making them: the what-if pattern.

Compares -ReferenceObject with -DifferenceObject (as Compare-ConsoleObject does), shows the preview, then asks for confirmation and returns $true or $false; act only on $true. Changes that remove objects need the number of removals typed (or the -TypedConfirmation text). It honours the caller's -WhatIf: the preview is shown and $false returned without asking. -Force (or -Confirm:$false) skips the question and returns $true when there are changes. Use it inside your own functions: if (Show-ConsoleChanges -ReferenceObject $current -DifferenceObject $planned -Property Name) { apply }.

## EXAMPLES

### EXAMPLE 1
```powershell
if (Show-ConsoleChanges -ReferenceObject $members -DifferenceObject $planned -Property SamAccountName -Title 'Domain Admins') { Set-Members $planned }
```

Preview a group membership change

## PARAMETERS

### -DifferenceObject
The objects after the change.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Skips the question and returns $true when there are changes.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Ignore
Properties or paths left out of the comparison.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoTypedWhenDestructive
Keeps a yes/no confirmation for removals instead of requiring the removal count. An explicit TypedConfirmation still takes precedence.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
The properties that identify an object (matching key).

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Question
The question (default "Apply these changes?").

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

### -ReferenceObject
The current objects.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title (markup).

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

### -TypedConfirmation
Text that must be typed to confirm (for example the name of what is changed).

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

### -View
The view of the preview: Full (default), Summary, Table, SideBySide or Unified.

```yaml
Type: ChangeViewMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Full, Summary, Table, SideBySide, Unified

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

- `System.Boolean`

## RELATED LINKS

- None
