---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleMarkdown
## SYNOPSIS
Reads Markdown with native console tables, highlighted code and interactive paging.

Reads CommonMark with pipe tables through the optional OfficeIMO-backed add-on. Headings use accent rules, lists have hanging indents, and block quotes have a vertical bar. Code uses the registered highlighter and its language label. Links use OSC 8 when supported and show a muted URL otherwise. HTML stays literal, images become labelled links, and document content is never executed or fetched. Long documents page only when output goes directly to an interactive host. Piped, redirected, recorded and non-interactive output never acquires input. Pipeline strings are joined before parsing, preserving reference links and block structure. Works on Windows PowerShell 5.1 and PowerShell 7.

## SYNTAX
### Content (Default)
```powershell
Show-ConsoleMarkdown [-Content] <string> [-Width <int>] [-NoPager] [-AsString] [-AsWidget] [<CommonParameters>]
```

### Path
```powershell
Show-ConsoleMarkdown -Path <string> [-Width <int>] [-NoPager] [-AsString] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Reads Markdown with native console tables, highlighted code and interactive paging.

Reads CommonMark with pipe tables through the optional OfficeIMO-backed add-on. Headings use accent rules, lists have hanging indents, and block quotes have a vertical bar. Code uses the registered highlighter and its language label. Links use OSC 8 when supported and show a muted URL otherwise. HTML stays literal, images become labelled links, and document content is never executed or fetched. Long documents page only when output goes directly to an interactive host. Piped, redirected, recorded and non-interactive output never acquires input. Pipeline strings are joined before parsing, preserving reference links and block structure. Works on Windows PowerShell 5.1 and PowerShell 7.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleMarkdown -Path .\README.md
```

Read a local document, paging only when it exceeds the interactive screen

### EXAMPLE 2
```powershell
Show-ConsoleMarkdown -Content '# Deployment notes' -NoPager
```

Render inline content without a pager

### EXAMPLE 3
```powershell
Get-Content .\deployment.md | Show-ConsoleMarkdown -Width 60 -AsString
```

Join pipeline source lines and return the complete plain rendering

## PARAMETERS

### -AsString
Returns the complete view as one plain string and never opens the pager.

```yaml
Type: SwitchParameter
Parameter Sets: Content, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the view widget without drawing or acquiring input.

```yaml
Type: SwitchParameter
Parameter Sets: Content, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
The Markdown source. Pipeline strings are joined with newlines before parsing.

```yaml
Type: String
Parameter Sets: Content
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -NoPager
Prints the entire document without acquiring interactive input.

```yaml
Type: SwitchParameter
Parameter Sets: Content, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A literal file path, resolved relative to the current PowerShell FileSystem location.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
The layout width in terminal cells. Omit it to use the current terminal width.

```yaml
Type: Int32
Parameter Sets: Content, Path
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

- `System.String`

## OUTPUTS

- `System.String`

## RELATED LINKS

- None
