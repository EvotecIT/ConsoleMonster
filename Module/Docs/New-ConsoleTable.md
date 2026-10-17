---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTable
## SYNOPSIS
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTable [[-InputObject] <psobject[]>] [-Property <string[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [<CommonParameters>]
```

## DESCRIPTION
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTable -Border 'Value'
```


## PARAMETERS

### -Border
Border: Rounded, Square, Double, Minimal or None.

```yaml
Type: TableBorder
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
A muted caption below the table.

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

### -Expand
Fill the available width (spare width goes to the widest text column).

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

### -InputObject
The objects to show, one row each.

```yaml
Type: PSObject[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -NoHeaderSeparator
Underline the header instead of drawing a separator row under it.

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
Properties to show, in order.

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

### -StatusProperty
A property whose values are statuses (pass/fail/warn/skip/running).

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

### -Title
A title above the table.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Table`

## RELATED LINKS

- None
