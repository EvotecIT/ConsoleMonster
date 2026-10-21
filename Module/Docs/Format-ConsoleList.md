---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleList
## SYNOPSIS
Formats pipeline objects as record views (labelled values) and writes them to the host.

Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Format-ConsoleTable. Redirected output is plain text.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleList [[-Property] <Object[]>] [-InputObject <psobject>] [-GroupBy <Object>] [-Title <Object>] [-ShowEmpty] [-Depth <int>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-PassThru] [-As <string>] [<CommonParameters>]
```

## DESCRIPTION
Formats pipeline objects as record views (labelled values) and writes them to the host.

Each object becomes a section titled by its key column (the first property, or one marked Key = $true) with the other properties as aligned label/value fields; empty values are hidden unless -ShowEmpty is given. Values go through presenters (statuses, sizes, durations, dates...), and complex values (nested objects, collections) expand as indented sections and nested tables down to -Depth levels. -Property works like Format-List, including calculated properties and the column option keys of Format-ConsoleTable. Redirected output is plain text.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process -Id $PID | Format-ConsoleList -Property Name, Id, @{ Name = 'WorkingSet64'; Presenter = 'Bytes' }, StartTime, Modules -Depth 1
```

A process as a record

## PARAMETERS

### -As
Returns a Markdown document (fenced rendered text) or a self-contained Html page. Cannot be combined with -AsString or -PassThru.

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

### -Culture
The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.

```yaml
Type: CultureInfo
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
How many levels of nested objects expand (default 1; 0 shows nested values as text).

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

### -GroupBy
Groups records under headings by a property name or a scriptblock.

```yaml
Type: Object
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
The objects to show (pipeline input).

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Also writes the input objects to the pipeline.

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

### -Property
Properties (names, wildcards, calculated properties and column option hashtables).

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

### -ShowEmpty
Shows empty values (hidden by default).

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

### -Title
A title on the summary line (markup).

```yaml
Type: Object
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
Lays the records out for this width instead of the terminal's.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
