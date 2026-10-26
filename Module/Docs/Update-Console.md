---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Update-Console
## SYNOPSIS
Updates a live widget or a caller-managed run through one validated operation.

WidgetId is position zero. Fields are checked against the resolved widget before any events are posted. Structural sets separate table rows, cell batches, content replacement and run items. Unknown targets and unsupported fields are terminating errors.

## SYNTAX
### Fields (Default)
```powershell
Update-Console [-WidgetId] <string> [-Session <LiveSession>] [-Key <string>] [-Label <Object>] [-Detail <Object>] [-Value <Object>] [-Total <double>] [-Increment <double>] [-Status <StatusKind>] [-Result <StatusKind>] [-ParentKey <string>] [-RemainingSeconds <double>] [-Counter <IDictionary>] [-Restart] [-Complete] [<CommonParameters>]
```

### Append
```powershell
Update-Console [-WidgetId] <string> -Value <Object> -Append [-Session <LiveSession>] [-Key <string>] [<CommonParameters>]
```

### Row
```powershell
Update-Console [-WidgetId] <string> -Row <Object> [-Session <LiveSession>] [<CommonParameters>]
```

### RemoveRow
```powershell
Update-Console [-WidgetId] <string> -Key <string> -RemoveRow [-Session <LiveSession>] [<CommonParameters>]
```

### ClearRows
```powershell
Update-Console [-WidgetId] <string> -ClearRows [-Session <LiveSession>] [<CommonParameters>]
```

### Cells
```powershell
Update-Console [-WidgetId] <string> -Status <StatusKind> -Index <int[]> [-Session <LiveSession>] [<CommonParameters>]
```

### AppendCells
```powershell
Update-Console [-WidgetId] <string> -AppendCells [-Session <LiveSession>] [-Status <StatusKind>] [-Count <int>] [<CommonParameters>]
```

### ResizeCells
```powershell
Update-Console [-WidgetId] <string> -CellCount <int> [-Session <LiveSession>] [<CommonParameters>]
```

### Content
```powershell
Update-Console [-WidgetId] <string> -Content <Object> [-Session <LiveSession>] [<CommonParameters>]
```

### FlowNode
```powershell
Update-Console [-WidgetId] <string> -Status <StatusKind> -NodeKey <string> [-Session <LiveSession>] [<CommonParameters>]
```

### FlowEdge
```powershell
Update-Console [-WidgetId] <string> -Status <StatusKind> -EdgeKey <string> [-Session <LiveSession>] [-Cut] [<CommonParameters>]
```

### Run
```powershell
Update-Console [-Run] <LiveRun> -Status <StatusKind> -Index <int[]> [-Label <Object>] [-Detail <Object>] [-DurationSeconds <Double>] [<CommonParameters>]
```

## DESCRIPTION
Updates a live widget or a caller-managed run through one validated operation.

WidgetId is position zero. Fields are checked against the resolved widget before any events are posted. Structural sets separate table rows, cell batches, content replacement and run items. Unknown targets and unsupported fields are terminating errors.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-Console build -Value 5 -Total 20 -Label 'Compile'
```

Advance a build and change its label

### EXAMPLE 2
```powershell
Update-Console -Run $run -Index 0 -Status Pass -Detail 'Uploaded'
```

Finish one run item

## PARAMETERS

### -Append
Appends Value as a chart sample.

```yaml
Type: SwitchParameter
Parameter Sets: Append
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AppendCells
Appends outcome cells.

```yaml
Type: SwitchParameter
Parameter Sets: AppendCells
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CellCount
The new cell count, at least one.

```yaml
Type: Int32
Parameter Sets: ResizeCells
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ClearRows
Clears all live table rows.

```yaml
Type: SwitchParameter
Parameter Sets: ClearRows
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Complete
Finishes the activity or lane.

```yaml
Type: SwitchParameter
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
The replacement widget or a script block producing widgets; null clears the region.

```yaml
Type: Object
Parameter Sets: Content
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Count
The number of appended cells; default one.

```yaml
Type: Int32
Parameter Sets: AppendCells
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Counter
Summary labels mapped to nonnegative integer counts.

```yaml
Type: IDictionary
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Cut
Severs the edge; Cut:$false reconnects it. Omission preserves connectivity.

```yaml
Type: SwitchParameter
Parameter Sets: FlowEdge
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
The replacement detail.

```yaml
Type: Object
Parameter Sets: Fields, Run
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DurationSeconds
The measured run duration in seconds.

```yaml
Type: Double
Parameter Sets: Run
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EdgeKey
The flow edge whose status or connectivity changes.

```yaml
Type: String
Parameter Sets: FlowEdge
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Increment
The additive change; cannot be combined with Value.

```yaml
Type: Double
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
Zero-based cells; the Run set requires exactly one index.

```yaml
Type: Int32[]
Parameter Sets: Cells, Run
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
The item key; RemoveRow requires it.

```yaml
Type: String
Parameter Sets: Fields, Append, RemoveRow
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The replacement label or current run phase.

```yaml
Type: Object
Parameter Sets: Fields, Run
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NodeKey
The flow node whose status changes.

```yaml
Type: String
Parameter Sets: FlowNode
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParentKey
The parent activity key.

```yaml
Type: String
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RemainingSeconds
The estimated remaining time, in seconds.

```yaml
Type: Double
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RemoveRow
Removes the row addressed by Key.

```yaml
Type: SwitchParameter
Parameter Sets: RemoveRow
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Restart
Restarts an indeterminate progress indicator.

```yaml
Type: SwitchParameter
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Result
The result of completed lane steps.

```yaml
Type: StatusKind
Parameter Sets: Fields
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Row
A row with Key and Values properties, retaining typed cells.

```yaml
Type: Object
Parameter Sets: Row
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Run
The caller-managed run handle.

```yaml
Type: LiveRun
Parameter Sets: Run
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The session, otherwise ConsoleSession or the current session.

```yaml
Type: LiveSession
Parameter Sets: Fields, Append, Row, RemoveRow, ClearRows, Cells, AppendCells, ResizeCells, Content, FlowNode, FlowEdge
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The new state.

```yaml
Type: StatusKind
Parameter Sets: Fields, Cells, AppendCells, FlowNode, FlowEdge, Run
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Total
The planned total; zero means unknown.

```yaml
Type: Double
Parameter Sets: Fields
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The absolute value or appended numeric sample.

```yaml
Type: Object
Parameter Sets: Fields, Append
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live widget identifier.

```yaml
Type: String
Parameter Sets: Fields, Append, Row, RemoveRow, ClearRows, Cells, AppendCells, ResizeCells, Content, FlowNode, FlowEdge
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
