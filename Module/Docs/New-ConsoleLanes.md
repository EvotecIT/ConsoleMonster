---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleLanes
## SYNOPSIS
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Declare lanes with names or Key/Label/Total records emitted by Content or the pipeline. Records can include progress, status and counts. A lane can also appear on its first Update-Console call in a live dashboard.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLanes [[-WidgetId] <string>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-Shape <BarShape>] [-Columns <Object[]>] [-HideCompleted] [-CollapseWhenFinished] [-Transient] [-CountColumn <ColumnFormat>] [-PercentColumn <ColumnFormat>] [-MaxLabelWidth <int>] [-NoDetail] [-CountSkipped] [-NoDimPending] [-BoldActive] [-ShowRunningCount] [-CollapseWhen <StatusKind>] [-RateWindowSeconds <double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a lanes widget: one row per worker, server or item of a parallel workload.

Each lane shows a status marker (a spinner while running), a progress bar, pass/fail/warning counts and what it is doing now. Declare lanes with names or Key/Label/Total records emitted by Content or the pipeline. Records can include progress, status and counts. A lane can also appear on its first Update-Console call in a live dashboard.

## EXAMPLES

### EXAMPLE 1
```powershell
'DC01', 'DC02', 'DC03' | ForEach-Object { @{ Key = $_; Label = $_; Total = 40 } } | New-ConsoleLanes -WidgetId servers
```

Lanes for three servers with 40 steps each

### EXAMPLE 2
```powershell
New-ConsoleLanes -Content {
    [pscustomobject]@{ Key = 'DC01'; Total = 40; Status = 'Pass'; Passed = 40; Value = 40 }
    [pscustomobject]@{ Key = 'DC02'; Total = 40; Status = 'Running'; Passed = 21; Warned = 1; Detail = 'replication'; Value = 22 }
    [pscustomobject]@{ Key = 'DC03'; Total = 40 }
} | Show-Console
```

Lanes at different points, printed once

## PARAMETERS

### -BoldActive
Emphasizes active lane labels.

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

### -CollapseWhen
Collapses lanes with this status into a summary.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

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

### -Columns
Builtin names, Type/Alignment/Priority/MaxWidth records or typed columns, in order: Standard (spinner, description, bar, percent, count, elapsed, remaining time, status), or any of Spinner, Description, Bar, Percent, Count, Elapsed, Remaining, Rate, ByteRate, Results and Status. Without it the classic layout shows the label, bar, percentage, pass/fail/warning counts and detail.

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

### -Content
A script block that emits the lanes: Key/Label records calls, or names.

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
Shared CountColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.

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

### -CountSkipped
Includes skipped work in the completed count.

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
Lanes from the pipeline: Key/Label records objects or names.

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

### -MaxLabelWidth
Maximum lane label width in cells; default 28.

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

### -NoDetail
Hides each lane's detail text.

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

### -NoDimPending
Uses normal text instead of dimming pending lanes.

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

### -PercentColumn
Shared PercentColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.

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

### -RateWindowSeconds
Rate-estimation window in seconds; default 10.

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

### -ShowRunningCount
Shows a separate count of running lanes.

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
