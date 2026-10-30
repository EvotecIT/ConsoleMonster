---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Compare-ConsoleObject
## SYNOPSIS
Compares two sets of objects and shows what was added, removed and changed, property by property.

Like Compare-Object, with a much better display: objects are matched by -Property (or by Id, Name, Key and similar members), then compared property by property, nested objects by path. The view shows the counts by kind and a table of the changes in the design-system tones (added in green, removed in red, changed in yellow), with line diffs of long and multi-line values; -View picks Summary, Table, SideBySide or Unified. -PassThru returns the structured changes (ConsoleMonster.Changes.ObjectChange) instead.

## SYNTAX
### Compare (Default)
```powershell
Compare-ConsoleObject [-ReferenceObject] <Object[]> [-DifferenceObject] <Object[]> [-Property <string[]>] [-CompareProperty <string[]>] [-Ignore <string[]>] [-Depth <int>] [-IncludeEqual] [-View <ChangeViewMode>] [-Title <Object>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Confirm
```powershell
Compare-ConsoleObject [-ReferenceObject] <Object[]> [-DifferenceObject] <Object[]> -Question <string> [-Property <string[]>] [-CompareProperty <string[]>] [-Ignore <string[]>] [-Depth <int>] [-IncludeEqual] [-View <ChangeViewMode>] [-Title <Object>] [-TypedConfirmation <string>] [-Force] [-NoTypedWhenDestructive] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Compares two sets of objects and shows what was added, removed and changed, property by property.

Like Compare-Object, with a much better display: objects are matched by -Property (or by Id, Name, Key and similar members), then compared property by property, nested objects by path. The view shows the counts by kind and a table of the changes in the design-system tones (added in green, removed in red, changed in yellow), with line diffs of long and multi-line values; -View picks Summary, Table, SideBySide or Unified. -PassThru returns the structured changes (ConsoleMonster.Changes.ObjectChange) instead.

## EXAMPLES

### EXAMPLE 1
```powershell
$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
$after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name
```

Compare two small inventories

### EXAMPLE 2
```powershell
$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
$after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
$accepted = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Property Name -Question 'Accept these changes?'
$accepted
```

Ask before accepting a configuration change

## PARAMETERS

### -CompareProperty
The properties to compare (default all).

```yaml
Type: String[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
How deep nested objects are compared (default 3).

```yaml
Type: Int32
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DifferenceObject
The new objects.

```yaml
Type: Object[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Force
Accepts a nonempty change set without asking. WhatIf still returns false.

```yaml
Type: SwitchParameter
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Ignore
Properties or paths (Address.City, wildcards) left out of the comparison.

```yaml
Type: String[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeEqual
Lists unchanged objects too.

```yaml
Type: SwitchParameter
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoTypedWhenDestructive
Uses a yes/no question for removals unless TypedConfirmation is supplied.

```yaml
Type: SwitchParameter
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the changes as objects instead of showing them.

```yaml
Type: SwitchParameter
Parameter Sets: Compare
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
The properties that identify an object (matching key). Default: Id, Key, Name and similar members.

```yaml
Type: String[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Question
Shows the changes, asks this question and returns a Boolean answer.

```yaml
Type: String
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReferenceObject
The old objects.

```yaml
Type: Object[]
Parameter Sets: Compare, Confirm
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
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TypedConfirmation
Text that must be typed to confirm the preview.

```yaml
Type: String
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -View
The view: Full (default), Summary, Table, SideBySide or Unified.

```yaml
Type: ChangeViewMode
Parameter Sets: Compare, Confirm
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

- `System.Object[]`

## OUTPUTS

- `ConsoleMonster.Changes.ObjectChange`

## RELATED LINKS

- None
