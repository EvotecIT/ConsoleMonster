---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleColumns
## SYNOPSIS
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleColumns [[-Content] <Object>] [-Gap <int>] [-FixedWidth <int[]>] [-CollapseBelow <int>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleColumns -Content { New-ConsolePanel -Title 'Steps' -Weight 2 -Content { New-ConsoleStatusList -WidgetId steps }; New-ConsolePanel -Title 'Info' -Content { New-ConsoleKeyValue -WidgetId info } }
```

Two panels, the first twice as wide

### EXAMPLE 2
```powershell
New-ConsoleColumns -FixedWidth 0, 0, 17 -CollapseBelow 90 -Content {
    New-ConsolePanel -Title 'Failures' -Weight 1.5 -Content { New-ConsoleTable -WidgetId failures -Live -Columns 'Server', 'Check' }
    New-ConsolePanel -Title 'Slowest' -Content { New-ConsoleTable -WidgetId slowest -Live -Columns 'Check', 'Took' }
    New-ConsolePanel -Title 'Durations' -Content { New-ConsoleChart -WidgetId durations -Height 5 -Type Histogram -Edges 0, 2, 4, 8 }
}
```

Two tables sharing the width and a histogram 17 cells wide, stacking below 90 cells

## PARAMETERS

### -CollapseBelow
The width in cells below which the columns stack vertically (default: the sum of the children's minimum widths, counting 16 cells for each shared column).

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
Scriptblock that emits the child widgets.

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

### -Fill
Take the remaining height of a live dashboard.

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

### -FixedWidth
Fixed widths in cells, one per child in order; 0 (or no entry) keeps the child's share of the width (-Weight).

```yaml
Type: Int32[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Gap
Blank columns between children (default 1).

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

### -Height
Fixed height in rows for this row of columns.

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
