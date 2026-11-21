---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/wait-consoledashboard/
schema: 2.0.0
---
# Wait-ConsoleDashboard
## SYNOPSIS
Pauses dashboard work, or captures its next checkpoint during a recording.

## SYNTAX
### __AllParameterSets
```powershell
Wait-ConsoleDashboard [-Seconds] <double> [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboardOptions2 = @{
    Title = 'Scan'
    Content = {
        $progressOptions = @{
            WidgetId = 'overall'
            Label = 'Targets'
            Total = 3
        }
        New-ConsoleProgress @progressOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions2
$dashboardOptions = @{
    Dashboard = $dashboard
    ScriptBlock = {
        foreach ($count in 1..3) {
            $updateConsoleOptions = @{
                WidgetId = 'overall'
                Value = $count
            }
            Update-Console @updateConsoleOptions
            Wait-ConsoleDashboard -Seconds 0.25
        }
    }
}
Invoke-ConsoleDashboard @dashboardOptions
```

Update progress between cancellable pauses

### EXAMPLE 2
```powershell
$dashboardOptions2 = @{
    Title = 'Inventory'
    Content = { New-ConsoleLanes -WidgetId servers }
}
$dashboard = New-ConsoleDashboard @dashboardOptions2
$dashboardOptions = @{
    Dashboard = $dashboard
    ScriptBlock = {
        $updateConsoleOptions2 = @{
            WidgetId = 'servers'
            Key = 'web01'
            Status = 'Running'
        }
        Update-Console @updateConsoleOptions2
        Wait-ConsoleDashboard -Seconds 0.1
        $updateConsoleOptions = @{
            Complete = $true
            WidgetId = 'servers'
            Key = 'web01'
            Status = 'Pass'
        }
        Update-Console @updateConsoleOptions
    }
}
Invoke-ConsoleDashboard @dashboardOptions
```

Pause briefly between live server updates

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'build-steps' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'log-tail' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see Wait-ConsoleDashboard in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Seconds
The pause in seconds, fractions allowed (0.25 is a quarter second); zero captures a checkpoint without waiting.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: 0
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/wait-consoledashboard/](https://consolemonster.dev/powershell/commands/wait-consoledashboard/)
