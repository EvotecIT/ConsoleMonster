---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolediagram/
schema: 2.0.0
---
# New-ConsoleDiagram
## SYNOPSIS
Builds a diagram from a script block or connection rows.

## SYNTAX
### Rows (Default)
```powershell
New-ConsoleDiagram [[-Content] <scriptblock>] [-InputObject <psobject>] [-Title <string>] [-Direction <DiagramDirection>] [-WidgetId <string>] [-From <Object>] [-To <Object>] [-Label <Object>] [-Status <Object>] [-Group <Object>] [-NoSummary] [-ShowLegend <bool>] [-ShowStepPanel <bool>] [-ShowDetails <bool>] [-ShowKeys] [-MaxPayloadLines <int>] [-MaxStepLines <int>] [<CommonParameters>]
```

## DESCRIPTION
Emit New-ConsoleDiagramNode, Link, Group and UseCase objects in Content. A node's label is its id unless Id is given. Pipeline rows use From, To, Label, Status and Group; selectors accept property names or script blocks using $_. Connections create undeclared endpoints. Graphics is optional for C#; the module includes it and registers its renderer.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDiagram -Title 'Home' { New-ConsoleDiagramNode Laptop -To Router; New-ConsoleDiagramNode Router -Kind Gateway } | Show-Console
```

A home network.

### EXAMPLE 2
```powershell
$rows | New-ConsoleDiagram -Title 'Replication' -From Source -To Destination
```

Connection rows.

## PARAMETERS

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

### -Direction
The layout direction.

```yaml
Type: DiagramDirection
Parameter Sets: Rows
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
Parameter Sets: Rows
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
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: 7
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoSummary
Hides the full-text attention list.

```yaml
Type: SwitchParameter
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowDetails
Shows focused-node details.

```yaml
Type: Boolean
Parameter Sets: Rows
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
Parameter Sets: Rows
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
Parameter Sets: Rows
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
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: $true
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

### -Title
The diagram title.

```yaml
Type: String
Parameter Sets: Rows
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
Parameter Sets: Rows
Aliases: None
Possible values:

Required: False
Position: named
Default value: diagram
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Diagram`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolediagram/](https://consolemonster.dev/powershell/commands/new-consolediagram/)
