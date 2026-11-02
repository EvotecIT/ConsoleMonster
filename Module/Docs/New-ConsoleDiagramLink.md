---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDiagramLink
## SYNOPSIS
Connects two diagram components.

From and To identify nodes by their label or explicit Id. The containing diagram creates undeclared endpoints. Labels are literal, Status uses the shared ConsoleMonster health states, and Both adds an arrow at each end.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDiagramLink [-From] <string> [-To] <string> [-Label <string>] [-Status <StatusKind>] [-Style <DiagramLinkStyle>] [-Both] [<CommonParameters>]
```

## DESCRIPTION
Connects two diagram components.

From and To identify nodes by their label or explicit Id. The containing diagram creates undeclared endpoints. Labels are literal, Status uses the shared ConsoleMonster health states, and Both adds an arrow at each end.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDiagramLink 'File server' Backup -Label nightly -Status Fail -Style Dashed
```

A failing backup connection.

## PARAMETERS

### -Both
Draws arrows at both ends.

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

### -Label
The connection text.

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
The connection health.

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

### -Style
Solid, dashed or dotted stroke.

```yaml
Type: DiagramLinkStyle
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Solid, Dashed, Dotted

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

- `ConsoleMonster.Widgets.DiagramLink`

## RELATED LINKS

- None
