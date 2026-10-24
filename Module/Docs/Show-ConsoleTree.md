---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleTree
## SYNOPSIS
Shows a hierarchy from objects, dictionaries or explicit tree nodes.

Use ChildrenProperty or Children for a hierarchy, Content for New-ConsoleTreeNode definitions, or InputObject for a nested dictionary or object graph. Object inspection is bounded by Depth, MaxItems and MaxNodes. Show-ConsoleObject provides the keyboard explorer.

## SYNTAX
### Nodes (Default)
```powershell
Show-ConsoleTree [[-Title] <Object>] [[-Content] <scriptblock>] [-Include <string[]>] [-Exclude <string[]>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [<CommonParameters>]
```

### Objects
```powershell
Show-ConsoleTree [[-Title] <Object>] -InputObject <psobject> [-Include <string[]>] [-Exclude <string[]>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ChildrenProperty <string>] [-Children <scriptblock>] [-LabelProperty <string>] [-StatusProperty <string>] [-DetailProperty <string>] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [<CommonParameters>]
```

## DESCRIPTION
Shows a hierarchy from objects, dictionaries or explicit tree nodes.

Use ChildrenProperty or Children for a hierarchy, Content for New-ConsoleTreeNode definitions, or InputObject for a nested dictionary or object graph. Object inspection is bounded by Depth, MaxItems and MaxNodes. Show-ConsoleObject provides the keyboard explorer.

## EXAMPLES

### EXAMPLE 1
```powershell
$configuration | Show-ConsoleTree -Depth 4
```

Display an object graph.

### EXAMPLE 2
```powershell
Show-ConsoleTree 'Checks' -Content { New-ConsoleTreeNode 'DNS' -Status Pass }
```

Show explicit nodes.

## PARAMETERS

### -AsString
Returns one plain-text string without drawing.

```yaml
Type: SwitchParameter
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the tree widget without drawing.

```yaml
Type: SwitchParameter
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Children
A script block that returns an object's children; $_ is the object. Use it when children have to be looked up (Get-ChildItem, Get-ADGroupMember).

```yaml
Type: ScriptBlock
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChildrenProperty
The property that holds an object's children (for example Members or Children).

```yaml
Type: String
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
A script block that emits the nodes: New-ConsoleTreeNode calls, or strings for leaves (shown literally).

```yaml
Type: ScriptBlock
Parameter Sets: Nodes
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Culture
Culture used for values.

```yaml
Type: CultureInfo
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Levels of children to follow: New-ConsoleTree defaults to 5. Show-ConsoleTree defaults to 3 for object graphs, or one for wide .NET objects when Depth is omitted.

```yaml
Type: Int32
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DetailProperty
The property shown as a muted note after the label.

```yaml
Type: String
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exclude
Member names to omit without reading their getters.

```yaml
Type: String[]
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Include
Member names to include when inspecting an object graph.

```yaml
Type: String[]
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Hierarchical objects (pipeline input); each becomes a top-level node. Nodes from New-ConsoleTreeNode go in as they are.

```yaml
Type: PSObject
Parameter Sets: Objects
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -LabelProperty
The property shown as the node label (default: the object's name, as tables title a record: Name, DisplayName, Title...).

```yaml
Type: String
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxItems
The most children listed per node, and objects listed at the top, before "+N more" (default 25; 0 lists all).

```yaml
Type: Int32
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxNodes
The most nodes the tree may grow to (default 5000; 0 for no limit); beyond it a "node limit reached" note ends the walk.

```yaml
Type: Int32
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the original pipeline objects after drawing.

```yaml
Type: SwitchParameter
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusProperty
The property whose value is the node status (pass, fail, warn, skip... or PowerShell's words).

```yaml
Type: String
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The root label (markup allowed; rich text from New-ConsoleText keeps its colours). Without it the top-level nodes are the roots.

```yaml
Type: Object
Parameter Sets: Nodes, Objects
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Layout width in cells; zero resolves the current target, falling back to 100.

```yaml
Type: Int32
Parameter Sets: Nodes, Objects
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

- `None`

## RELATED LINKS

- None
