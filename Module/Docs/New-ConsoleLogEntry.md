---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLogEntry
## SYNOPSIS
Creates one log line with a level and a time, for New-ConsoleLog.

Use it inside the script block of New-ConsoleLog to show a log printed once, or pipe a log made earlier to Add-ConsoleItem with these entries. -Level takes info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). -At is the time shown at the start of the line: a TimeSpan, a number of seconds, or a number with a unit (500ms, 2s, 1.5m, 1h). The message is shown literally; rich text from New-ConsoleText keeps its colours. In a running dashboard, write to the log with Write-ConsoleLog instead.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLogEntry [-Message] <Object> [-Level <string>] [-At <timespan>] [<CommonParameters>]
```

## DESCRIPTION
Creates one log line with a level and a time, for New-ConsoleLog.

Use it inside the script block of New-ConsoleLog to show a log printed once, or pipe a log made earlier to Add-ConsoleItem with these entries. -Level takes info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). -At is the time shown at the start of the line: a TimeSpan, a number of seconds, or a number with a unit (500ms, 2s, 1.5m, 1h). The message is shown literally; rich text from New-ConsoleText keeps its colours. In a running dashboard, write to the log with Write-ConsoleLog instead.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLog -Height 5 {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    New-ConsoleLogEntry 'DC02: replication partner DC05 last success 3h ago' -Level Warning -At 31s
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
} | Write-ConsoleWidget
```

A short log printed once

### EXAMPLE 2
```powershell
$events = Import-Csv deploy-events.csv
New-ConsoleLog {
    foreach ($event in $events) { New-ConsoleLogEntry $event.Message -Level $event.Level -At $event.Seconds }
} | Write-ConsoleWidget
```

Entries from objects

## PARAMETERS

### -At
The time shown at the start of the line: a TimeSpan, seconds (2 or 1.5) or a number with a unit (500ms, 2s, 1.5m, 1h). Default 0.

```yaml
Type: TimeSpan
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Level
The level: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Verbose, Debug, Success, Skipped). Default info.

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

### -Message
The message (shown literally; rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Widgets.LogEntry`

## RELATED LINKS

- None
