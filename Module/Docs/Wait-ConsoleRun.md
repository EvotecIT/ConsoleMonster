---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Wait-ConsoleRun
## SYNOPSIS
Waits for worker updates while pumping run progress to the PowerShell host.

Use after starting background workers that call the handle's Started and Finished methods. Waits until all items have finished and receives queued host output on the pipeline thread, including timed CI progress while workers are slow. It does not schedule work or print the final summary. Call Complete-ConsoleRun in finally after joining the workers. Ctrl+C signals the handle's Cancellation token so workers can drain and report their last outcomes.

## SYNTAX
### __AllParameterSets
```powershell
Wait-ConsoleRun [-Run] <LiveRun> [<CommonParameters>]
```

## DESCRIPTION
Waits for worker updates while pumping run progress to the PowerShell host.

Use after starting background workers that call the handle's Started and Finished methods. Waits until all items have finished and receives queued host output on the pipeline thread, including timed CI progress while workers are slow. It does not schedule work or print the final summary. Call Complete-ConsoleRun in finally after joining the workers. Ctrl+C signals the handle's Cancellation token so workers can drain and report their last outcomes.

## EXAMPLES

### EXAMPLE 1
```powershell
try { Wait-ConsoleRun $run; $results = Receive-Job $workerJob -Wait } finally { Complete-ConsoleRun $run }
```

Wait for an existing background runner and retain its results

## PARAMETERS

### -Run
The handle shared with background workers.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
