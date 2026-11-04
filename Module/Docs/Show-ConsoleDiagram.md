---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleDiagram
## SYNOPSIS
Shows a diagram from connection rows, a Diagram widget or a ChartForgeX file, with use case playback.

Pipe connection rows with From, To, Label, Status and Group, or select other columns with property names or script blocks using $_. New-ConsoleDiagram builds the same widget from nodes, links, groups and use cases. Nodes keep their subtitle and live value on separate lines. A narrow view becomes a grouped wrapping tree, retaining every connection label. The Needs attention list includes each non-Pass node and link; -NoSummary hides it.

Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes or scroll a diagram taller than the viewport, and D shows the focused node's details. The app footer shows the exit key. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Errors in the selected diagram stop the command with the file line and diagnostic code. Errors in other visual fences and markup warnings are written as PowerShell warnings. -Path reads one file and cannot be combined with pipeline input; pipe connection rows without -Path.

## SYNTAX
### Rows (Default)
```powershell
Show-ConsoleDiagram [[-Content] <scriptblock>] [-Diagram <string>] [-Scenario <string>] [-Step <int>] [-Mode <InteractionMode>] [-Width <int>] [-StaticSnapshot] [-AsString] [-AsWidget] [-InputObject <psobject>] [-Title <string>] [-Direction <DiagramDirection>] [-WidgetId <string>] [-From <Object>] [-To <Object>] [-Label <Object>] [-Status <Object>] [-Group <Object>] [-NoSummary] [-ShowLegend <bool>] [-ShowStepPanel <bool>] [-ShowDetails <bool>] [-ShowKeys] [-MaxPayloadLines <int>] [-MaxStepLines <int>] [<CommonParameters>]
```

### Path
```powershell
Show-ConsoleDiagram [-Path] <string> [-Diagram <string>] [-Scenario <string>] [-Step <int>] [-Mode <InteractionMode>] [-Width <int>] [-StaticSnapshot] [-AsString] [-AsWidget] [-Title <string>] [-Direction <DiagramDirection>] [-WidgetId <string>] [-NoSummary] [-ShowLegend <bool>] [-ShowStepPanel <bool>] [-ShowDetails <bool>] [-ShowKeys] [-MaxPayloadLines <int>] [-MaxStepLines <int>] [<CommonParameters>]
```

## DESCRIPTION
Shows a diagram from connection rows, a Diagram widget or a ChartForgeX file, with use case playback.

Pipe connection rows with From, To, Label, Status and Group, or select other columns with property names or script blocks using $_. New-ConsoleDiagram builds the same widget from nodes, links, groups and use cases. Nodes keep their subtitle and live value on separate lines. A narrow view becomes a grouped wrapping tree, retaining every connection label. The Needs attention list includes each non-Pass node and link; -NoSummary hides it.

Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes or scroll a diagram taller than the viewport, and D shows the focused node's details. The app footer shows the exit key. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Errors in the selected diagram stop the command with the file line and diagnostic code. Errors in other visual fences and markup warnings are written as PowerShell warnings. -Path reads one file and cannot be combined with pipeline input; pipe connection rows without -Path.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleDiagram -Path ./ad-replication.md
```

Explore a diagram and its use cases with the keyboard.

### EXAMPLE 2
```powershell
Show-ConsoleDiagram -Path ./ad-replication.md -Scenario replicate -Step 6 -StaticSnapshot
```

Print one step of a use case without reading keys.

### EXAMPLE 3
```powershell
Show-ConsoleDiagram -Path ./tierbridge.md -Scenario blocked -Step 7 -Width 120 -AsString
```

Capture the diagram as plain text for a log or a ticket.

### EXAMPLE 4
```powershell
$rows | Show-ConsoleDiagram -Title 'Replication' -StaticSnapshot
```

Show connection rows using their standard columns.

### EXAMPLE 5
```powershell
$rows | Show-ConsoleDiagram -From Source -To Destination -Label { $_.Lag.ToString() + ' min' } -Status Health -Group Site -AsString
```

Select columns and calculate connection labels.

## PARAMETERS

### -AsString
Returns the static view as one plain-text string.

```yaml
Type: SwitchParameter
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the diagram widget without drawing it, for static display or live updates in Show-ConsoleApp layouts. The consumer must handle keys to enable diagram navigation and playback. Cannot be combined with -AsString or -StaticSnapshot.

```yaml
Type: SwitchParameter
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
Emits typed diagram nodes, links, groups and use cases.

```yaml
Type: ScriptBlock
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Diagram
The diagram id to show when the file has more than one architecture, topology or flow diagram.

```yaml
Type: String
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Direction
The layout direction.

```yaml
Type: DiagramDirection
Parameter Sets: Rows, Path
Aliases: None
Possible values: Right, Down, Left, Up

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -From
Source property name or script block; defaults to From.

```yaml
Type: Object
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Group
Source group property name or script block; defaults to Group.

```yaml
Type: Object
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Connection rows or an existing diagram.

```yaml
Type: PSObject
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Label
Connection label property name or script block; defaults to Label.

```yaml
Type: Object
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxPayloadLines
The maximum visible payload lines.

```yaml
Type: Int32
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxStepLines
The maximum visible step lines.

```yaml
Type: Int32
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mode
Rich runs the keyboard view. Line, Accessible, None and Snapshot print the static view.

```yaml
Type: InteractionMode
Parameter Sets: Rows, Path
Aliases: None
Possible values: Rich, Line, Accessible, None, Snapshot

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoSummary
Hides the full-text attention list.

```yaml
Type: SwitchParameter
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The Markdown or architecture file to read. A file without visual fences is read as one ChartForgeX architecture diagram.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Scenario
The use case id to select. Defaults to the diagram's first use case.

```yaml
Type: String
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowDetails
Shows focused-node details.

```yaml
Type: Boolean
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowKeys
Shows keyboard hints.

```yaml
Type: SwitchParameter
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowLegend
Shows status meanings below the topology.

```yaml
Type: Boolean
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowStepPanel
Shows use-case steps.

```yaml
Type: Boolean
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StaticSnapshot
Prints a static view without reading keys.

```yaml
Type: SwitchParameter
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
Health property name or script block; defaults to Status.

```yaml
Type: Object
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Step
The one-based step to highlight in the selected use case. Interactive views start there; static views show that step.

```yaml
Type: Int32
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The diagram title.

```yaml
Type: String
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -To
Destination property name or script block; defaults to To.

```yaml
Type: Object
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The identifier addressed by Update-Console.

```yaml
Type: String
Parameter Sets: Rows, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Width used by the static view; zero detects the host width.

```yaml
Type: Int32
Parameter Sets: Rows, Path
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Diagram`
- `System.String`

## RELATED LINKS

- None
