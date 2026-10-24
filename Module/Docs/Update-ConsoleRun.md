---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleRun
## SYNOPSIS
Reports an item's running phase or final outcome to a run dashboard.

Index is the zero-based position in Start-ConsoleRun's Items. Running starts the item or updates its phase without resetting elapsed time. Pass, Fail, Warn and Skip finish an item once; repeated completions do not print duplicate lines. Updates do not produce pipeline output or change the caller's error handling. For direct worker-thread use the handle exposes Started and Finished methods with the same contracts.

## SYNTAX
### __AllParameterSets
```powershell
Update-ConsoleRun [-Run] <LiveRun> -Index <int> -Status <StatusKind> [-Phase <string>] [-Message <string>] [-DurationSeconds <Double>] [<CommonParameters>]
```

## DESCRIPTION
Reports an item's running phase or final outcome to a run dashboard.

Index is the zero-based position in Start-ConsoleRun's Items. Running starts the item or updates its phase without resetting elapsed time. Pass, Fail, Warn and Skip finish an item once; repeated completions do not print duplicate lines. Updates do not produce pipeline output or change the caller's error handling. For direct worker-thread use the handle exposes Started and Finished methods with the same contracts.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleRun $run -Index 0 -Status Running -Phase 'collecting'; Update-ConsoleRun $run -Index 0 -Status Warn -Message 'Response was incomplete'
```

Start and finish one item

## PARAMETERS

### -DurationSeconds
Optional measured duration. Default time since the Running update.

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

### -Index
Zero-based item position.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Message
Full completion detail, retained for the final summary.

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

### -Phase
Literal phase text for a Running update.

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

### -Run
The handle returned by Start-ConsoleRun.

```yaml
Type: LiveRun
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
Running, Pass, Fail, Warn or Skip.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: True
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
