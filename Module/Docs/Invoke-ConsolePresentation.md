---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Invoke-ConsolePresentation
## SYNOPSIS
Types a scripted PowerShell demo into a real terminal and records it.

Runs plain PowerShell with optional chapter, export pace, export hold, recording pause, setup, type-only, narration, WPM and output-wait directives. Uses OSC 133 readiness marks independent of visible prompt text and operating system. Character jitter uses a fixed seed; use fixed data and environment for repeatable output.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsolePresentation [-Path] <string> [-Destination] <string> [-Width <int>] [-Height <int>] [-Seed <int>] [-StepTimeoutSeconds <double>] [-Jitter <double>] [-IdleLimitSeconds <double>] [-Redact <string[]>] [-Shell <string>] [<CommonParameters>]
```

## DESCRIPTION
Types a scripted PowerShell demo into a real terminal and records it.

Runs plain PowerShell with optional chapter, export pace, export hold, recording pause, setup, type-only, narration, WPM and output-wait directives. Uses OSC 133 readiness marks independent of visible prompt text and operating system. Character jitter uses a fixed seed; use fixed data and environment for repeatable output.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-ConsolePresentation -Path ./demo.ps1 -Destination ./talk.cast -Seed 42
```

Record a talk

## PARAMETERS

### -Destination
Destination asciicast v2 recording. Existing recordings are never overwritten.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Terminal height in cells.

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

### -IdleLimitSeconds
Asciicast player idle-limit hint in seconds. Source timestamps are always preserved; zero disables the hint.

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

### -Jitter
Fractional character-delay variation from zero to one; default 0.2.

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

### -Path
Plain PowerShell demo file with optional # demo: directives.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Redact
Output patterns redacted before recording.

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

### -Seed
Seed for repeatable character timing.

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

### -Shell
Child PowerShell executable. Defaults to the current PowerShell.

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

### -StepTimeoutSeconds
Maximum wait for a semantic prompt or output pattern.

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

### -Width
Terminal width in cells.

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
