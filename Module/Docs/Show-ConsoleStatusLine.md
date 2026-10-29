---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleStatusLine
## SYNOPSIS
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

Each call replaces the line (Write-Color style pieces, colours, -Markup, -Decoration and -Role as in Write-ConsoleText). -Spinner, -ShowTimestamp and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleStatusLine [[-Text] <Object[]>] [-Color <Object[]>] [-BackgroundColor <Object[]>] [-Markup <string>] [-Decoration <string[]>] [-Role <ThemeToken>] [-Status <StatusKind>] [-Spinner] [-ShowTimestamp] [-ShowElapsed] [-TimestampFormat <string>] [-Complete] [<CommonParameters>]
```

## DESCRIPTION
Shows one status line that updates in place, with an optional spinner, clock and elapsed time.

Each call replaces the line (Write-Color style pieces, colours, -Markup, -Decoration and -Role as in Write-ConsoleText). -Spinner, -ShowTimestamp and -ShowElapsed keep refreshing about ten times a second between calls. -Complete writes the final line (with -Status Pass, Fail or Warn for a marker) and moves on. The refresh stops by itself when other output or the prompt moves the cursor, so it never draws over them. In redirected output, CI, transcripts, jobs and other non-console hosts each call writes a normal line instead (unchanged repeats are skipped).

## EXAMPLES

### EXAMPLE 1
```powershell
foreach ($file in $files) { Show-ConsoleStatusLine 'Copying ', $file.Name -Color Gray, Cyan -Spinner -ShowElapsed; Copy-Item $file.FullName $target }
Show-ConsoleStatusLine "Copied $($files.Count) files" -Status Pass -ShowElapsed -Complete
```

A copy loop with a spinner and elapsed time

### EXAMPLE 2
```powershell
Show-ConsoleStatusLine 'You have waited ' -Color Green -BackgroundColor Blue -ShowTimestamp -ShowElapsed; Start-Sleep 5; Show-ConsoleStatusLine 'Done' -Status Pass -Complete
```

A waiting line with a clock (PSWriteColor issue #15)

## PARAMETERS

### -BackgroundColor
Background colour per piece.

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

### -Color
Foreground colour per piece.

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

### -Complete
Writes the final version of the line and ends it.

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

### -Decoration
Decorations: Bold, Dim, Italic, Underline, Strikethrough, Invert.

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

### -Markup
Markup added after the pieces.

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

### -Role
A theme role for pieces without their own colour.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus, Strong, Low, Critical, Gap

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowElapsed
Shows the time since the status line started.

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

### -ShowTimestamp
Shows the time of day, refreshed every second.

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

### -Spinner
Shows a spinner while the status is open.

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

### -Status
A status marker before the text (Pass, Fail, Warn, Info, Running...).

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Text
Text pieces, shown literally (strings, or rich text from New-ConsoleText).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimestampFormat
The clock format (default HH:mm:ss).

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
