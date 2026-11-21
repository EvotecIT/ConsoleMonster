---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/invoke-consoledashboard/
schema: 2.0.0
---
# Invoke-ConsoleDashboard
## SYNOPSIS
Runs a console dashboard live while a script does the work.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleDashboard [-Dashboard] <IWidget> [-ScriptBlock] <scriptblock> [-ArgumentList <Object[]>] [-InputObject <Object>] [-Parallel] [-ThrottleLimit <int>] [-ImportFunction <string[]>] [-ImportModule <string[]>] [-RecordingPath <string>] [-AutoRecord <int>] [-Summary <IWidget>] [-Inline] [-PrintFinalState] [-Clock <SimulatedClock>] [-FramesPerSecond <double>] [-NoLive] [-ReducedMotion] [<CommonParameters>]
```

## DESCRIPTION
The script runs in a runspace the module owns (or, with -Parallel, once per input item on a runspace pool, started as items arrive and limited by -ThrottleLimit). Inside it, $ConsoleSession, Update-Console and Write-ConsoleLogEntry update the screen; Write-Host, Write-Warning, Write-Verbose, errors and Console.WriteLine go to the dashboard's log region instead of scribbling over the screen. The screen is redrawn at about 15 frames per second however many updates arrive. Ctrl+C stops the script and always restores the terminal; the dashboard then leaves the alternate screen and prints its final state (or -Summary). Script output is returned when the dashboard ends. In redirected or CI output the dashboard becomes append-only lines. Variables and functions from the caller are not visible inside the script: read a variable with $using:name (as with ForEach-Object -Parallel), bring functions in with -ImportFunction, or pass values with -ArgumentList and a param() block.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboardOptions2 = @{
    Title = 'Checks'
    Content = {
        $panelOptions2 = @{
            Title = 'Servers'
            Content = { New-ConsoleLanes -WidgetId servers }
        }
        New-ConsolePanel @panelOptions2
        $panelOptions = @{
            Fill = $true
            Title = 'Log'
            Content = { New-ConsoleLogRegion }
        }
        New-ConsolePanel @panelOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions2
$dashboardOptions = @{
    Parallel = $true
    Dashboard = $dashboard
    ThrottleLimit = 3
    ScriptBlock = {
        param($Server)
        $updateConsoleOptions3 = @{
            WidgetId = 'servers'
            Key = $Server
            Status = 'Running'
            Total = 5
        }
        Update-Console @updateConsoleOptions3
        1..5 | ForEach-Object {
            $updateConsoleOptions2 = @{
                WidgetId = 'servers'
                Key = $Server
                Result = 'Pass'
            }
            Update-Console @updateConsoleOptions2
        }
        Write-Host "$Server checked"
        $updateConsoleOptions = @{
            Complete = $true
            WidgetId = 'servers'
            Key = $Server
        }
        Update-Console @updateConsoleOptions
    }
}
'SRV1', 'SRV2', 'SRV3' | Invoke-ConsoleDashboard @dashboardOptions
```

Process three sample servers in parallel. Each string from the pipeline becomes the worker's first argument, $Server. The report uses the same value as the lane key. Update-Console assigns five planned checks to the lane, posts five passing results, and completes the lane. Worker messages appear in the log.

### EXAMPLE 2
```powershell
$frames = @([pscustomobject]@{ Cpu = 20 }, [pscustomobject]@{ Cpu = 45 })
$dashboardOptions3 = @{
    Title = 'CPU samples'
    Content = {
        $chartOptions = @{
            WidgetId = 'cpu'
            Type = 'Sparkline'
        }
        New-ConsoleChart @chartOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions3
function Show-Frame($Frame) {
    $updateConsoleOptions = @{
        Append = $true
        WidgetId = 'cpu'
        Value = $Frame.Cpu
    }
    Update-Console @updateConsoleOptions
}
$dashboardOptions2 = @{
    Dashboard = $dashboard
    ImportFunction = @('Show-Frame')
    ScriptBlock = {
        foreach ($frame in $using:frames) { Show-Frame $frame;
            $dashboardOptions = @{
                Seconds = 0.5
            }
            Wait-ConsoleDashboard @dashboardOptions
        }
    }
}
Invoke-ConsoleDashboard @dashboardOptions2
```

Append two CPU samples. The dashboard script runs in a separate runspace. $using:frames copies the caller's data into that script, and ImportFunction names one function to bring into the runspace as a one-element array. Show-Frame then addresses the cpu chart by WidgetId and adds one sample per call.

### EXAMPLE 3
```powershell
$dashboardOptions = @{
    Title = 'Build'
    Content = { New-ConsoleProgress -WidgetId 'build' -Total 1 }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$invokeOptions = @{
    Dashboard = $dashboard
    ArgumentList = @('Compile')
    ScriptBlock = {
        param($Phase)
        $update = @{
            WidgetId = 'build'
            Value = 1
            Label = $Phase
        }
        Update-Console @update
    }
}
Invoke-ConsoleDashboard @invokeOptions
```

Pass a label into a sequential dashboard script. ArgumentList is an array of positional arguments. Here the one entry binds to the script's first parameter, $Phase. A parallel dashboard reserves that first argument for the pipeline item, so the entries in ArgumentList start at the second parameter in that mode.

### EXAMPLE 4
```powershell
Get-ConsoleDemo -Slug 'container-status' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'process-monitor' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'build-steps' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'log-tail' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see Invoke-ConsoleDashboard in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

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
Records the run automatically to the local ConsoleMonster\Recordings folder and keeps only this many recent runs (within 50 MB together), for Show-ConsoleReplay. Ignored with -RecordingPath.

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

### -Clock
An optional C# run clock implementing IClock for the header clock, the summary bar's times and log times, instead of real time. Pass it to the script with -ArgumentList to move it forward there.

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
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 15
Accept pipeline input: False
Accept wildcard characters: False
```

### -ImportFunction
Functions from the caller's session to define in the dashboard runspaces, by name.

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
Default value: $false
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
Default value: $false
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
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrintFinalState
Prints the final dashboard state in scrollback (default true); pass -PrintFinalState:$false to leave nothing behind.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $true
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordingPath
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
Default value: 4
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

- [https://consolemonster.dev/powershell/commands/invoke-consoledashboard/](https://consolemonster.dev/powershell/commands/invoke-consoledashboard/)
