---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleTree
## SYNOPSIS
Writes a console tree from New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Same parameters as New-ConsoleTree (nodes in a script block, hierarchical objects from the pipeline with -ChildrenProperty or -Children, or -Data hashtables); the tree is written immediately instead of returned.

## SYNTAX
### Nodes
```powershell
Write-ConsoleTree [[-Title] <Object>] [[-Content] <scriptblock>] [<CommonParameters>]
```

### Data
```powershell
Write-ConsoleTree [-Data] <IDictionary> [-Title <Object>] [<CommonParameters>]
```

### Objects
```powershell
Write-ConsoleTree [[-Title] <Object>] -InputObject <psobject> [-ChildrenProperty <string>] [-Children <scriptblock>] [-NameProperty <string>] [-StatusProperty <string>] [-DetailProperty <string>] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes a console tree from New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Same parameters as New-ConsoleTree (nodes in a script block, hierarchical objects from the pipeline with -ChildrenProperty or -Children, or -Data hashtables); the tree is written immediately instead of returned.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleTree -Title 'Forest' -Data ([ordered] @{ 'ad.adatum.com' = @('DC01', 'DC02'); 'eu.ad.adatum.com' = @('DC07') })
```

Write a small hierarchy

### EXAMPLE 2
```powershell
Write-ConsoleTree 'Replication' {
    New-ConsoleTreeNode 'DC01' -Status Pass -Detail 'last sync 42s ago'
    New-ConsoleTreeNode 'DC02' -Status Warn -Detail 'last sync 42 min ago'
}
```

Nodes with a status and a detail

## PARAMETERS

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

### -Data
Nested hashtables describing the tree: a nested hashtable becomes child nodes, an array leaf nodes, any other value a muted note after the key.

```yaml
Type: IDictionary
Parameter Sets: Data
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
How many levels of children are followed below each object (default 5).

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

### -NameProperty
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
Parameter Sets: Nodes, Data, Objects
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

- `None`

## RELATED LINKS

- None
