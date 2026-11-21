---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolediagram/
schema: 2.0.0
---
# Show-ConsoleDiagram
## SYNOPSIS
Shows a diagram from connection rows, a Diagram widget or a ChartForgeX file, with use case playback.

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
Pipe connection rows with From, To, Label, Status and Group, or select other columns with property names or script blocks using $_. New-ConsoleDiagram builds the same widget from nodes, links, groups and use cases. Path reads one Markdown file with ChartForgeX visual fences or one plain architecture file. Errors in the selected diagram stop the command with the file line and diagnostic code. Other visual-fence errors and markup warnings use the warning stream.

Interactive diagram
-------------------

The diagram shows boxes, routed connections, dashed groups and a legend. Nodes keep their subtitle and live value on separate lines. Use Up and Down to move between nodes or scroll a diagram taller than the viewport. Press D to inspect the focused node. The diagram starts on the first use case unless Scenario selects another. Left and Right step through its payloads and steps, Space plays it, and Tab switches use cases. The app footer shows the exit key.

Static diagram
--------------

Redirected, CI, line and non-interactive sessions, or StaticSnapshot, print one view at the selected step. A narrow view becomes a grouped wrapping tree, retaining the connection labels. The Needs attention list shows non-Pass nodes and links. Use NoSummary to hide it.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleDiagram -Path ./ad-replication.md
```

Explore a diagram and its use cases with the keyboard.

### EXAMPLE 2
```powershell
$diagramOptions = @{
    StaticSnapshot = $true
    Path = './ad-replication.md'
    Scenario = 'replicate'
    Step = 6
}
Show-ConsoleDiagram @diagramOptions
```

Print one step of a use case without reading keys.

### EXAMPLE 3
```powershell
$diagramOptions = @{
    AsString = $true
    Path = './tierbridge.md'
    Scenario = 'blocked'
    Step = 7
    Width = 120
}
Show-ConsoleDiagram @diagramOptions
```

Capture the diagram as plain text for a log or a ticket.

### EXAMPLE 4
```powershell
$diagramOptions = @{
    StaticSnapshot = $true
    Title = 'Replication'
}
$rows | Show-ConsoleDiagram @diagramOptions
```

Show connection rows using their standard columns.

### EXAMPLE 5
```powershell
$diagramOptions = @{
    AsString = $true
    From = 'Source'
    To = 'Destination'
    Label = { $_.Lag.ToString() + ' min' }
    Status = 'Health'
    Group = 'Site'
}
$rows | Show-ConsoleDiagram @diagramOptions
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
Default value: $false
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
Default value: $false
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
Default value: Right
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
Default value: 8
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
Default value: 7
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
Default value: $false
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
Default value: $true
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
Default value: $false
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
Default value: $true
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
Default value: $true
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
Default value: $false
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
Default value: 0
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
Default value: diagram
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
Default value: 0
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

- [https://consolemonster.dev/powershell/commands/show-consolediagram/](https://consolemonster.dev/powershell/commands/show-consolediagram/)
