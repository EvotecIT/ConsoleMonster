---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Write-ConsoleLogEntry
## SYNOPSIS
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleLogEntry [-Message] <Object> [-Level <string>] [-WidgetId <string>] [-Tag <string>] [-Highlight <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Writes a line to the log region of a running dashboard.

-Level sets the tag: info, warn, err, verbose, debug, pass or skip, or PowerShell's words (Information, Warning, Error, Success...). Errors, warnings, passes and skips carry a status marker; only warnings and errors are coloured. Write-Host in a dashboard runspace reaches the same log automatically.

## EXAMPLES

### EXAMPLE 1
```powershell
$dashboard = New-ConsoleDashboard -Title 'Connectivity' -Content { New-ConsoleLogRegion }
Invoke-ConsoleDashboard $dashboard { Write-ConsoleLogEntry 'DC03 responded slowly' -Level warn }
```

Log a warning

### EXAMPLE 2
```powershell
$dashboard = New-ConsoleDashboard -Title 'Patching' -Content { New-ConsoleLogRegion -NoElapsed }
Invoke-ConsoleDashboard $dashboard {
    foreach ($server in 'DC01', 'DC02', 'FS01') {
        Write-ConsoleLogEntry 'Installing KB5044284' -Tag $server
    }
    Write-ConsoleLogEntry 'Reboot pending' -Level warn -Tag 'FS01'
}
```

Tag lines with the server they came from

## PARAMETERS

### -Highlight
Highlights the message: Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings) or a language such as Json or PowerShell. The message stays literal.

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

### -Level
The level: info, warn, err, verbose, debug, pass or skip. PowerShell's words work too: Information, Warning, Error, Verbose, Debug, Success, Skipped (and the status words of Show-ConsoleStatusLine).

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
The message.

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

### -Tag
The source of the line (a job, server or component), shown in its own column with a colour that stays the same for that source, and matched by New-ConsoleLogRegion -Tag.

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

### -WidgetId
The log name; defaults to the dashboard's log region. In a running app, the key of a log viewport.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
