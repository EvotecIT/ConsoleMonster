---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLineChart
## SYNOPSIS
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

Series come from a script block of New-ConsoleChartSeries calls (each with its values and an optional tone), from the pipeline, or from -InputObject (series names mapped to arrays of values). Values are spaced evenly. -Series declares empty series for a live chart. To add a series later, pipe the chart to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue -Key series.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLineChart [[-WidgetId] <string>] [[-InputObject] <psobject>] [[-Content] <scriptblock>] [-Series <string[]>] [-Height <int>] [-Scatter] [-Unit <string>] [-Minimum <Double>] [-Maximum <Double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a line or scatter chart (braille on modern fonts, half blocks on Consolas, characters in ASCII mode).

Series come from a script block of New-ConsoleChartSeries calls (each with its values and an optional tone), from the pipeline, or from -InputObject (series names mapped to arrays of values). Values are spaced evenly. -Series declares empty series for a live chart. To add a series later, pipe the chart to Add-ConsoleItem; in a live dashboard, append values with Add-ConsoleValue -Key series.

## EXAMPLES

### EXAMPLE 1
```powershell
$completed = foreach ($i in 0..47) { 20 + 9 * [math]::Sin($i / 7) + $i % 3 }
$failed = foreach ($i in 0..47) { 3 + 2 * [math]::Cos($i / 5) }
New-ConsoleLineChart -Height 7 -Content {
    New-ConsoleChartSeries 'completed' -Value $completed -Role Accent
    New-ConsoleChartSeries 'failed' -Value $failed -Role Fail
} | Show-Console
```

Two series with their own tones

### EXAMPLE 2
```powershell
New-ConsoleLineChart -Height 8 -InputObject ([ordered] @{ cpu = 12, 30, 45, 38, 52; memory = 40, 41, 43, 47, 50 }) | Show-Console
```

Series from a hashtable

### EXAMPLE 3
```powershell
$samples = foreach ($i in 0..59) { 40 + ($i * 37) % 23 }
New-ConsoleLineChart -Scatter -Height 7 -Unit 'ms' -Content { New-ConsoleChartSeries 'latency' $samples -Role Accent2 } | Show-Console
```

A scatter of latency samples

## PARAMETERS

### -Content
A script block that emits the series: New-ConsoleChartSeries calls.

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

### -InputObject
Series from the pipeline: New-ConsoleChartSeries objects.

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

### -Maximum
The highest value on the axis (default: the largest value).

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

### -Minimum
The lowest value on the axis (default: the smallest value).

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

### -WidgetId
The live name (default chart).

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

- `ConsoleMonster.Widgets.LineChart`

## RELATED LINKS

- None
