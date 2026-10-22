---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Watch-ConsoleLog
## SYNOPSIS
Follows a log file or a local Windows event log with a live, scrollable log view.

File lines are decoded incrementally and followed across observed truncation and rotation. Event logs use Get-WinEvent on Windows. The view follows new entries; scrolling pauses following and End resumes it. Filter and Highlight are literal case-insensitive substrings. Piped, redirected, CI and -Plain output is ordinary literal text and never reads keyboard input. Count is source polls, as in Watch-Console: one takes a snapshot, zero follows until q, Escape or Ctrl+C. Capacity bounds retained history. The default interval is 0.25 seconds.

## SYNTAX
### Path (Default)
```powershell
Watch-ConsoleLog [-Path] <string> [-Encoding <string>] [-Tail <int>] [-Count <long>] [-Interval <double>] [-Filter <string>] [-Highlight <string>] [-Capacity <int>] [-Plain] [<CommonParameters>]
```

### EventLog
```powershell
Watch-ConsoleLog -LogName <string> [-Tail <int>] [-Count <long>] [-Interval <double>] [-Filter <string>] [-Highlight <string>] [-Capacity <int>] [-Plain] [<CommonParameters>]
```

## DESCRIPTION
Follows a log file or a local Windows event log with a live, scrollable log view.

File lines are decoded incrementally and followed across observed truncation and rotation. Event logs use Get-WinEvent on Windows. The view follows new entries; scrolling pauses following and End resumes it. Filter and Highlight are literal case-insensitive substrings. Piped, redirected, CI and -Plain output is ordinary literal text and never reads keyboard input. Count is source polls, as in Watch-Console: one takes a snapshot, zero follows until q, Escape or Ctrl+C. Capacity bounds retained history. The default interval is 0.25 seconds.

## EXAMPLES

### EXAMPLE 1
```powershell
Watch-ConsoleLog -Path ./service.log -Highlight database
```

Follow a file and emphasize a component.

### EXAMPLE 2
```powershell
Watch-ConsoleLog -Path ./service.log -Tail 50 -Filter ERROR -Count 1 | Out-File ./errors.txt
```

Capture the current matching lines as ordinary strings.

### EXAMPLE 3
```powershell
Watch-ConsoleLog -LogName Application -Tail 20
```

Follow a local Windows event log.

## PARAMETERS

### -Capacity
Maximum retained history lines, default 1000.

```yaml
Type: Int32
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Count
Source polls before returning, or zero to follow until cancelled.

```yaml
Type: Int64
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Encoding
File encoding without a BOM, default utf8. A UTF-8, UTF-16 or UTF-32 BOM takes precedence. Uses the same encoding names as Write-ConsoleText.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values: unknown, string, unicode, bigendianunicode, utf8, utf8BOM, utf8NoBOM, utf32, ascii, default, oem

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Filter
Literal case-insensitive text required in a line. Null or empty accepts every line.

```yaml
Type: String
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Highlight
Literal case-insensitive text emphasized in live output. Plain output stays unchanged.

```yaml
Type: String
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Interval
Seconds between polls, from 0.05 to 86400; defaults to 0.25.

```yaml
Type: Double
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogName
One literal local Windows event-log name, such as Application or System. Wildcards are not supported.

```yaml
Type: String
Parameter Sets: EventLog
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
One literal FileSystem path. Wildcards are not expanded.

```yaml
Type: String
Parameter Sets: Path
Aliases: LiteralPath
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Plain
Writes literal lines without a live screen or keyboard input.

```yaml
Type: SwitchParameter
Parameter Sets: Path, EventLog
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Tail
Completed history lines or events shown initially; zero starts with later entries.

```yaml
Type: Int32
Parameter Sets: Path, EventLog
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

- `System.String`

## RELATED LINKS

- None
