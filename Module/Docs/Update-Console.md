---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/update-console/
schema: 2.0.0
---
# Update-Console
## SYNOPSIS
Updates a live widget or a caller-managed run through one validated operation.

## SYNTAX
### Fields (Default)
```powershell
Update-Console [-WidgetId] <string> [-Session <LiveSession>] [-Key <string>] [-Label <Object>] [-Detail <Object>] [-Value <Object>] [-Total <double>] [-Increment <double>] [-Status <StatusKind>] [-Result <StatusKind>] [-ParentKey <string>] [-RemainingSeconds <double>] [-Counter <IDictionary>] [-Restart] [-Complete] [<CommonParameters>]
```

### Append
```powershell
Update-Console [-WidgetId] <string> -Value <Object> -Append [-Session <LiveSession>] [-Key <string>] [<CommonParameters>]
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

### Row
```powershell
Update-Console [-WidgetId] <string> -Row <Object> [-Session <LiveSession>] [<CommonParameters>]
```

### Content
```powershell
Update-Console [-WidgetId] <string> -Content <Object> [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
For a dashboard, pass the WidgetId assigned when the widget was built. Inside Invoke-ConsoleDashboard the command uses the current session. To update a caller-managed run, pass the handle returned by Start-ConsoleRun.

The selected parameter set determines what the update addresses. Key identifies a row for RemoveRow. Row takes a record and projects it through the live table's KeyProperty and columns. Index is an integer array in both the Cells and Run sets. The Cells set applies one status to all the specified zero-based cell positions. The Run set uses exactly one entry in that array as the zero-based item position in the run's input.

Fields are checked against the resolved widget before any events are posted. Unknown targets and unsupported fields are terminating errors.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboardOptions = @{
    Title = 'Build'
    Content = {
        New-ConsoleProgress -WidgetId 'build' -Total 20
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$invokeOptions = @{
    Dashboard = $dashboard
    ScriptBlock = {
        $update = @{
            WidgetId = 'build'
            Value = 5
            Label = 'Compile'
        }
        Update-Console @update
    }
}
Invoke-ConsoleDashboard @invokeOptions
```

Advance a progress widget and change its label. The WidgetId in the update matches the WidgetId on New-ConsoleProgress. Value sets the absolute progress to 5 out of the 20 steps already configured on the widget. The dashboard runner supplies the session.

### EXAMPLE 2
```powershell
$dashboardOptions = @{
    Title = 'Results'
    Content = {
        $tableOptions = @{
            WidgetId = 'results'
            Live = $true
            Property = @('Server', 'State', 'Ms')
            KeyProperty = 'Server'
        }
        New-ConsoleTable @tableOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$invokeOptions = @{
    Dashboard = $dashboard
    ScriptBlock = {
        $update = @{
            WidgetId = 'results'
            Row = [pscustomobject]@{ Server = 'web01'; State = 'Running'; Ms = 0 }
        }
        Update-Console @update

        $update.Row = [pscustomobject]@{ Server = 'web01'; State = 'Pass'; Ms = 12 }
        Update-Console @update
    }
}
Invoke-ConsoleDashboard @invokeOptions
```

Add a live table row, then replace it. Property declares the display columns in order. KeyProperty selects Server as the stable row identity. The two Row records both have Server set to web01, so the second update changes that row to Pass with 12 milliseconds.

### EXAMPLE 3
```powershell
$dashboardOptions = @{
    Title = 'Checks'
    Content = {
        $mapOptions = @{
            WidgetId = 'checks'
            Count = 4
            Columns = 2
            RowLabels = @('web01', 'web02')
        }
        New-ConsoleCellMap @mapOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$invokeOptions = @{
    Dashboard = $dashboard
    ScriptBlock = {
        $update = @{
            WidgetId = 'checks'
            Index = @(0, 1)
            Status = 'Pass'
        }
        Update-Console @update
    }
}
Invoke-ConsoleDashboard @invokeOptions
```

Set two cells in a dashboard to Pass. This example uses the grid arrangement of a cell map, as in the parallel-patch gallery demo. A four-cell map with two columns places zero-based positions 0 and 1 in the first row. The update applies the same status to both cells and leaves the second row unchanged.

### EXAMPLE 4
```powershell
$items = @(
    [pscustomobject]@{ Name = 'Report' }
)
$runOptions = @{
    InputObject = $items
    Label = 'Name'
    NoLive = $true
}
$run = Start-ConsoleRun @runOptions
$update = @{
    Run = $run
    Index = @(0)
    Status = 'Pass'
    Detail = 'Uploaded'
}
Update-Console @update
Complete-ConsoleRun -Run $run
```

Finish one caller-managed run item. Start-ConsoleRun receives an array containing one input record. The Run parameter set of Update-Console requires a one-element integer array for Index. Its value, 0, addresses the first input record, Report. The example records the result of a simulated upload and then completes the run.

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
Default value: $false
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
Default value: $false
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
Default value: 0
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
Default value: $false
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
Default value: $false
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
Default value: 1
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
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
Detail text replacing the current message on the addressed live item.

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
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
Array of zero-based positions. In the Cells set, each entry identifies a cell to receive Status. In the Run set, supply exactly one entry to identify an item by its position in the run input. Use @(0) for the first item.

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
The parent activity key, or the phase an item belongs to in a phase strip (Key is then the item).

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
Default value: 0
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
Default value: $false
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
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Result
The result of completed lane steps.

```yaml
Type: StatusKind
Parameter Sets: Fields
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: Pending
Accept pipeline input: False
Accept wildcard characters: False
```

### -Row
A record with the live table's column properties (keyed by its KeyProperty), or a Key and Values descriptor that retains typed cells.

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
Parameter Sets: Fields, Append, RemoveRow, ClearRows, Cells, AppendCells, ResizeCells, FlowNode, FlowEdge, Row, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The replacement live status, such as Running, Pass, Warn or Fail.

```yaml
Type: StatusKind
Parameter Sets: Fields, Cells, AppendCells, FlowNode, FlowEdge, Run
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: Pending
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
Default value: 0
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
Parameter Sets: Fields, Append, RemoveRow, ClearRows, Cells, AppendCells, ResizeCells, FlowNode, FlowEdge, Row, Content
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

- [https://consolemonster.dev/powershell/commands/update-console/](https://consolemonster.dev/powershell/commands/update-console/)
