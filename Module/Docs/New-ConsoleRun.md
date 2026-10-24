---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleRun
## SYNOPSIS
Starts a bounded run dashboard for work scheduled by your script.

Declares the queued items and returns a LiveRun handle. Use Update-ConsoleRun as workers start or finish and Complete-ConsoleRun in a finally block. Finished items stream above the live dashboard; failures and warnings remain pinned. The dashboard uses at most 40 percent of terminal height by default. CI and redirected hosts use append-only lines with periodic totals. This command does not execute work or change its results or exit code. Name, Target and Group accept property names or script blocks evaluated once, before the session starts. Interactive keys are opt-in: p pauses the log, f filters it to problems, g toggles groups, and ? shows help.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRun -Items <psobject[]> [-Title <string>] [-Name <Object>] [-Target <Object>] [-Group <Object>] [-NoLive] [-InteractiveKeys] [-Trend] [-HeightPercent <int>] [-ProgressEvery <int>] [-ProgressSeconds <double>] [-ReportPath <string>] [<CommonParameters>]
```

## DESCRIPTION
Starts a bounded run dashboard for work scheduled by your script.

Declares the queued items and returns a LiveRun handle. Use Update-ConsoleRun as workers start or finish and Complete-ConsoleRun in a finally block. Finished items stream above the live dashboard; failures and warnings remain pinned. The dashboard uses at most 40 percent of terminal height by default. CI and redirected hosts use append-only lines with periodic totals. This command does not execute work or change its results or exit code. Name, Target and Group accept property names or script blocks evaluated once, before the session starts. Interactive keys are opt-in: p pauses the log, f filters it to problems, g toggles groups, and ? shows help.

## EXAMPLES

### EXAMPLE 1
```powershell
$files = @(Get-ChildItem *.log)
$run = New-ConsoleRun -Items $files -Title 'Archive logs' -Name Name -Target DirectoryName
try {
    for ($i = 0; $i -lt $files.Count; $i++) {
        Update-ConsoleRun $run -Index $i -State Running -Phase 'archiving'
        try { Compress-Archive $files[$i].FullName "$($files[$i].FullName).zip" -ErrorAction Stop; Update-ConsoleRun $run -Index $i -State Pass }
        catch { Update-ConsoleRun $run -Index $i -State Fail -Message $_.Exception.Message }
    }
} finally { Complete-ConsoleRun $run }
```

Track a script without changing its output

### EXAMPLE 2
```powershell
$run = New-ConsoleRun -Items $checks -Name Name -Target Server -Group Category -NoLive -ProgressEvery 20
```

Use append-only output in CI

## PARAMETERS

### -Group
An optional group property or script block.

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

### -HeightPercent
Maximum terminal height percentage used by the live dashboard (default 40).

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

### -InteractiveKeys
Enable optional p/f/g/? keys when keyboard input is available.

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

### -Items
The complete input list. Updates identify each item by zero-based position.

```yaml
Type: PSObject[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
A name property or script block ($_ is the item). Default the item's display title.

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

### -NoLive
Force append-only output without cursor movement.

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

### -ProgressEvery
Print CI progress after this many items (default 25). Zero disables the count trigger.

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

### -ProgressSeconds
Print CI progress at this interval in seconds (default 10). Zero disables the timer.

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

### -ReportPath
Optional report location shown in the final summary.

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

### -Target
An optional target property or script block.

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

### -Title
The literal run title.

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

### -Trend
Show the throughput trend next to its rate.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Live.LiveRun`

## RELATED LINKS

- None
