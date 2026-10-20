---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Start-ConsoleReplay
## SYNOPSIS
Replays a recorded dashboard session (JSON lines) into a dashboard.

Recordings are written by Start-ConsoleDashboard -RecordPath. Replays keep the original timing, scaled by -Speed (0 plays as fast as possible). Useful for demos, documentation captures, tests and bug reports.

## SYNTAX
### __AllParameterSets
```powershell
Start-ConsoleReplay [-Dashboard] <IWidget> [-Path] <string> [-Speed <double>] [<CommonParameters>]
```

## DESCRIPTION
Replays a recorded dashboard session (JSON lines) into a dashboard.

Recordings are written by Start-ConsoleDashboard -RecordPath. Replays keep the original timing, scaled by -Speed (0 plays as fast as possible). Useful for demos, documentation captures, tests and bug reports.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-ConsoleReplay -Dashboard $dashboard -Path .\run.jsonl -Speed 2
```

Replay twice as fast

## PARAMETERS

### -Dashboard
The dashboard (or any widget tree) whose widget names match the recording.

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The recording file.

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

### -Speed
Replay speed (default 1).

```yaml
Type: Double
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

- `None`

## RELATED LINKS

- None
