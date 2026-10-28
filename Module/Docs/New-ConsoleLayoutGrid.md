---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleLayoutGrid
## SYNOPSIS
Lays console widgets out in an equal-width grid.

Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLayoutGrid [[-Content] <Object>] [-Columns <int>] [-ColumnGap <int>] [-RowGap <int>] [-MinColumnWidth <int>] [-NoFillHeight] [<CommonParameters>]
```

## DESCRIPTION
Lays console widgets out in an equal-width grid.

Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLayoutGrid -Columns 3 -Content { New-ConsoleChart -WidgetId a -Type Sparkline; New-ConsoleChart -WidgetId b -Type Sparkline; New-ConsoleChart -WidgetId c -Type Sparkline }
```

Three charts in a row

## PARAMETERS

### -ColumnGap
Blank columns between cells.

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

### -Columns
Number of columns (default 2).

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

### -Content
Scriptblock that emits the cells.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -MinColumnWidth
Minimum column width before reducing the column count.

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

### -NoFillHeight
Fits rows to content instead of the default-on height fill.

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

### -RowGap
Blank rows between grid rows.

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

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
