---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleCode
## SYNOPSIS
Shows source text with syntax highlighting, line numbers and highlighted lines.

Highlights JSON, XML/HTML, YAML, PowerShell, C#, SQL, INI/TOML, CSV/TSV, diffs, log lines and Markdown, or plain text with -Language Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings). -Language Auto (the default) picks the language from the file extension, then from the content, and falls back to plain text. Text comes from -Path or from pipeline strings (joined as lines). The text is always literal: markup is not read and control characters are dropped. -LineNumbers adds a gutter, -Highlight marks lines (10, '10-14'), long lines are truncated with an ellipsis or wrapped with -Wrap, and output stops after -MaxLines lines with "+n more lines". A file is shown on a panel titled with its name (-Title changes it, -NoTitle leaves the panel out). Redirected output is plain text.

## SYNTAX
### Text (Default)
```powershell
Format-ConsoleCode [[-InputObject] <Object>] [-Language <string>] [-LineNumbers] [-Highlight <string[]>] [-FirstLine <int>] [-Wrap] [-MaxLines <int>] [-Title <string>] [-NoTitle] [-Width <int>] [-AsWidget] [<CommonParameters>]
```

### Path
```powershell
Format-ConsoleCode -Path <string> [-Language <string>] [-LineNumbers] [-Highlight <string[]>] [-FirstLine <int>] [-Wrap] [-MaxLines <int>] [-Title <string>] [-NoTitle] [-Width <int>] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Shows source text with syntax highlighting, line numbers and highlighted lines.

Highlights JSON, XML/HTML, YAML, PowerShell, C#, SQL, INI/TOML, CSV/TSV, diffs, log lines and Markdown, or plain text with -Language Smart (numbers, booleans, GUIDs, IP addresses, URLs, paths, dates, DNs, quoted strings). -Language Auto (the default) picks the language from the file extension, then from the content, and falls back to plain text. Text comes from -Path or from pipeline strings (joined as lines). The text is always literal: markup is not read and control characters are dropped. -LineNumbers adds a gutter, -Highlight marks lines (10, '10-14'), long lines are truncated with an ellipsis or wrapped with -Wrap, and output stops after -MaxLines lines with "+n more lines". A file is shown on a panel titled with its name (-Title changes it, -NoTitle leaves the panel out). Redirected output is plain text.

## EXAMPLES

### EXAMPLE 1
```powershell
Format-ConsoleCode -AsWidget
```


### EXAMPLE 2
```powershell
Format-ConsoleCode -Path 'C:\Path'
```


## PARAMETERS

### -AsWidget
Returns the view instead of writing it.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FirstLine
The number of the first line (default 1), for excerpts.

```yaml
Type: Int32
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Highlight
Lines to mark: numbers or ranges such as 10, '10-14'.

```yaml
Type: String[]
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Text to show (pipeline strings are joined as lines).

```yaml
Type: Object
Parameter Sets: Text
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Language
The language: Auto (default), Json, Xml, Yaml, PowerShell, CSharp, Sql, Ini, Csv, Diff, Log, Markdown, Smart, Text, or a language registered from C#.

```yaml
Type: String
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineNumbers
Shows line numbers.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
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
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoTitle
Leaves the panel out.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A file to show.

```yaml
Type: String
Parameter Sets: Path
Aliases: FullName
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The panel title (default the file name with -Path; none for pipeline text).

```yaml
Type: String
Parameter Sets: Text, Path
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
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wrap
Wraps long lines instead of truncating them.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
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
