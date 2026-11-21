---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolepresentation/
schema: 2.0.0
---
# Show-ConsolePresentation
## SYNOPSIS
Presents a scripted demo one command at a time in a real shell.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsolePresentation [-ScriptPath] <string> [-Path <string>] [-Width <int>] [-Height <int>] [-Seed <int>] [-StepTimeoutSeconds <double>] [-Jitter <double>] [-IdleLimitSeconds <double>] [-Redact <string[]>] [-Shell <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
ScriptPath selects the demo script that runs in a child PowerShell process.

### Step-through presentation

Run without Path to control the steps from the terminal. Enter types and runs the current command. Space types it without running it, b moves back, s skips a step and q quits. The title shows the step counter. Moving back preserves effects from commands already run. Stop or cancellation ends the owned child process and restores the terminal title. Temporary capture files are removed on exit.

### Unattended recording

Supplying Path runs the complete presentation unattended, saves the child output as an asciicast, and returns a FileInfo for the recording.

## EXAMPLES

### EXAMPLE 1
```powershell
# WhatIf previews the operation without executing the script or writing the recording.
            $presentationOptions = @{
    WhatIf = $true
    ScriptPath = './demo.ps1'
    Path = './talk.cast'
}
Show-ConsolePresentation @presentationOptions
```

Preview presenting and recording a demo script

### EXAMPLE 2
```powershell
$presentationOptions = @{
    WhatIf = $true
    ScriptPath = './inventory-demo.ps1'
}
Show-ConsolePresentation @presentationOptions
```

Preview presenting an inventory demonstration

## PARAMETERS

### -Height
Terminal height in cells.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 28
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
Default value: 2
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
Default value: 0.2
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The asciicast output file. Supplying Path records the complete presentation unattended. Without it, keys control the presentation and temporary output is removed.

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

### -ScriptPath
The input PowerShell demo script, with optional # demo: directives.

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

### -Seed
Seed for repeatable character timing.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 42
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
Default value: 30
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
Default value: 0
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

- [https://consolemonster.dev/powershell/commands/show-consolepresentation/](https://consolemonster.dev/powershell/commands/show-consolepresentation/)
