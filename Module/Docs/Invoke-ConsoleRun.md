---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Invoke-ConsoleRun
## SYNOPSIS
Runs a script block for every input item, optionally in parallel, with a live view of the run.

$_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Label sets each item's label: a property name or a script block. -RetryCount runs a failed or timed-out item again (after -RetryDelaySeconds); only the last attempt's output and errors are kept, the item's cell shows the retry and the summary counts retried items. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleRun [-ScriptBlock] <scriptblock> [-InputObject <psobject>] [-Parallel] [-ThrottleLimit <int>] [-TimeoutSeconds <double>] [-RetryCount <int>] [-RetryDelaySeconds <double>] [-Unordered] [-Label <Object>] [-Title <Object>] [-NoLive] [<CommonParameters>]
```

## DESCRIPTION
Runs a script block for every input item, optionally in parallel, with a live view of the run.

$_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Label sets each item's label: a property name or a script block. -RetryCount runs a failed or timed-out item again (after -RetryDelaySeconds); only the last attempt's output and errors are kept, the item's cell shows the retry and the summary counts retried items. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.

## EXAMPLES

### EXAMPLE 1
```powershell
$port = 443; $servers | Invoke-ConsoleRun -Parallel -ThrottleLimit 8 -Label Name { Test-NetConnection $_.Name -Port $using:port -InformationLevel Quiet }
```

Check servers in parallel

### EXAMPLE 2
```powershell
Get-ChildItem *.log | Invoke-ConsoleRun { Compress-Archive $_ "$($_.BaseName).zip" -Force }
```

One at a time, in the current scope

### EXAMPLE 3
```powershell
1..40 | Invoke-ConsoleRun -Parallel -ThrottleLimit 8 -TimeoutSeconds 10 -Title 'Patching web farm' -Label { "web$('{0:d2}' -f $_)" } {
    Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 1500)
    if ($_ % 13 -eq 0) { throw "web$_ did not come back after the reboot" }
}
```

Forty items in parallel with a title and a time limit: items that throw or run past 10 seconds are failures, listed when the run ends

### EXAMPLE 4
```powershell
$servers | Invoke-ConsoleRun -Parallel -RetryCount 2 -RetryDelaySeconds 5 -Label Name { Restart-Computer $_.Name -Wait -For WinRM -Timeout 300 -ErrorAction Stop }
```

Retry flaky servers twice, five seconds apart

## PARAMETERS

### -InputObject
The items (pipeline input).

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Label
Each item's label in the view: a property name or a script block ($_ is the item). Default a name-like property, or the item's text.

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
Runs items at the same time, each in its own runspace (pass caller variables with $using:name).

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

### -RetryCount
How many more times a failed or timed-out item runs before it counts as failed (default 0).

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

### -RetryDelaySeconds
Seconds to wait before each retry (default 0).

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
The script block run for each item ($_ is the item).

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

### -ThrottleLimit
How many items run at the same time with -Parallel (default 5).

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

### -TimeoutSeconds
Seconds an item may run with -Parallel before it is stopped and counted as failed (default no limit; ignored with a warning without -Parallel).

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

### -Title
A title above the live view (markup).

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

### -Unordered
Writes each item's output as soon as it finishes instead of in input order (when the live view is not showing).

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
