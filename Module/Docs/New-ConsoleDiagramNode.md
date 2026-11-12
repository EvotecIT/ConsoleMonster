---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolediagramnode/
schema: 2.0.0
---
# New-ConsoleDiagramNode
## SYNOPSIS
Creates a diagram component.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDiagramNode [-Label] <string> [-Id <string>] [-Kind <DiagramNodeKind>] [-Subtitle <string>] [-Status <StatusKind>] [-Value <string>] [-To <string[]>] [<CommonParameters>]
```

## DESCRIPTION
Label is the connection identity unless Id is set. Subtitle is the second line and Value is the third. To adds unlabelled outgoing connections; missing destination nodes are created by the diagram.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDiagramNode Router -Kind Gateway -Subtitle '192.168.1.1' -Value '1 ms' -Status Pass -To Internet
```

A monitored gateway.

### EXAMPLE 2
```powershell
New-ConsoleDiagram -Title 'Request path' -Content {
                New-ConsoleDiagramNode -Id api -Label 'Orders API' -Kind Service
            } | Show-Console
```

Compose an inventory diagram node

## PARAMETERS

### -Id
An explicit connection and live-update key.

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

### -Kind
The component kind and icon.

```yaml
Type: DiagramNodeKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Service, Server, Workstation, Gateway, Cloud, Storage, Database, Queue, User, Person, App, Process, Device, External

Required: False
Position: named
Default value: Service
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The component's displayed name and default id.

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
The component health. Omit it to show no status marker.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Subtitle
Secondary text shown below the diagram node title.

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

### -To
Destination node ids for outgoing connections.

```yaml
Type: String[]
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
The independent third line.

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

- `ConsoleMonster.Widgets.DiagramNode`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolediagramnode/](https://consolemonster.dev/powershell/commands/new-consolediagramnode/)
