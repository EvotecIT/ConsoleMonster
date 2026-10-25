---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Invoke-ConsoleRecording
## SYNOPSIS
Records a script or an interactive child shell and saves the result.

Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

## SYNTAX
### Script (Default)
```powershell
Invoke-ConsoleRecording [-ScriptBlock] <scriptblock> -Destination <string> [-Theme <ExportTheme>] [-NoWindowChrome] [-ChromeStyle <WindowChromeStyle>] [-FinalFrame] [-RecordingPath <string>] [-Folder <string>] [-Title <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimitSeconds <double>] [-InProcess] [-TimeoutSeconds <double>] [<CommonParameters>]
```

### Interactive
```powershell
Invoke-ConsoleRecording -Interactive [-Destination <string>] [-Folder <string>] [-Title <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimitSeconds <double>] [-InProcess] [-TimeoutSeconds <double>] [<CommonParameters>]
```

## DESCRIPTION
Records a script or an interactive child shell and saves the result.

Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-ConsoleRecording { Add-ConsoleRecordingMarker 'Build'; git --version; Write-Host 'Done' -ForegroundColor Green } -Destination ./demo.html
```

Record native coloured output and a chapter

### EXAMPLE 2
```powershell
Invoke-ConsoleRecording -Interactive -Destination ./session.cast
```

Record an interactive shell

## PARAMETERS

### -ChromeStyle
MacOS (default), Windows, Linux or None. None or -NoWindowChrome hides chrome.

```yaml
Type: WindowChromeStyle
Parameter Sets: Script
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: MacOS
Accept pipeline input: False
Accept wildcard characters: False
```

### -Destination
Export path for a script; Interactive writes a cast here, or uses a timestamped name when omitted.

```yaml
Type: String
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinalFrame
Export the final frame as a static SVG.

```yaml
Type: SwitchParameter
Parameter Sets: Script
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Folder
Folder for automatic cast names.

```yaml
Type: String
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Initial terminal height in cells.

```yaml
Type: Int32
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IdleLimitSeconds
Asciicast player idle-limit hint in seconds. Source timestamps are always preserved; zero disables the hint.

```yaml
Type: Double
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InProcess
Explicit limited fallback: PowerShell host output in this process; no native terminal capture.

```yaml
Type: SwitchParameter
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Interactive
Records an interactive child shell until it exits or stops recording.

```yaml
Type: SwitchParameter
Parameter Sets: Interactive
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxBytes
Maximum cast bytes. At the cap recording stops with a warning.

```yaml
Type: Int64
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoWindowChrome
Hides the default export title bar.

```yaml
Type: SwitchParameter
Parameter Sets: Script
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordingPath
Local asciicast v2 output; omitted names are timestamped.

```yaml
Type: String
Parameter Sets: Script
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Redact
Additional regular expressions to redact from recorded output.

```yaml
Type: String[]
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RedactionText
Visible replacement for matched output (default [REDACTED]). No control characters.

```yaml
Type: String
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: '[REDACTED]'
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScriptBlock
Script to run in the recorded PowerShell child.

```yaml
Type: ScriptBlock
Parameter Sets: Script
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shell
Shell executable. The default is the current PowerShell executable.

```yaml
Type: String
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Theme
Export colours.

```yaml
Type: ExportTheme
Parameter Sets: Script
Aliases: None
Possible values: Auto, Dark, Light, HighContrast

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutSeconds
Maximum child lifetime in seconds.

```yaml
Type: Double
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
Recording title.

```yaml
Type: String
Parameter Sets: Script, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Initial terminal width in cells.

```yaml
Type: Int32
Parameter Sets: Script, Interactive
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

- `System.IO.FileInfo`

## RELATED LINKS

- None
