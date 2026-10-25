---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleBarChart
## SYNOPSIS
Creates a horizontal bar chart, or a stacked bar with -Stacked.

Bars come from -InputObject (labels mapped to numbers), from a script block of New-ConsoleBarItem calls (when a bar needs its own tone) or from the pipeline. A stacked bar splits one bar into proportional segments with a legend showing values and shares. -Role sets the colour of every bar as a theme role (Fail, Warn, Pass, Accent, Series1 to Series6...); -Warning and -Critical colour bars by threshold instead. To add bars later, pipe the chart to Add-ConsoleItem; in a live dashboard, update values with Add-ConsoleValue or Set-ConsoleValue.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleBarChart [[-WidgetId] <string>] [[-InputObject] <psobject>] [[-Content] <scriptblock>] [-Stacked] [-Unit <string>] [-Shape <BarShape>] [-Role <ThemeToken>] [-Maximum <Double>] [-Warning <Double>] [-Critical <Double>] [-ValueColumn <ColumnFormat>] [-CountColumn <ColumnFormat>] [-PercentColumn <ColumnFormat>] [-LabelColumn <ColumnFormat>] [<CommonParameters>]
```

## DESCRIPTION
Creates a horizontal bar chart, or a stacked bar with -Stacked.

Bars come from -InputObject (labels mapped to numbers), from a script block of New-ConsoleBarItem calls (when a bar needs its own tone) or from the pipeline. A stacked bar splits one bar into proportional segments with a legend showing values and shares. -Role sets the colour of every bar as a theme role (Fail, Warn, Pass, Accent, Series1 to Series6...); -Warning and -Critical colour bars by threshold instead. To add bars later, pipe the chart to Add-ConsoleItem; in a live dashboard, update values with Add-ConsoleValue or Set-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleBarChart -Unit ' ms' -InputObject ([ordered] @{ Cloudflare = 12; Google = 18; Quad9 = 24 }) | Show-Console
```

Resolver latency

### EXAMPLE 2
```powershell
New-ConsoleBarChart -Role Fail -InputObject ([ordered] @{ 'Domain controllers' = 14; 'Group Policy' = 9; 'DNS' = 4 }) | Show-Console
```

Failures per area, drawn in the fail colour

### EXAMPLE 3
```powershell
New-ConsoleBarChart -Unit '%' -Warning 80 -Critical 90 -InputObject ([ordered] @{ 'C:' = 93; 'D:' = 34; 'E:' = 81 }) | Show-Console
```

Disk use with thresholds: 80 % and more is a warning, 90 % and more critical

### EXAMPLE 4
```powershell
New-ConsoleBarChart -Stacked -Content {
    New-ConsoleBarItem 'Passed' 412 -Role Pass
    New-ConsoleBarItem 'Warnings' 51 -Role Warn
    New-ConsoleBarItem 'Failed' 23 -Role Fail
} | Show-Console
```

Results split, each segment in its status colour

## PARAMETERS

### -Content
A script block that emits the bars or segments: New-ConsoleBarItem calls or hashtables with Label, Value and Tone.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CountColumn
Shared CountColumn settings from New-ConsoleColumnFormat.

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

### -Critical
The value from which a bar turns to the fail colour.

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

### -InputObject
Bars from the pipeline: New-ConsoleBarItem objects or hashtables with Label, Value and Tone.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -LabelColumn
Shared LabelColumn settings from New-ConsoleColumnFormat.

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

### -Maximum
The value that fills a whole bar (default: the largest value).

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

### -PercentColumn
Shared PercentColumn settings from New-ConsoleColumnFormat.

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

### -Role
The colour of the bars as a theme role: Accent, Pass, Fail, Warn, Info, Muted or Series1 to Series6 (default Series1). A New-ConsoleBarItem -Role overrides it for one bar.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

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

### -ValueColumn
Shared ValueColumn settings from New-ConsoleColumnFormat.

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

### -Warning
The value from which a bar turns to the warning colour (with -Critical below it, lower values are worse, as for free space).

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

### -WidgetId
The live name (default bars, or stacked with -Stacked).

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

- `ConsoleMonster.Widgets.BarChart`
- `ConsoleMonster.Widgets.StackedBar`

## RELATED LINKS

- None
