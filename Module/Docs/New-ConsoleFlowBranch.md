---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleFlowBranch
## SYNOPSIS
Creates the single side branch below a flow strip.

Builds the single side branch for New-ConsoleFlow or Add-ConsoleItem. From identifies its source node. Omit To to end below that node, or name a later node to return to the strip. Label explains the branch using literal text.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFlowBranch [-Label] <string> -From <string> [-To <string>] [-Status <StatusKind>] [<CommonParameters>]
```

## DESCRIPTION
Creates the single side branch below a flow strip.

Builds the single side branch for New-ConsoleFlow or Add-ConsoleItem. From identifies its source node. Omit To to end below that node, or name a later node to return to the strip. Label explains the branch using literal text.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleFlowBranch -From issuer -Label 'expired 2026-09-30' -Status Fail
```

An expiry reason below the issuer

## PARAMETERS

### -From
Source node key.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
Literal branch label.

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

### -Status
Optional branch status.

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

### -To
Optional later node to return to; omitted to end the branch.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.FlowBranch`

## RELATED LINKS

- None
