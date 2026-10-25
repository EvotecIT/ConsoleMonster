---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleColumnFormat
## SYNOPSIS
Creates shared numeric or detail-column layout settings.

Pass the result to a widget's CountColumn, PercentColumn, ValueColumn or DetailColumn parameter. MinWidth is measured in cells; ReserveDigits reserves room for numbers before they grow. Widths remain stable during live updates unless -AllowShrink is specified.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleColumnFormat [-Alignment <HorizontalAlignment>] [-MinWidth <int>] [-ReserveDigits <int>] [-AllowShrink] [<CommonParameters>]
```

## DESCRIPTION
Creates shared numeric or detail-column layout settings.

Pass the result to a widget's CountColumn, PercentColumn, ValueColumn or DetailColumn parameter. MinWidth is measured in cells; ReserveDigits reserves room for numbers before they grow. Widths remain stable during live updates unless -AllowShrink is specified.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleProgress -WidgetId build -Label Build -CountColumn (New-ConsoleColumnFormat -ReserveDigits 4)
```

Reserve four digits for progress counts.

## PARAMETERS

### -Alignment
Alignment within the column; numbers default to Right.

```yaml
Type: HorizontalAlignment
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Center, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AllowShrink
Allows the column to shrink when its current content becomes shorter.

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

### -MinWidth
Minimum width in terminal cells.

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

### -ReserveDigits
Numeric digits reserved from the first frame.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Layout.ColumnFormat`

## RELATED LINKS

- None
