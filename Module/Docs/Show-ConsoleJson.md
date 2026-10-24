---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleJson
## SYNOPSIS
Shows JSON with syntax highlighting: JSON text re-indented, or objects written as JSON.

A thin convenience over Show-ConsoleCode -Language Json. Pipeline strings are joined (so Get-Content works); when they are JSON they are re-indented, otherwise they are shown as a JSON string. Other objects are collected and written with ConsoleMonster's shared value rules to -Depth levels (default 4): dictionaries with any key type, cycles as "(cycle)", values past the depth as "…", secure strings masked and byte arrays as their size.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleJson [[-InputObject] <Object>] [-Depth <int>] [-LineNumbers] [-MaxLines <int>] [-Title <string>] [-AsString] [-Width <int>] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Shows JSON with syntax highlighting: JSON text re-indented, or objects written as JSON.

A thin convenience over Show-ConsoleCode -Language Json. Pipeline strings are joined (so Get-Content works); when they are JSON they are re-indented, otherwise they are shown as a JSON string. Other objects are collected and written with ConsoleMonster's shared value rules to -Depth levels (default 4): dictionaries with any key type, cycles as "(cycle)", values past the depth as "…", secure strings masked and byte arrays as their size.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process -Id $PID | Select-Object Name, Id, StartTime | Show-ConsoleJson
```

An object as highlighted JSON

### EXAMPLE 2
```powershell
Get-Content .\settings.json | Show-ConsoleJson -LineNumbers
```

A JSON file with line numbers

## PARAMETERS

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
Returns the view instead of writing it.

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

### -Depth
How many levels of objects and lists are written (default 4); deeper values are shown as one line of text.

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

### -InputObject
JSON text or objects (pipeline input).

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

### -LineNumbers
Shows line numbers.

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

### -MaxLines
The most lines shown (default 2000).

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

### -Title
A panel title, or none.

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
