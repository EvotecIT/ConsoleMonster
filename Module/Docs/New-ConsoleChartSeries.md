---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleChartSeries
## SYNOPSIS
Creates one series of a line or scatter chart: a name, values and an optional tone.

Use it inside the script block of New-ConsoleLineChart. Values come from -Value or from the pipeline, oldest first, and are spaced evenly. -Role takes a theme role (Accent, Accent2, Pass, Fail, Warn, Series1 to Series6); without it the chart picks the next series colour, and a series named with a status word (failed, passed, warnings) takes that status colour. To add a series to a chart made earlier, pipe the chart to Add-ConsoleItem; in a running dashboard, append values with Add-ConsoleValue -Key.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleChartSeries [-Key] <Object> [[-Value] <double[]>] [-Role <ThemeToken>] [-Label <Object>] [<CommonParameters>]
```

## DESCRIPTION
Creates one series of a line or scatter chart: a name, values and an optional tone.

Use it inside the script block of New-ConsoleLineChart. Values come from -Value or from the pipeline, oldest first, and are spaced evenly. -Role takes a theme role (Accent, Accent2, Pass, Fail, Warn, Series1 to Series6); without it the chart picks the next series colour, and a series named with a status word (failed, passed, warnings) takes that status colour. To add a series to a chart made earlier, pipe the chart to Add-ConsoleItem; in a running dashboard, append values with Add-ConsoleValue -Key.

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

Two series computed in the script

### EXAMPLE 2
```powershell
New-ConsoleLineChart -Scatter -Unit 'ms' -Content {
    Get-Content latency.txt | New-ConsoleChartSeries 'latency'
} | Show-Console
```

Values from the pipeline

## PARAMETERS

### -Key
The series name, shown in the legend (rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
Visible legend text; defaults to the series key.

```yaml
Type: Object
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
The colour as a theme role: Accent, Accent2, Pass, Fail, Warn, Info, Muted or Series1 to Series6.

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

### -Value
The values, oldest first (pipeline input works too).

```yaml
Type: Double[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Double[]`

## OUTPUTS

- `ConsoleMonster.Widgets.ChartSeries`

## RELATED LINKS

- None
