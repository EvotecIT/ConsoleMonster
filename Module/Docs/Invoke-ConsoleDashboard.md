---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Invoke-ConsoleDashboard
## SYNOPSIS
Runs a console dashboard live while a script does the work.

The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession and the Update-Console, Update-Console, Write-ConsoleLogEntry and related cmdlets update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleDashboard [-Dashboard] <IWidget> [-ScriptBlock] <scriptblock> [-ArgumentList <Object[]>] [-InputObject <Object>] [-Parallel] [-ThrottleLimit <int>] [-ImportModule <string[]>] [-RecordPath <string>] [-AutoRecord <int>] [-Summary <IWidget>] [-Inline] [-Transient] [-Clock <SimulatedClock>] [-FramesPerSecond <int>] [-NoLive] [<CommonParameters>]
```

## DESCRIPTION
Runs a console dashboard live while a script does the work.

The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession and the Update-Console, Update-Console, Write-ConsoleLogEntry and related cmdlets update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables from the caller are not visible inside the script: pass them with -ArgumentList and a param() block.

## EXAMPLES

### EXAMPLE 1
```powershell
$d = New-ConsoleDashboard -Title 'Patch' -Content { New-ConsolePanel -Title 'Servers' -Content { New-ConsoleLanes }; New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion } }
'SRV1', 'SRV2', 'SRV3' | Invoke-ConsoleDashboard $d -Parallel -ThrottleLimit 3 {
    param($Server)
    Update-Console -Key $Server -Status Running -Total 5 -WidgetId 'lanes'
    1..5 | ForEach-Object { Start-Sleep -Milliseconds 300; Update-Console -Key $Server -Result Pass -WidgetId 'lanes' }
    Write-Host "$Server patched"
    Update-Console -Key $Server -Complete -WidgetId 'lanes'
}
```

Three servers processed in parallel, three at a time

## PARAMETERS

### -ArgumentList
Arguments passed to the script (after the input item with -Parallel).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AutoRecord
Records the run automatically to the local ConsoleMonster\Recordings folder and keeps only this many recent runs (within 50 MB together), for Show-ConsoleReplay. Ignored with -RecordPath.

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

### -Clock
A run clock (New-ConsoleSimulatedClock) for the header clock, the summary bar's times and log times, instead of real time. Pass it to the script with -ArgumentList to move it forward there.

```yaml
Type: SimulatedClock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Dashboard
The dashboard from New-ConsoleDashboard (or any widget tree).

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

### -FramesPerSecond
Redraw rate (default 15).

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

### -ImportModule
Modules to import into the dashboard runspaces.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Inline
Draws the dashboard inline, below the cursor, instead of on the alternate screen: output and log lines scroll above it and the final state stays in the scrollback.

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

### -InputObject
Items processed with -Parallel; each item is the script's first argument.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
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

### -Parallel
Run the script once per input item on a runspace pool.

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

### -RecordPath
Record every update as JSON lines for Show-ConsoleReplay -PlaybackMode Automatic.

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

### -ScriptBlock
The work to run while the dashboard is live.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Summary
A widget printed to scrollback after the dashboard ends (default: the dashboard's final state).

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ThrottleLimit
Maximum concurrent runspaces with -Parallel (default 4).

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

### -Transient
Leaves nothing behind when the dashboard ends (no final state in the scrollback).

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

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
