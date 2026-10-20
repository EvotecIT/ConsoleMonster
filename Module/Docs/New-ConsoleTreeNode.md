---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTreeNode
## SYNOPSIS
Creates a tree node with an optional status, a muted detail and child nodes, for New-ConsoleTree.

Use it inside the script block of New-ConsoleTree, or inside another New-ConsoleTreeNode to nest. The label accepts markup or rich text from New-ConsoleText. -Status shows a status marker before the label (Pass, Fail, Warn, Skip, Info, Running, Pending, or PowerShell's words such as Success, Warning and Error). -Detail is a muted note after the label, and -Value a value formatted by a presenter (sizes, dates, durations). The script block emits the children: more New-ConsoleTreeNode calls, or strings for leaves (shown literally). To add nodes to a tree made earlier, pipe the tree to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTreeNode [-Label] <Object> [[-Content] <scriptblock>] [-Status <StatusKind>] [-Detail <Object>] [-Value <Object>] [-Presenter <string>] [-Unit <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a tree node with an optional status, a muted detail and child nodes, for New-ConsoleTree.

Use it inside the script block of New-ConsoleTree, or inside another New-ConsoleTreeNode to nest. The label accepts markup or rich text from New-ConsoleText. -Status shows a status marker before the label (Pass, Fail, Warn, Skip, Info, Running, Pending, or PowerShell's words such as Success, Warning and Error). -Detail is a muted note after the label, and -Value a value formatted by a presenter (sizes, dates, durations). The script block emits the children: more New-ConsoleTreeNode calls, or strings for leaves (shown literally). To add nodes to a tree made earlier, pipe the tree to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTree '[bold]evotec.xyz[/]' {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
} | Write-ConsoleWidget
```

A mail domain with records and their state

### EXAMPLE 2
```powershell
New-ConsoleTree 'Domain controllers' {
    New-ConsoleTreeNode 'ad.adatum.com' { 'DC01', 'DC02' }
    New-ConsoleTreeNode 'eu.ad.adatum.com' -Status Warn -Detail 'one offline' { 'DC07' }
} | Write-ConsoleWidget
```

Leaves from a list of names

### EXAMPLE 3
```powershell
New-ConsoleTree 'Reports share' {
    New-ConsoleTreeNode 'Weekly' -Value 734003200 -Presenter Bytes
    New-ConsoleTreeNode 'Archive' -Value 9663676416 -Presenter Bytes -Status Warn
} | Write-ConsoleWidget
```

A value shown through a presenter

## PARAMETERS

### -Content
A script block that emits the child nodes: New-ConsoleTreeNode calls, or strings for leaves (shown literally).

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
A muted note after the label, for example a count or a short reason (shown literally).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The label (markup allowed; rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Presenter
How -Value is shown: Bytes, Duration, Date, Number, Boolean, Status, Progress, DataBar, Link, Markup or Text.

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

### -Status
A status marker before the label: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success, Warning and Error work too).

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
A unit for -Value with the Number presenter, for example ms.

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

### -Value
A value after the label, formatted by -Presenter or by the presenter its type suggests (dates, sizes, durations, booleans).

```yaml
Type: Object
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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.TreeNode`

## RELATED LINKS

- None
