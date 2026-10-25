---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleFlow
## SYNOPSIS
Updates a node or edge in a live dashboard's flow strip.

Posts a status change to an existing flow in a running dashboard. WidgetId selects the flow; NodeKey or EdgeKey selects its item. Cut severs an edge, Cut:$false reconnects it, and omitting Cut preserves its connection state.

## SYNTAX
### Node
```powershell
Update-ConsoleFlow -NodeKey <string> -Status <StatusKind> [-WidgetId <string>] [-Session <LiveSession>] [<CommonParameters>]
```

### Edge
```powershell
Update-ConsoleFlow -EdgeKey <string> -Status <StatusKind> [-WidgetId <string>] [-Cut] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Updates a node or edge in a live dashboard's flow strip.

Posts a status change to an existing flow in a running dashboard. WidgetId selects the flow; NodeKey or EdgeKey selects its item. Cut severs an edge, Cut:$false reconnects it, and omitting Cut preserves its connection state.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleFlow -WidgetId chain -NodeKey issuer -Status Fail
            Update-ConsoleFlow -WidgetId chain -EdgeKey signature -Status Fail -Cut
```

Mark an issuer failure and sever its incoming connection

## PARAMETERS

### -Cut
Cuts the edge; -Cut:$false reconnects it. Omitted preserves connectivity.

```yaml
Type: SwitchParameter
Parameter Sets: Edge
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -EdgeKey
Edge to update.

```yaml
Type: String
Parameter Sets: Edge
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NodeKey
Node to update.

```yaml
Type: String
Parameter Sets: Node
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: Node, Edge
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
New status.

```yaml
Type: StatusKind
Parameter Sets: Node, Edge
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
Flow widget identifier.

```yaml
Type: String
Parameter Sets: Node, Edge
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

- `None`

## RELATED LINKS

- None
