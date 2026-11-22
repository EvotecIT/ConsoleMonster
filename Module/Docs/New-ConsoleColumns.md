---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolecolumns/
schema: 2.0.0
---
# New-ConsoleColumns
## SYNOPSIS
Places console widgets side by side.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleColumns [[-Content] <Object>] [-Gap <int>] [-FixedWidth <int[]>] [-CollapseBelow <int>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Children share the width equally unless they were created with -Weight. -FixedWidth gives children a fixed width in cells instead, by position (a narrow histogram beside two tables). Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content: by default below the sum of the children's minimum widths, or below -CollapseBelow cells.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleColumns -Content {
    $panelOptions2 = @{
        Title = 'Steps'
        Weight = 2
        Content = { New-ConsoleStatusList -WidgetId steps }
    }
    New-ConsolePanel @panelOptions2;
    $panelOptions = @{
        Title = 'Info'
        Content = { New-ConsoleKeyValue -WidgetId info }
    }
    New-ConsolePanel @panelOptions
}
```

Two panels, the first twice as wide

### EXAMPLE 2
```powershell
$columnsOptions = @{
    FixedWidth = @(0, 0, 17)
    CollapseBelow = 90
    Content = {
        $panelOptions3 = @{
            Title = 'Failures'
            Weight = 1.5
            Content = {
                $tableOptions2 = @{
                    Live = $true
                    WidgetId = 'failures'
                    Property = @('Server', 'Check')
                }
                New-ConsoleTable @tableOptions2
            }
        }
        New-ConsolePanel @panelOptions3
        $panelOptions2 = @{
            Title = 'Slowest'
            Content = {
                $tableOptions = @{
                    Live = $true
                    WidgetId = 'slowest'
                    Property = @('Check', 'Took')
                }
                New-ConsoleTable @tableOptions
            }
        }
        New-ConsolePanel @panelOptions2
        $panelOptions = @{
            Title = 'Durations'
            Content = {
                $chartOptions = @{
                    WidgetId = 'durations'
                    Height = 5
                    Type = 'Histogram'
                    Edges = @(0, 2, 4, 8)
                }
                New-ConsoleChart @chartOptions
            }
        }
        New-ConsolePanel @panelOptions
    }
}
New-ConsoleColumns @columnsOptions
```

Keep the histogram at 17 cells and distribute the rest of the width between the two tables. FixedWidth is indexed in the order that Content emits its children: 0 leaves Failures and Slowest on their weighted shares, and 17 fixes Durations. Below 90 cells, CollapseBelow stacks the three children vertically.

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'disk-usage' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'event-log-summary' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleColumns in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

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
Default value: -1
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
Default value: $false
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
Default value: 1
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

- [https://consolemonster.dev/powershell/commands/new-consolecolumns/](https://consolemonster.dev/powershell/commands/new-consolecolumns/)
