---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleSimulatedClock
## SYNOPSIS
Creates a run clock (ConsoleMonster.Live.SimulatedClock) the script moves forward, for simulated runs whose screen shows simulated time.

A live session reads the clock for the header clock, the summary bar's elapsed time and time left, the times of log lines and activities, and the run result's duration, so a demo or a replay of logged work shows its own time instead of the wall clock. Give it to a menu run as the third argument of $MenuContext.Run($dashboard, { ... }, $clock), or to Invoke-ConsoleDashboard -Clock. In the script, $clock.Wait(seconds) and $clock.WaitUntil(seconds) move it forward in small steps, sleeping 1/-Speed of each simulated second (never at speed 0, which runs at once); $clock.Advance(seconds) jumps without waiting and $clock.Hold(seconds) waits real time with the clock standing still. $clock.Elapsed is the time on the clock. Once the dashboard is stopped (Ctrl+C) waits stop sleeping, so the script reaches its end at once.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSimulatedClock [[-Speed] <double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a run clock (ConsoleMonster.Live.SimulatedClock) the script moves forward, for simulated runs whose screen shows simulated time.

A live session reads the clock for the header clock, the summary bar's elapsed time and time left, the times of log lines and activities, and the run result's duration, so a demo or a replay of logged work shows its own time instead of the wall clock. Give it to a menu run as the third argument of $MenuContext.Run($dashboard, { ... }, $clock), or to Invoke-ConsoleDashboard -Clock. In the script, $clock.Wait(seconds) and $clock.WaitUntil(seconds) move it forward in small steps, sleeping 1/-Speed of each simulated second (never at speed 0, which runs at once); $clock.Advance(seconds) jumps without waiting and $clock.Hold(seconds) waits real time with the clock standing still. $clock.Elapsed is the time on the clock. Once the dashboard is stopped (Ctrl+C) waits stop sleeping, so the script reaches its end at once.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleMenuItem 'Run checks' -Action {
    $clock = New-ConsoleSimulatedClock -Speed 7
    $dashboard = New-ConsoleDashboard -Title 'Checks' -Content { New-ConsoleSummaryBar -Total 3; New-ConsoleLogRegion -Height 5 }
    $null = $MenuContext.Run($dashboard, {
        foreach ($done in 1..3) {
            $clock.Wait(4.5)
            Update-ConsoleSummaryBar -Done $done -Counter @{ passed = $done }
            Write-ConsoleLogEntry "Check $done passed"
        }
        New-ConsoleMenuRunResult 'Checks finished'
    }, $clock)
}
```

A menu run on a simulated clock seven times faster than real time

### EXAMPLE 2
```powershell
$clock = New-ConsoleSimulatedClock -Speed 10
Invoke-ConsoleDashboard $dashboard -Clock $clock -ArgumentList $clock, $entries {
    param($Clock, $Entries)
    foreach ($entry in $Entries) { $Clock.WaitUntil($entry.Seconds); Write-ConsoleLogEntry $entry.Text }
}
```

A dashboard replaying logged work at its recorded pace, ten times faster

## PARAMETERS

### -Speed
Simulated seconds per real second while the script waits (default 1, real time); 0 never sleeps.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Live.SimulatedClock`

## RELATED LINKS

- None
