---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleFlowNode
## SYNOPSIS
Creates a labelled flow node with an optional status glyph and colour.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFlowNode [-Label] <string> -Key <string> [-Status <StatusKind>] [<CommonParameters>]
```

## DESCRIPTION
Creates a labelled flow node with an optional status glyph and colour.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleFlowNode -Key client -Label 'Client' -Status Pass
```


## PARAMETERS

### -Key
Stable node key.

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
Literal node label.

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
Optional node status.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.FlowNode`

## RELATED LINKS

- None
