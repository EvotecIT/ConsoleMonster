---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTree
## SYNOPSIS
Creates a console tree from nested New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of New-ConsoleTreeNode calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -NameProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -Data maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

## SYNTAX
### Nodes (Default)
```powershell
New-ConsoleTree [[-Title] <Object>] [[-Content] <scriptblock>] [<CommonParameters>]
```

### Data
```powershell
New-ConsoleTree [-Data] <IDictionary> [-Title <Object>] [<CommonParameters>]
```

### Objects
```powershell
New-ConsoleTree [[-Title] <Object>] -InputObject <psobject> [-ChildrenProperty <string>] [-Children <scriptblock>] [-NameProperty <string>] [-StatusProperty <string>] [-DetailProperty <string>] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates a console tree from nested New-ConsoleTreeNode calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of New-ConsoleTreeNode calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -NameProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -Data maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTree '[bold]evotec.xyz[/]' {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'SPF' -Status Warn -Detail '9 of 10 DNS lookups' {
        'include:spf.protection.outlook.com'
        'include:_spf.salesforce.com'
    }
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
} | Write-ConsoleWidget
```

A mail domain with records and their state

### EXAMPLE 2
```powershell
Get-Item C:\Support\Reports | New-ConsoleTree -Children { if ($_.PSIsContainer) { Get-ChildItem $_.FullName } } -Depth 3 | Write-ConsoleWidget
```

Folders and files from the pipeline, three levels deep

### EXAMPLE 3
```powershell
$groups = @(
    [pscustomobject] @{ Name = 'Domain Admins'; Health = 'Warn'; Note = '6 members'; Members = @(
        [pscustomobject] @{ Name = 'Tier 0 operators'; Health = 'Pass'; Members = @([pscustomobject] @{ Name = 'anna.k' }) }
        [pscustomobject] @{ Name = 'svc-backup'; Health = 'Fail'; Note = 'password never expires' }
    ) }
)
$groups | New-ConsoleTree 'Privileged groups' -ChildrenProperty Members -StatusProperty Health -DetailProperty Note | Write-ConsoleWidget
```

Hierarchical objects with a status per node

### EXAMPLE 4
```powershell
New-ConsoleTree -Title 'evotec.xyz' -Data ([ordered] @{ NS = @('dana.ns.cloudflare.com', 'kirk.ns.cloudflare.com'); MX = [ordered] @{ 'mail.protection.outlook.com' = '52.101.68.17' } }) | Write-ConsoleWidget
```

A DNS delegation from nested hashtables

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

- `ConsoleMonster.Widgets.Tree`

## RELATED LINKS

- None
