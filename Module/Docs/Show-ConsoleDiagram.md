---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleDiagram
## SYNOPSIS
Shows a ChartForgeX architecture or topology diagram in the terminal, with use case playback.

Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes, D shows the focused node's details and Esc ends the view. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Markup errors stop the command with the file line and diagnostic code; warnings are written as PowerShell warnings.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleDiagram [-Path] <string> [-Diagram <string>] [-Scenario <string>] [-Step <int>] [-Mode <InteractionMode>] [-Width <int>] [-StaticSnapshot] [-AsString] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Shows a ChartForgeX architecture or topology diagram in the terminal, with use case playback.

Reads a Markdown file with ChartForgeX visual fences, or a plain architecture file without fences, and draws the diagram in terminal cells: boxes, routed connections, dashed groups and a legend. When the diagram has use cases, the first one is selected and its steps, payloads and keys are shown. In an interactive console, Left and Right step through the use case, Space plays it, Tab switches use cases, Up and Down move the focus between nodes, D shows the focused node's details and Esc ends the view. In redirected, CI, line and non-interactive sessions, or with -StaticSnapshot, one static view is printed at the chosen step. Markup errors stop the command with the file line and diagnostic code; warnings are written as PowerShell warnings.

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

## PARAMETERS

### -AsString
Returns the static view as one plain-text string.

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

### -AsWidget
Returns the diagram widget without drawing it, for Show-ConsoleApp layouts or live updates.

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

### -Diagram
The diagram id to show when the file has more than one architecture, topology or flow diagram.

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

### -Mode
Rich runs the keyboard view. Line, Accessible and None print the static view.

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None

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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Graphics.TopologyDiagram`
- `System.String`

## RELATED LINKS

- None
