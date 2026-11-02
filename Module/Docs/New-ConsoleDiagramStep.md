---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDiagramStep
## SYNOPSIS
Creates a request, message, reply or failed interaction.

From and To identify diagram nodes. Payload is a hashtable of fields shown with the active step. Reply and Failed are shorthand for Kind; choose one interaction kind.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDiagramStep [-From] <string> [-To] <string> [-Label] <string> [-Kind <DiagramStepKind>] [-Reply] [-Failed] [-Payload <hashtable>] [<CommonParameters>]
```

## DESCRIPTION
Creates a request, message, reply or failed interaction.

From and To identify diagram nodes. Payload is a hashtable of fields shown with the active step. Reply and Failed are shorthand for Kind; choose one interaction kind.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDiagramStep Web 'Orders API' 'create order' -Payload @{ sku = 'MUG-01'; qty = 2 }
```

A request payload.

## PARAMETERS

### -Failed
Marks a failed interaction.

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

### -From
The source node id.

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

### -Kind
Request, Message, Reply or Failed.

```yaml
Type: DiagramStepKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Request, Message, Reply, Failed

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The interaction text.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Payload
Named payload fields.

```yaml
Type: Hashtable
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Reply
Marks a reply.

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

### -To
The destination node id.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.DiagramStep`

## RELATED LINKS

- None
