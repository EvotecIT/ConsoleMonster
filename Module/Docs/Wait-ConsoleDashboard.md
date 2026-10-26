---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Wait-ConsoleDashboard
## SYNOPSIS
Pauses dashboard work, or captures its next checkpoint during a recording.

Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.

## SYNTAX
### __AllParameterSets
```powershell
Wait-ConsoleDashboard [-Seconds] <double> [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Pauses dashboard work, or captures its next checkpoint during a recording.

Waits cancellably on the resolved live session and writes no output. A recording target with SkipDelays enabled captures earlier queued updates instead of waiting.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboard = New-ConsoleDashboard -Title 'Scan' -Content {
    New-ConsoleProgress -WidgetId overall -Label Targets -Total 3
}
Invoke-ConsoleDashboard -Dashboard $dashboard -ScriptBlock {
    foreach ($count in 1..3) {
        Update-Console -WidgetId overall -Value $count
        Wait-ConsoleDashboard -Seconds 0.25
    }
}
```

Update progress between cancellable pauses

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
Default value: None
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

- None
