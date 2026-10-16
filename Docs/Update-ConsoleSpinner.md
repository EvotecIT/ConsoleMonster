---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleSpinner
## SYNOPSIS
Updates a spinner in a running dashboard.

-Label renames it, -Detail changes the muted detail, -Restart restarts its clock and returns it to running, and -Status sets a final state that replaces the animation with the status marker.

## SYNTAX
### __AllParameterSets
```powershell
Update-ConsoleSpinner [[-Name] <string>] [-Label <string>] [-Detail <string>] [-Restart] [-Status <StatusKind>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Updates a spinner in a running dashboard.

-Label renames it, -Detail changes the muted detail, -Restart restarts its clock and returns it to running, and -Status sets a final state that replaces the animation with the status marker.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleSpinner -Name 'Name'
```


## PARAMETERS

### -Detail
A new detail.

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

### -Label
A new label.

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

### -Name
The spinner name (default spinner).

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

### -Restart
Restart the clock and return to running.

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

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
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
A final status (Pass, Fail, Warn, Skip).

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

- `None`

## RELATED LINKS

- None
