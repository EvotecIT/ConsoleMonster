---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolechartview/
schema: 2.0.0
---
# New-ConsoleChartView
## SYNOPSIS
Makes a line chart explorable inside Show-ConsoleApp.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleChartView [-Key] <string> [[-Label] <string>] -Chart <Object> [-Height <int>] [-PointLabels <string[]>] [-LinkKey <string>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
Left/Right or a click moves a crosshair along X, and the row above the plot reads every shown series at that point; the app footer repeats it for screen readers. A number key or a click on a legend entry hides or shows a series. + and - or the wheel zoom along X, Shift+Left/Right or a drag pans, and Home shows everything. The state holds the index of the point under the crosshair. Give New-ConsoleDataGrid the same -LinkKey to link a table: selecting a row moves the crosshair and moving the crosshair selects the row. -Chart takes a Line or Scatter chart from New-ConsoleChart, or a ChartForgeX line chart, whose zoom uses ChartForgeX's prepared X levels.

## EXAMPLES

### EXAMPLE 1
```powershell
$samples = 1..30 | ForEach-Object { [pscustomobject]@{ Minute = $_; Cpu = 40 + (Get-Random -Maximum 50); Memory = 50 + $_ } }
$chart = New-ConsoleChart -Type Line -Unit '%' -Series @(
    @{ Key = 'cpu'; Values = $samples.Cpu }, @{ Key = 'memory'; Values = $samples.Memory })
Show-ConsoleApp -Content {
    New-ConsoleChartView -Key Load -Chart $chart -PointLabels $samples.Minute -LinkKey Minute
    New-ConsoleDataGrid -Key Samples -InputObject $samples -Property Minute, Cpu, Memory -Height 8 -LinkKey Minute
}
```

Explore CPU and memory samples next to the table they came from

### EXAMPLE 2
```powershell
Show-ConsoleApp -Content {
    New-ConsoleChartView -Key cpu -Chart (New-ConsoleChart -Type Line -Series @{ Key = 'cpu'; Values = 12, 25, 18, 31 })
}
```

Compose an inventory chart view

## PARAMETERS

### -Chart
A Line or Scatter chart from New-ConsoleChart, a ChartForgeX line chart with numeric X, or a ChartPlot.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Disabled
Disables editing and removes the input from keyboard focus traversal.

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

### -Height
Rows for the readout and the plot, legend included.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 10
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state and final hashtable.

```yaml
Type: String
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
Visible label; defaults to the name using the core control's labels.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LinkKey
The state key shared with linked controls (New-ConsoleDataGrid, New-ConsoleSelect, another chart): the index of the point under the crosshair. A position it cannot show is ignored.

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

### -OnChange
Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PointLabels
Labels for the points along X, such as times, read out with their values.

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

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

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

### -ValidationMessage
Initial validation text displayed below the input.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.ChartView`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolechartview/](https://consolemonster.dev/powershell/commands/new-consolechartview/)
