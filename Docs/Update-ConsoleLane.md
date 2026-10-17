---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleLane
## SYNOPSIS
Updates a lane of a running dashboard: progress, step results, status and detail.

With -Result, counts one completed step (or -Steps steps) as passed, failed, warning or skipped and advances the bar. With -Status, sets the lane state (Running, Pass, Fail, Warn, Skip). -Complete finishes the lane (Fail if any step failed, Warn if any warned, otherwise Pass). Safe to call from any runspace or thread.

## SYNTAX
### __AllParameterSets
```powershell
Update-ConsoleLane [-Key] <string> [-Name <string>] [-Result <StatusKind>] [-Steps <double>] [-Status <StatusKind>] [-Detail <Object>] [-Label <Object>] [-Total <double>] [-Complete] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Updates a lane of a running dashboard: progress, step results, status and detail.

With -Result, counts one completed step (or -Steps steps) as passed, failed, warning or skipped and advances the bar. With -Status, sets the lane state (Running, Pass, Fail, Warn, Skip). -Complete finishes the lane (Fail if any step failed, Warn if any warned, otherwise Pass). Safe to call from any runspace or thread.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleLane -Key 'Value'
```


## PARAMETERS

### -Complete
Finish the lane.

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

### -Detail
What the lane is doing now.

```yaml
Type: Object
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
The lane key (created on first use).

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
The lane label (when adding or renaming).

```yaml
Type: Object
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
The lanes widget name (default lanes).

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

### -Result
Result of completed steps.

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
Set the lane status.

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

### -Steps
Steps completed with this result (default 1).

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

### -Total
Planned steps for the lane.

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
