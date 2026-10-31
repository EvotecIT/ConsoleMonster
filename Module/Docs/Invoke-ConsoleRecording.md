---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Invoke-ConsoleRecording
## SYNOPSIS
Records a script, an app on a headless terminal, or an interactive child shell and saves the result.

Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. With -Headless the script runs here, against a headless terminal of -Width by -Height cells, and -InputScript drives it (key names, quoted text, wait:250 pauses, resize:80x24); the recording holds exactly what an app shows, including its log panel, footer and dialogs, and the app ends when the keys run out. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG; Export-ConsoleRecording turns the cast into GIF or APNG. -InProcess explicitly selects the limited PowerShell output fallback.

## SYNTAX
### Script (Default)
```powershell
Invoke-ConsoleRecording [-ScriptBlock] <scriptblock> -Path <string> [-Theme <ExportTheme>] [-ChromeStyle <WindowChromeStyle>] [-FinalFrame] [-RecordingPath <string>] [-Title <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimitSeconds <double>] [-InProcess] [-TimeoutSeconds <double>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Headless
```powershell
Invoke-ConsoleRecording [-ScriptBlock] <scriptblock> -Path <string> -Headless [-Theme <ExportTheme>] [-ChromeStyle <WindowChromeStyle>] [-FinalFrame] [-InputScript <string>] [-RecordingPath <string>] [-Title <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimitSeconds <double>] [-InProcess] [-TimeoutSeconds <double>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Interactive
```powershell
Invoke-ConsoleRecording -Interactive [-Path <string>] [-Title <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimitSeconds <double>] [-InProcess] [-TimeoutSeconds <double>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Records a script, an app on a headless terminal, or an interactive child shell and saves the result.

Runs the script in an isolated child process, or opens a child shell with Interactive until it exits. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. With -Headless the script runs here, against a headless terminal of -Width by -Height cells, and -InputScript drives it (key names, quoted text, wait:250 pauses, resize:80x24); the recording holds exactly what an app shows, including its log panel, footer and dialogs, and the app ends when the keys run out. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG; Export-ConsoleRecording turns the cast into GIF or APNG. -InProcess explicitly selects the limited PowerShell output fallback.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-ConsoleRecording { Add-ConsoleRecordingMarker 'Build'; Get-Date; Write-Host 'Done' -ForegroundColor Green } -Path ./demo.html -WhatIf
```

Preview recording coloured output and a chapter

### EXAMPLE 2
```powershell
Invoke-ConsoleRecording -Interactive -Path ./session.cast -WhatIf
```

Preview recording a child shell

### EXAMPLE 3
```powershell
Invoke-ConsoleRecording -Headless -Width 100 -Height 30 -InputScript 'wait:800 Down Down wait:600 Enter wait:1200 q' -Path ./app.cast -WhatIf {
    Show-ConsoleApp -Content { New-ConsoleSelect -Key Color -Choices Red, Green, Blue }
}
Export-ConsoleRecording -RecordingPath ./app.cast -Path ./app.gif -WhatIf
```

Preview recording an app driven by keys, then make a GIF of it

## PARAMETERS

### -ChromeStyle
MacOS (default), Windows, Linux or None. Choose None to hide chrome.

```yaml
Type: WindowChromeStyle
Parameter Sets: Script, Headless
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: MacOS
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinalFrame
Export the final frame as a static SVG.

```yaml
Type: SwitchParameter
Parameter Sets: Script, Headless
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Headless
Runs the script in this session against a headless terminal and records what it shows. Caller variables and functions are available; only ConsoleMonster output is recorded.

```yaml
Type: SwitchParameter
Parameter Sets: Headless
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Initial terminal height in cells.

```yaml
Type: Int32
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InProcess
Explicit limited fallback: PowerShell host output in this process; no native terminal capture. Not used with -Headless.

```yaml
Type: SwitchParameter
Parameter Sets: Script, Headless, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputScript
Input for -Headless: key names (Down, Enter, Ctrl+K, F2), repeats (Down*3), quoted text, paste:"text", resize:80x24 and wait:250 pauses. Input ends after the last key.

```yaml
Type: String
Parameter Sets: Headless
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
Parameter Sets: Script, Headless, Interactive
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Export path for a script; Interactive writes a cast here, or uses a timestamped name when omitted.

```yaml
Type: String
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless
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
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shell
Shell executable. The default is the current PowerShell executable. Not used with -Headless.

```yaml
Type: String
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless
Aliases: None
Possible values: Auto, Dark, Light, HighContrast

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutSeconds
Maximum child lifetime in seconds. Not used with -Headless, where the script runs in this session.

```yaml
Type: Double
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless, Interactive
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
Parameter Sets: Script, Headless, Interactive
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

- `System.IO.FileInfo`

## RELATED LINKS

- None
