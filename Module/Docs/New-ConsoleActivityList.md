---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleActivityList
## SYNOPSIS
Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.

Each activity has an id, a title, an optional status, a percentage (or none, which spins) and an optional parent it is indented under; finished activities disappear, and an empty list takes no space. Update it with Update-ConsoleActivity from the script of a running dashboard or menu run. -MaxVisible caps the rows (more are summed up in one line). The default name, $progress, is also where Write-Progress goes in Invoke-ConsoleDashboard, so a list with that name shows the script's progress records; give another name for activities of your own.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleActivityList [[-WidgetId] <string>] [-MaxVisible <int>] [-ShowIds] [-NoElapsed] [<CommonParameters>]
```

## DESCRIPTION
Creates an activity list: the work in progress right now, one row per activity, with a spinner or a bar and the time it has taken.

Each activity has an id, a title, an optional status, a percentage (or none, which spins) and an optional parent it is indented under; finished activities disappear, and an empty list takes no space. Update it with Update-ConsoleActivity from the script of a running dashboard or menu run. -MaxVisible caps the rows (more are summed up in one line). The default name, $progress, is also where Write-Progress goes in Invoke-ConsoleDashboard, so a list with that name shows the script's progress records; give another name for activities of your own.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboard = New-ConsoleDashboard -Title 'Checks' -Content { New-ConsolePanel -Title 'Running' -Content { New-ConsoleActivityList -WidgetId running -MaxVisible 4 } }
Invoke-ConsoleDashboard $dashboard {
    Update-ConsoleActivity -WidgetId running -Id 1 -Activity 'SRV01 · Free space'
    Start-Sleep -Seconds 1
    Update-ConsoleActivity -WidgetId running -Id 1 -Completed
}
```

The checks running on four workers

## PARAMETERS

### -MaxVisible
The most activities shown (default 8); more are summed up in one line.

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

### -NoElapsed
Leaves out the time each activity has taken at the end of its row.

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

### -ShowIds
Starts each row with the activity's id (#3), for parallel work.

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

### -WidgetId
The live name used by Update-ConsoleActivity (default $progress, where Write-Progress also goes in Invoke-ConsoleDashboard).

```yaml
Type: String
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

- `ConsoleMonster.Widgets.ActivityList`

## RELATED LINKS

- None
