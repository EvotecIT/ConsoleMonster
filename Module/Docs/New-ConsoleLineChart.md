---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLineChart
## SYNOPSIS
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

-Data maps series names to arrays of values; values are spaced evenly. Append values live with Add-ConsoleValue -Key series.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLineChart [[-Data] <IDictionary>] [-Series <string[]>] [-Height <int>] [-Scatter] [-Unit <string>] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

-Data maps series names to arrays of values; values are spaced evenly. Append values live with Add-ConsoleValue -Key series.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLineChart -Name 'Name'
```


## PARAMETERS

### -Data
Series names mapped to value arrays.

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

### -Height
Rows including the axis (default 8).

```yaml
Type: Int32
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
The live name (default chart).

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

### -Scatter
Draw points instead of lines.

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

### -Series
Series to declare without values (for live charts).

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

### -Unit
A unit for the axis labels.

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

- `ConsoleMonster.Widgets.LineChart`

## RELATED LINKS

- None
