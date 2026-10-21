---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleException
## SYNOPSIS
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleException [[-InputObject] <Object>] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-AsString] [-As <string>] [-Width <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes an error or exception readably (Format-ConsoleError without -AsWidget).

The same view as Format-ConsoleError: type, message, position, script stack, frames with file:line links, collapsed framework frames and inner errors.

## EXAMPLES

### EXAMPLE 1
```powershell
try { [int]::Parse('x') } catch { Write-ConsoleException $_ }
```

In a catch block

## PARAMETERS

### -As
Returns a Markdown document (fenced rendered text) or a self-contained Html page. Cannot be combined with -AsString or -AsWidget.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Markdown, Html

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsString
Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, PowerShell 7.2+ captures a view typed at the prompt ($x = ..., > file, Out-File); inside scripts and functions, and on Windows PowerShell 5.1, the view is drawn on the screen. Piped into another command (Out-Host, Out-String, Select-String) the view goes on as plain lines, without colour. Cannot be combined with -PassThru.

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

### -InputObject
An ErrorRecord, an exception or a message (pipeline input).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -MaxFrames
The most app frames shown per error (default 8).

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

### -NoStack
Leaves out the stack frames: type, message and position only.

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

### -ShowFrameworkFrames
Shows framework frames (System, Microsoft, the PowerShell engine) instead of collapsing them.

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

### -Width
Lays the output out for this width instead of the terminal's.

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

- `System.Object`

## OUTPUTS

- `None`

## RELATED LINKS

- None
