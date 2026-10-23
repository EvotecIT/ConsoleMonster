---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Start-ConsoleRecording
## SYNOPSIS
Opens a recorded child shell in a pseudo-console.

Records terminal output and resizes to local asciicast v2. Exit the child shell or run Stop-ConsoleRecording inside it to finalize. Input bytes are never stored; output secret patterns are redacted before writing. The title shows a REC indicator. Use -InProcess for explicitly limited PowerShell output capture when pseudo-consoles are unavailable.

## SYNTAX
### __AllParameterSets
```powershell
Start-ConsoleRecording [-Path <string>] [-Folder <string>] [-Name <string>] [-Shell <string>] [-Width <int>] [-Height <int>] [-Redact <string[]>] [-RedactionText <string>] [-MaxBytes <long>] [-IdleLimit <double>] [-InProcess] [-TimeoutSeconds <int>] [<CommonParameters>]
```

## DESCRIPTION
Opens a recorded child shell in a pseudo-console.

Records terminal output and resizes to local asciicast v2. Exit the child shell or run Stop-ConsoleRecording inside it to finalize. Input bytes are never stored; output secret patterns are redacted before writing. The title shows a REC indicator. Use -InProcess for explicitly limited PowerShell output capture when pseudo-consoles are unavailable.

## EXAMPLES

### EXAMPLE 1
```powershell
Start-ConsoleRecording -Path ./demo.cast -Name 'Deployment'
```

Record a PowerShell session

## PARAMETERS

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

### -RedactionText
Visible replacement for matched output (default [REDACTED]). No control characters.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: '[REDACTED]'
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
