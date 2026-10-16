---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleBarChart
## SYNOPSIS
Creates a horizontal bar chart, or a stacked bar with -Stacked.

-Data maps labels to numbers. A stacked bar splits one bar into proportional segments with a legend showing values and shares. Update values live with Add-ConsoleValue or Set-ConsoleValue.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleBarChart [[-Data] <IDictionary>] [-Stacked] [-Unit <string>] [-Name <string>] [-Shape <BarShape>] [<CommonParameters>]
```

## DESCRIPTION
Creates a horizontal bar chart, or a stacked bar with -Stacked.

-Data maps labels to numbers. A stacked bar splits one bar into proportional segments with a legend showing values and shares. Update values live with Add-ConsoleValue or Set-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleBarChart -Name 'Name'
```


## PARAMETERS

### -Data
Labels mapped to values.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The live name (default bars, or stacked with -Stacked).

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

### -Shape
Bar shape: Line (thin stroke) or Block (solid blocks). Default Block.

```yaml
Type: BarShape
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Block, Line

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Stacked
Draw one stacked bar with a legend instead of one bar per label.

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

### -Unit
A unit appended to values.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.BarChart`
- `ConsoleMonster.Widgets.StackedBar`

## RELATED LINKS

- None
