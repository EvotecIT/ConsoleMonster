---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolelayoutgrid/
schema: 2.0.0
---
# New-ConsoleLayoutGrid
## SYNOPSIS
Lays console widgets out in an equal-width grid.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLayoutGrid [[-Content] <Object>] [-Columns <int>] [-ColumnGap <int>] [-RowGap <int>] [-MinColumnWidth <int>] [-NoFillHeight] [<CommonParameters>]
```

## DESCRIPTION
Children fill the grid row by row. When columns would become narrower than 24 cells the grid uses fewer columns.

## EXAMPLES

### EXAMPLE 1
```powershell
$layoutGridOptions = @{
    Columns = 3
    Content = {
        $chartOptions3 = @{
            WidgetId = 'a'
            Type = 'Sparkline'
        }
        New-ConsoleChart @chartOptions3;
        $chartOptions2 = @{
            WidgetId = 'b'
            Type = 'Sparkline'
        }
        New-ConsoleChart @chartOptions2;
        $chartOptions = @{
            WidgetId = 'c'
            Type = 'Sparkline'
        }
        New-ConsoleChart @chartOptions
    }
}
New-ConsoleLayoutGrid @layoutGridOptions
```

Three charts in a row

### EXAMPLE 2
```powershell
$layoutGridOptions = @{
    Columns = 2
    Content = {
        New-ConsolePanel -Title 'Inventory' -Content {
            $textOptions2 = @{
                Text = @('2 servers')
            }
            New-ConsoleText @textOptions2
        }
        New-ConsolePanel -Title 'Status' -Content {
            $textOptions = @{
                Text = @('Online')
                Role = 'Pass'
            }
            New-ConsoleText @textOptions
        }
    }
}
New-ConsoleLayoutGrid @layoutGridOptions | Show-Console
```

Arrange an inventory count beside its status

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
Default value: 1
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
Default value: 2
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
Default value: 24
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
Default value: $false
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
Default value: 0
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

- [https://consolemonster.dev/powershell/commands/new-consolelayoutgrid/](https://consolemonster.dev/powershell/commands/new-consolelayoutgrid/)
