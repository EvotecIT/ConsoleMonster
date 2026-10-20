---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleSummaryBar
## SYNOPSIS
Updates a summary bar (New-ConsoleSummaryBar) in the running dashboard: progress and counters.

-Done sets the items done (-Total changes the total; without it the bar keeps its total) and -Counter sets counts by label, in one call; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour, and new labels are added after the existing ones. The elapsed time and the time left follow the session clock, so a simulated clock (New-ConsoleClock) drives them too. Safe to call from any runspace or thread.

## SYNTAX
### __AllParameterSets
```powershell
Update-ConsoleSummaryBar [[-Done] <Double>] [[-Total] <Double>] [-Counter <IDictionary>] [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Updates a summary bar (New-ConsoleSummaryBar) in the running dashboard: progress and counters.

-Done sets the items done (-Total changes the total; without it the bar keeps its total) and -Counter sets counts by label, in one call; a label that is a status word (passed, failed, warnings, skipped, running) takes that status's symbol and colour, and new labels are added after the existing ones. The elapsed time and the time left follow the session clock, so a simulated clock (New-ConsoleClock) drives them too. Safe to call from any runspace or thread.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleSummaryBar -Done $done -Counter ([ordered] @{ passed = $passed; warnings = $warned; failed = $failed })
```

Count a finished check

### EXAMPLE 2
```powershell
Update-ConsoleSummaryBar -Name summary -Done 12 -Total 80
```

A run whose total grows as work is discovered

## PARAMETERS

### -Counter
Labels mapped to counts (use [ordered] to keep the order of new labels); status words take their status symbol and colour.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Done
Items done.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The summary bar name (default summary).

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

### -Total
Items in total; without it the bar keeps the total it has (0 = unknown).

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `None`

## RELATED LINKS

- None
