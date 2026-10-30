---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleTree
## SYNOPSIS
Creates a console tree from nested Label/Children records calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of Label/Children records calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -LabelProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -InputObject maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

## SYNTAX
### Nodes (Default)
```powershell
New-ConsoleTree [[-Title] <Object>] [[-Content] <scriptblock>] [-WrapLabels] [-NoRoot] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Objects
```powershell
New-ConsoleTree [[-Title] <Object>] -InputObject <psobject> [-ChildrenProperty <string>] [-Children <scriptblock>] [-LabelProperty <string>] [-StatusProperty <string>] [-DetailProperty <string>] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [-WrapLabels] [-NoRoot] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

## DESCRIPTION
Creates a console tree from nested Label/Children records calls, hierarchical objects or nested hashtables.

Three ways in, one tree out. Nodes: a script block of Label/Children records calls, each with its own status, detail and children (strings become leaves shown literally); nodes can also be piped in. Objects: pipe hierarchical objects (groups and members, folders and files) and name their children with -ChildrenProperty or a -Children script block ($_ is the parent); -LabelProperty, -StatusProperty and -DetailProperty pick what each node shows, and -Depth, -MaxItems and -MaxNodes keep a large or cyclic hierarchy bounded and mark what was left out (a repeated item is shown once with "(cycle)"). Hashtables: -InputObject maps keys to nested hashtables (child nodes), arrays (leaves) or other values (a muted note); use [ordered] to keep order. The title (markup allowed) is the root; without it the top-level nodes are the roots.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTree '[bold]evotec.xyz[/]' -Content {
    [pscustomobject]@{ Label = 'MX'; Status = 'Pass'; Detail = '2 records'; Children = @(& {
        [pscustomobject]@{ Label = 'evotec-xyz.mail.protection.outlook.com'; Detail = 'priority 0' }
    }) }
    [pscustomobject]@{ Label = 'SPF'; Status = 'Warn'; Detail = '9 of 10 DNS lookups'; Children = @(& {
        'include:spf.protection.outlook.com'
        'include:_spf.salesforce.com'
    }) }
    [pscustomobject]@{ Label = 'DMARC'; Status = 'Fail'; Detail = 'p=none' }
} | Show-Console
```

A mail domain with records and their state

### EXAMPLE 2
```powershell
Get-Item . | New-ConsoleTree -Children { if ($_.PSIsContainer) { Get-ChildItem $_.FullName } } -Depth 3 | Show-Console
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
$groups | New-ConsoleTree 'Privileged groups' -ChildrenProperty Members -StatusProperty Health -DetailProperty Note | Show-Console
```

Hierarchical objects with a status per node

### EXAMPLE 4
```powershell
New-ConsoleTree -Title 'evotec.xyz' -InputObject ([ordered] @{ NS = @('dana.ns.cloudflare.com', 'kirk.ns.cloudflare.com'); MX = [ordered] @{ 'mail.protection.outlook.com' = '52.101.68.17' } }) | Show-Console
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
A script block that emits the nodes: Label/Children records calls, or strings for leaves (shown literally).

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

### -Depth
Levels of children to follow: New-ConsoleTree defaults to 5. Show-Console -View Tree defaults to 3 for object graphs, or one for wide .NET objects when Depth is omitted.

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
Hierarchical objects (pipeline input); each becomes a top-level node. Nodes from Label/Children records go in as they are.

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

### -NoRoot
Hides the default-on root when a title was supplied.

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

### -Presentation
Typed culture, time-zone and value presentation configuration.

```yaml
Type: PresentationOptions
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

### -WrapLabels
Wraps node labels and values with hanging indentation.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Tree`

## RELATED LINKS

- None
