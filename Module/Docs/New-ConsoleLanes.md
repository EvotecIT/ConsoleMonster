---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLanes
## SYNOPSIS
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane (names sharing one -Total), with a script block of New-ConsoleLane calls (each with its own total, progress, status and counts), from the pipeline, or appear on their first Update-ConsoleLane in a live dashboard. To add lanes later, pipe the widget to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLanes [[-WidgetId] <string>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-Lane <Object[]>] [-Total <double>] [-Shape <BarShape>] [-Column <string[]>] [-HideCompleted] [-CollapseWhenFinished] [-Transient] [-CountColumn <ColumnFormat>] [-PercentColumn <ColumnFormat>] [<CommonParameters>]
```

## DESCRIPTION
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Lanes can be declared up front with -Lane (names sharing one -Total), with a script block of New-ConsoleLane calls (each with its own total, progress, status and counts), from the pipeline, or appear on their first Update-ConsoleLane in a live dashboard. To add lanes later, pipe the widget to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLanes -WidgetId servers -Lane 'DC01', 'DC02', 'DC03' -Total 40
```

Lanes for three servers with 40 steps each

### EXAMPLE 2
```powershell
New-ConsoleLanes -Content {
    New-ConsoleLane 'DC01' -Total 40 -Done 40 -Status Pass -Passed 40
    New-ConsoleLane 'DC02' -Total 40 -Done 22 -Status Running -Passed 21 -Warned 1 -Detail 'replication'
    New-ConsoleLane 'DC03' -Total 40
} | Show-Console
```

Lanes at different points, printed once

## PARAMETERS

### -CollapseWhenFinished
Collapses the display to one summary line once every lane has finished.

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

### -Column
The columns of each row, in order: Standard (spinner, description, bar, percent, count, elapsed, remaining time, status), or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Without it the classic layout shows the label, bar, percentage, pass/fail/warning counts and detail.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Standard, Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results, Status

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
A script block that emits the lanes: New-ConsoleLane calls, or names.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountColumn
Shared CountColumn layout settings from New-ConsoleColumnFormat.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -HideCompleted
Finished lanes leave the display and are counted in the summary line.

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
Lanes from the pipeline: New-ConsoleLane objects or names.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Lane
Lanes to declare up front (key and label).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PercentColumn
Shared PercentColumn layout settings from New-ConsoleColumnFormat.

```yaml
Type: ColumnFormat
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
Bar shape: Line (thin stroke) or Block (solid blocks). Default Line.

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

### -Total
Planned steps per declared lane.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Transient
The display disappears once every lane has finished (with Invoke-ConsoleDashboard -Transient nothing is left).

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

### -WidgetId
The live name (default lanes). A script block here is taken as -Content.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Lanes`

## RELATED LINKS

- None
