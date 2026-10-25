---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleFlow
## SYNOPSIS
Builds a native flow strip of nodes, labelled edges and one side branch.

Emit nodes first, then edges and an optional branch. Pipeline rows use Kind (Node, Edge or Branch) and the item command's property names. Labels are literal. The strip becomes vertical in a narrow terminal.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFlow [[-WidgetId] <string>] [[-Content] <scriptblock>] [-InputObject <psobject>] [<CommonParameters>]
```

## DESCRIPTION
Builds a native flow strip of nodes, labelled edges and one side branch.

Emit nodes first, then edges and an optional branch. Pipeline rows use Kind (Node, Edge or Branch) and the item command's property names. Labels are literal. The strip becomes vertical in a narrow terminal.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleFlow -Content {
    New-ConsoleFlowNode -Key leaf -Label 'Leaf' -Status Pass
    New-ConsoleFlowNode -Key issuer -Label 'Intermediate' -Status Fail
    New-ConsoleFlowEdge -Key chain -From leaf -To issuer -Label 'expired' -Status Fail -Cut
}
```

A certificate path with a broken connection

## PARAMETERS

### -Content
Emits New-ConsoleFlowNode, New-ConsoleFlowEdge and New-ConsoleFlowBranch items.

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

### -InputObject
Items or plain rows with Kind and the corresponding item properties.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -WidgetId
Identifier used by Update-ConsoleFlow (default flow).

```yaml
Type: String
Parameter Sets: __AllParameterSets
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

- `ConsoleMonster.Widgets.Flow`

## RELATED LINKS

- None
