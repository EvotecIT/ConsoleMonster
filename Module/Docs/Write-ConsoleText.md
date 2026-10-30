---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Write-ConsoleText
## SYNOPSIS
Writes coloured text with optional markup, alignment, wrapping and a text log.

Text is literal; Color and BackgroundColor apply per piece. Role, Decoration, Link and Gradient apply to the message. Indent adds spaces and Alignment places text within Width. ShowTimestamp adds a timestamp. NoNewLine continues the current line. LogFile appends plain text with timestamps; new logs use UTF-8 and existing logs retain their encoding. Redirected output is plain text.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleText [[-Text] <Object[]>] [-Indent <int>] [-Alignment <HorizontalAlignment>] [-Color <Object[]>] [-BackgroundColor <Object[]>] [-LinesBefore <int>] [-LinesAfter <int>] [-LogFile <string>] [-TimestampFormat <string>] [-ShowTimestamp] [-NoNewLine] [-Markup <string>] [-Decoration <string[]>] [-Link <string>] [-Role <ThemeToken>] [-Gradient <Object[]>] [-Wrap] [-Width <int>] [-SplitLines <string>] [<CommonParameters>]
```

## DESCRIPTION
Writes coloured text with optional markup, alignment, wrapping and a text log.

Text is literal; Color and BackgroundColor apply per piece. Role, Decoration, Link and Gradient apply to the message. Indent adds spaces and Alignment places text within Width. ShowTimestamp adds a timestamp. NoNewLine continues the current line. LogFile appends plain text with timestamps; new logs use UTF-8 and existing logs retain their encoding. Redirected output is plain text.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleText -Text 'Checking group: ', 'Operators' -Color Gray, Green -Indent 4
```

Colour two pieces and indent the message

### EXAMPLE 2
```powershell
Write-ConsoleText 'Maintenance complete' -Role Pass -Alignment Center -Width 60
```

Centre a heading within sixty columns

### EXAMPLE 3
```powershell
Write-ConsoleText '[info] ', "Path exists`n2. line`n3. line" -Color Yellow, White -ShowTimestamp -SplitLines "`n"
```

A multi-line message aligned under the time stamp

## PARAMETERS

### -Alignment
Left, Center or Right within the available width.

```yaml
Type: HorizontalAlignment
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Left, Center, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BackgroundColor
Background colour per piece.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Color
Foreground colour per piece: ConsoleColor names (exactly as Write-Host shows them: DarkRed is ANSI red, Red bright red; grey spells Gray), theme roles, #RRGGBB, rgb(r,g,b), r,g,b, or a markup palette name (cadetblue; palette.darkred for the palette colour where the bare name is a ConsoleColor).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Decoration
Decorations for the whole text: Bold, Dim, Italic, Underline, Strikethrough, Invert.

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

### -Gradient
Two colours: the text is coloured from the first to the second (per character; steps on 16-colour terminals).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Indent
Spaces before the text.

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

### -LinesAfter
Empty lines after the text.

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

### -LinesBefore
Empty lines before the text.

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

### -Link
A hyperlink target for the whole text (OSC 8 where supported).

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

### -LogFile
A log file the text is appended to (plain text, see -LogFormat).

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

### -Markup
Markup added after the pieces ([bold lime]Ready[/] in [muted]3s[/]).

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

### -NoNewLine
Leaves the line open, so the next output continues on it.

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

### -Role
A theme role for pieces without their own colour (Pass, Fail, Warn, Info, Muted, Accent...).

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus, Strong, Low, Critical, Gap

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowTimestamp
Starts the line with the time in brackets.

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

### -SplitLines
Splits the text at this separator; the following lines are aligned under the first, after the time and prefix.

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

### -Text
Text pieces, shown literally (strings, or rich text from New-ConsoleText).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -TimestampFormat
The time format for -ShowTimestamp and the log (default yyyy-MM-dd HH:mm:ss).

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
The width for -Wrap and -Alignment (default the window width).

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

### -Wrap
Wraps long text at the window width, continuing under the text after the time and prefix.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object[]`

## OUTPUTS

- `None`

## RELATED LINKS

- None
