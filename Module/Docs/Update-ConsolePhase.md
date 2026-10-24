---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsolePhase
## SYNOPSIS
Updates a phase of a running dashboard's phase strip.

Set -Status Running when a phase starts; its duration is measured until it gets a final status (Pass, Fail, Warn, Skip). -Detail replaces the shown detail (for example a percentage).

## SYNTAX
### __AllParameterSets
```powershell
Update-ConsolePhase [[-WidgetId] <string>] [-Key] <string> [-Status <StatusKind>] [-Detail <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Updates a phase of a running dashboard's phase strip.

Set -Status Running when a phase starts; its duration is measured until it gets a final status (Pass, Fail, Warn, Skip). -Detail replaces the shown detail (for example a percentage).

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsolePhase -Key Build -Status Running; dotnet build; Update-ConsolePhase -Key Build -Status Pass
```

Start and finish a phase

## PARAMETERS

### -Detail
A detail such as a percentage or duration.

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

### -Key
The phase key.

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
The new status.

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

### -WidgetId
The phases widget name (default phases).

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

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
