---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/invoke-consolerun/
schema: 2.0.0
---
# Invoke-ConsoleRun
## SYNOPSIS
Runs a script block for every input item, optionally in parallel, with a live view of the run.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleRun [-ScriptBlock] <scriptblock> [-InputObject <psobject>] [-Parallel] [-ThrottleLimit <int>] [-TimeoutSeconds <double>] [-RetryCount <int>] [-RetryDelaySeconds <double>] [-Unordered] [-Label <Object>] [-Title <Object>] [-NoLive] [-ReducedMotion] [<CommonParameters>]
```

## DESCRIPTION
In GitHub Actions, Azure Pipelines and GitLab CI, sequential items enclose their work and result in a collapsible log group. Parallel items group each completed result atomically; independently emitted worker output keeps its existing routing. CONSOLEMONSTER_CI=none disables provider groups.

$_ is the item, as in ForEach-Object. While it runs, a live view shows a summary line (done of total, percentage, time left, counts), one cell per item, the running items with their elapsed time and the failures; without an interactive console (redirected output, CI, a job) it prints one line per finished item and a summary instead. The output is the script block's ordinary output, in input order (or as each item finishes with -Unordered); an item's errors are written as error records and mark it as failed. While the live view is showing, ordinary output and native stream records are delivered when it ends. With -Parallel, while the live view is showing, host messages, warnings, information, verbose and debug messages, and errors also appear immediately in its log; Write-Progress appears in an activity list, with independent parent and child activities per worker. Without -Parallel, progress bars from the script block are hidden while the live view is showing. Without -Parallel the items run one at a time in the caller's scope, as with ForEach-Object, so variables set in the script block stay. -Parallel runs up to -ThrottleLimit items at a time in their own runspaces on Windows PowerShell 5.1 and PowerShell 7: variables of the caller are passed with $using:name, and -TimeoutSeconds stops an item that runs too long. -Label sets each item's label: a property name or a script block. -RetryCount runs a failed or timed-out item again (after -RetryDelaySeconds); only the last attempt's output and errors are kept, the item's cell shows the retry and the summary counts retried items. Ctrl+C stops the run: running items are stopped and the rest are counted as skipped.

## EXAMPLES

### EXAMPLE 1
```powershell
$servers = @(
    [pscustomobject]@{ Name = 'web01' }
    [pscustomobject]@{ Name = 'web02' }
)
$runOptions = @{
    Parallel = $true
    ThrottleLimit = 8
    Label = 'Name'
    ScriptBlock = { $_.Name.ToUpperInvariant() }
}
$servers | Invoke-ConsoleRun @runOptions
```

Inspect two records in parallel. The pipeline supplies one record per invocation of ScriptBlock. Label reads Name for the dashboard, while ScriptBlock returns the uppercased name to the PowerShell pipeline. ThrottleLimit is an upper bound, so this example starts at most two workers.

### EXAMPLE 2
```powershell
$servers = @('web-01', 'web-02', 'web-03')
$runOptions = @{
    Parallel = $true
    ThrottleLimit = 3
    TimeoutSeconds = 3
    ErrorAction = 'SilentlyContinue'
    ErrorVariable = 'failures'
    ScriptBlock = {
        if ($_ -eq 'web-03') { throw "$_ restart check failed" }
        [pscustomobject]@{ Host = $_; Message = "$_ preview passed" }
    }
}
$results = @($servers | Invoke-ConsoleRun @runOptions)
$results
$failures
```

Separate successful results from error records, as in patch-progress. ErrorAction keeps the synthetic error off the display while ErrorVariable retains it. Output from the other preview checks remains available as ordinary PowerShell objects. The sample makes no changes to any host.

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'patch-progress' | Select-Object -ExpandProperty Source
```

Read the full patch-progress example, which uses Invoke-ConsoleRun and renders a report from the retained successes and failures.

### EXAMPLE 4
```powershell
$runOptions = @{
    ScriptBlock = { $_.Name }
}
Get-ChildItem -File | Select-Object -First 5 | Invoke-ConsoleRun @runOptions
```

Read up to five filenames sequentially. Each invocation sees the current FileInfo as $_ and emits its Name. Without Parallel, the script runs in the caller's scope.

### EXAMPLE 5
```powershell
$runOptions = @{
    Parallel = $true
    ThrottleLimit = 8
    TimeoutSeconds = 10
    Title = 'Patching web farm'
    Label = { 'web{0:d2}' -f $_ }
    ScriptBlock = {
        Start-Sleep -Milliseconds (Get-Random -Minimum 200 -Maximum 1500)
        if ($_ % 13 -eq 0) { throw ('web{0:d2} did not come back after the reboot' -f $_) }
    }
}
1..40 | Invoke-ConsoleRun @runOptions
```

Simulate a forty-host patch wave like the parallel-patch gallery demo. Up to eight items run at once with a ten-second limit per item in this example. Label maps each number to a display name. The synthetic failures on items 13, 26 and 39 produce error records and mark those items as failed.

### EXAMPLE 6
```powershell
$servers = @(
    [pscustomobject]@{ Name = 'server01' }
    [pscustomobject]@{ Name = 'server02' }
)
$runOptions = @{
    Parallel = $true
    RetryCount = 2
    RetryDelaySeconds = 5
    Label = 'Name'
    ScriptBlock = { "Would restart $($_.Name)" }
}
$servers | Invoke-ConsoleRun @runOptions
```

Configure up to two retries with a five-second delay after a failed attempt. The sample emits a message and succeeds on its first attempt. Use the same settings around a script block that can fail to exercise the retry path.

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
Default value: $false
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
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReducedMotion
Disables animation and limits live redraws to once a second without changing the layout. Accessible mode always enables reduced motion.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
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
Default value: 0
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
Default value: 0
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
Default value: 5
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
Default value: 0
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
Default value: $false
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

- [https://consolemonster.dev/powershell/commands/invoke-consolerun/](https://consolemonster.dev/powershell/commands/invoke-consolerun/)
