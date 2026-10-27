---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleCellMap
## SYNOPSIS
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. -Columns and -RowLabels turn it into a labelled grid (one row per server, one column per check) with one legend below, which -LegendLines lets wrap. To add cells later, pipe the map to Add-ConsoleItem; in a live dashboard, update cells with Update-Console.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCellMap [[-WidgetId] <string>] [[-Status] <Object[]>] [-Count <int>] [-MaxRows <int>] [-NoLegend] [-Columns <int>] [-RowLabels <string[]>] [-LegendLines <int>] [-Gap <int>] [-Window <CellMap+CellWindow>] [-NoSpinRunning] [<CommonParameters>]
```

## DESCRIPTION
Creates a cell map: one status symbol per item of a run, with a legend of counts.

The map Invoke-ConsoleRun shows for many items, as a widget of its own: each item is one cell in its status colour (pass, fail, warn, skip, running, pending), wrapping to the width, followed by a legend with each state's count. States come from -Status or the pipeline, in the shared status vocabulary (PowerShell's words such as Success and Error work too). -Count adds pending cells for items not started yet. -Columns and -RowLabels turn it into a labelled grid (one row per server, one column per check) with one legend below, which -LegendLines lets wrap. To add cells later, pipe the map to Add-ConsoleItem; in a live dashboard, update cells with Update-Console.

## EXAMPLES

### EXAMPLE 1
```powershell
$states = @('Pass') * 28 + @('Fail') * 2 + @('Warn') + @('Running') * 4
New-ConsoleCellMap -Status $states -Count 40 | Show-Console
```

Forty items part-way through a run

### EXAMPLE 2
```powershell
$results | ForEach-Object Result | New-ConsoleCellMap | Show-Console
```

States from objects

### EXAMPLE 3
```powershell
New-ConsoleCellMap -WidgetId checks -Count 12 -Columns 4 -RowLabels 'SRV01', 'SRV02', 'SRV03' -LegendLines 2 -Status Pass, Pass, Fail, Warn, Pass | Show-Console
```

A grid of three servers by four checks, with the server names beside the rows

## PARAMETERS

### -Columns
The cells per row (0 = as many as fit). Set it to draw a grid, such as one row per server and one column per check; cells past the width are cut off rather than moved to the next row.

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

### -Count
The total number of items; cells beyond the given states are pending.

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

### -Gap
Space between cells; default one column.

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

### -LegendLines
The most lines the legend wraps onto when it is wider than the map (default 1: it is cut off instead).

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

### -MaxRows
The most rows of cells (0 = all); a last line counts the items not shown.

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

### -NoLegend
Hide the legend line with each state's count.

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

### -NoSpinRunning
Uses a fixed running marker instead of a spinner.

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

### -RowLabels
Labels drawn muted before the rows, one per row in order; the cells start after the widest label. With -Columns they make a labelled grid with one legend below it.

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

### -Status
The item states, in order: Pass, Fail, Warn, Skip, Info, Running or Pending (pipeline input works too).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -WidgetId
The live name (default cells).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Window
Chooses which cells remain visible when MaxRows limits the map.

```yaml
Type: CellWindow
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Head, Active

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

- `ConsoleMonster.Widgets.CellMap`

## RELATED LINKS

- None
