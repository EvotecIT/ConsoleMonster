---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Add-ConsoleValue
## SYNOPSIS
Adds a value to a chart in a running dashboard.

Appends to a sparkline or line-chart series (-Key names the series), counts a value in a histogram, or adds to a bar or stacked-bar segment (-Key names it).

## SYNTAX
### __AllParameterSets
```powershell
Add-ConsoleValue [-WidgetId] <string> [-Value] <double> [-Key <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Adds a value to a chart in a running dashboard.

Appends to a sparkline or line-chart series (-Key names the series), counts a value in a histogram, or adds to a bar or stacked-bar segment (-Key names it).

## EXAMPLES

### EXAMPLE 1
```powershell
Add-ConsoleValue -WidgetId throughput -Key completed -Value 42
```

Append a throughput sample

## PARAMETERS

### -Key
The series, bar or segment.

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

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The value.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -WidgetId
The chart name.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Double`

## OUTPUTS

- `None`

## RELATED LINKS

- None
