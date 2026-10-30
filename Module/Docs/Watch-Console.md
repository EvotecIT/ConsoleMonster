---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Watch-Console
## SYNOPSIS
Runs a script block again and again and shows its latest output as a live view.

Like watch: every -PollIntervalSeconds seconds the script block runs and its output replaces the previous one, shaped as Show-ConsoleTable and the other commands shape it (objects as a table, one dictionary as a record, text as it is). The heading shows the interval, -Title, the run number and the time. -Differences highlights the cells that changed since the previous run. A run that fails shows its error and the watch goes on. Warnings, errors and Write-Host text from the script block are shown under its output in the view (an error marks the run in the heading) instead of interrupting it. Ctrl+C ends the watch and the last frame stays in the scrollback; -Count ends it after that many runs. Piped into another command (Out-File, Out-String), or without an interactive console (redirected output, CI, a job), every run is printed instead, so use -Count there; assigning the result to a variable at the prompt does not capture the live view, so pipe it instead.

## SYNTAX
### __AllParameterSets
```powershell
Watch-Console [-ScriptBlock] <scriptblock> [-PollIntervalSeconds <double>] [-Count <int>] [-Differences] [-Title <Object>] [-NoLive] [<CommonParameters>]
```

## DESCRIPTION
Runs a script block again and again and shows its latest output as a live view.

Like watch: every -PollIntervalSeconds seconds the script block runs and its output replaces the previous one, shaped as Show-ConsoleTable and the other commands shape it (objects as a table, one dictionary as a record, text as it is). The heading shows the interval, -Title, the run number and the time. -Differences highlights the cells that changed since the previous run. A run that fails shows its error and the watch goes on. Warnings, errors and Write-Host text from the script block are shown under its output in the view (an error marks the run in the heading) instead of interrupting it. Ctrl+C ends the watch and the last frame stays in the scrollback; -Count ends it after that many runs. Piped into another command (Out-File, Out-String), or without an interactive console (redirected output, CI, a job), every run is printed instead, so use -Count there; assigning the result to a variable at the prompt does not capture the live view, so pipe it instead.

## EXAMPLES

### EXAMPLE 1
```powershell
Watch-Console { Get-Process | Select-Object -First 5 Name, Id, CPU } -Count 3 -PollIntervalSeconds 1
```

Watch a bounded process sample

### EXAMPLE 2
```powershell
Watch-Console { Get-Process -Id $PID | Select-Object Name, Id, CPU } -Count 3 -PollIntervalSeconds 1 | Out-String
```

Capture repeated process snapshots as text

### EXAMPLE 3
```powershell
Watch-Console -Title 'Disk space' -PollIntervalSeconds 1 -Count 3 -Differences { Get-PSDrive -PSProvider FileSystem | Select-Object Name, Used, Free }
```

Highlight changes in file-system drive space

## PARAMETERS

### -Count
How many runs, or 0 (default) until Ctrl+C.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Differences
Highlights the cells that changed since the previous run.

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

### -NoLive
Disables live redraws and uses append-only output.

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

### -PollIntervalSeconds
Seconds between runs (default 2).

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

### -ScriptBlock
The script block to run.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title for the heading (markup); default the script block's text.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
