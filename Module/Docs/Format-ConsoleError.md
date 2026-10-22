---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleError
## SYNOPSIS
Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.

Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleError [[-InputObject] <Object>] [-AsWidget] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-Compact] [-AsString] [-As <string>] [-Width <int>] [<CommonParameters>]
```

## DESCRIPTION
Shows errors and exceptions readably: type, message, position, script stack, frames and inner errors.

Takes ErrorRecords (from $Error, $_ in a catch block, -ErrorVariable) and .NET exceptions from the pipeline. Each shows the type and the message, the category and error id, PowerShell's position message with the failing line, the script stack, the .NET frames as Type.Method() with shortened paths and file:line links, framework frames collapsed into "+n framework frames", and the inner exceptions as "Caused by". With -AsWidget the views are returned for New-ConsolePanel, dashboards or Export-ConsoleFrame instead of written. Redirected output is plain text.

## EXAMPLES

### EXAMPLE 1
```powershell
$Error[0] | Format-ConsoleError
```

The last error

### EXAMPLE 2
```powershell
try { Get-Item C:\missing -ErrorAction Stop } catch { $_ | Format-ConsoleError -ShowFrameworkFrames }
```

In a catch block, with the framework frames

## PARAMETERS

### -As
Returns semantic Markdown, fenced Text with the exact console layout, or a self-contained Html page. Cannot be combined with -AsString or -AsWidget.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Markdown, Html

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

### -AsWidget
Returns the views instead of writing them.

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

### -Compact
Uses the message-first everyday error layout, highlighted invocation and folded inner exceptions. Stacks remain hidden unless -ShowFrameworkFrames is requested.

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

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
