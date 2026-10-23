---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Invoke-ConsoleRecording
## SYNOPSIS
Records a script in a child PowerShell terminal and exports the result.

Runs the script in an isolated child process. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleRecording [-ScriptBlock] <scriptblock> -Destination <string> [-Theme <ExportTheme>] [-FinalFrame] [-Path <string>] [-Folder <string>] [-Name <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-MaxBytes <long>] [-IdleLimit <double>] [-InProcess] [-TimeoutSeconds <int>] [<CommonParameters>]
```

## DESCRIPTION
Records a script in a child PowerShell terminal and exports the result.

Runs the script in an isolated child process. Caller variables and functions are not inherited. The selected shell must be pwsh or powershell. Saves a local v2 cast plus metadata, then uses the existing renderer for an HTML player, animated SVG, text, Markdown or a final-frame SVG. -InProcess explicitly selects the limited PowerShell output fallback.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-ConsoleRecording { Add-ConsoleRecordingMarker 'Build'; git --version; Write-Host 'Done' -ForegroundColor Green } -Destination ./demo.html
```

Record native coloured output and a chapter

## PARAMETERS

### -Destination
Export path. Extension selects HTML, SVG, cast, text or Markdown.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinalFrame
Export the final frame as a static SVG.

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

### -Folder
Folder for automatic cast names.

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

### -Height
Initial terminal height in cells.

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

### -IdleLimit
Longest playback idle gap in seconds; zero keeps original timing.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxBytes
Maximum cast bytes. At the cap recording stops with a warning.

```yaml
Type: Int64
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Recording title.

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

### -Path
Local asciicast v2 output; omitted names are timestamped.

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

### -Redact
Additional regular expressions to redact from recorded output.

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

### -ScriptBlock
Script to run in the recorded PowerShell child.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Auto, Dark, Light

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TimeoutSeconds
Maximum child lifetime in seconds.

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

### -Width
Initial terminal width in cells.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.IO.FileInfo`

## RELATED LINKS

- None
