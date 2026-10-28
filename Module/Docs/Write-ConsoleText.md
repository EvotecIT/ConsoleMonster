---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Write-ConsoleText
## SYNOPSIS
Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.

Writes literal text pieces with Color and BackgroundColor, optional Role and Decoration, links, gradients and terminal-cell padding. ShowTimestamp adds a wall-clock timestamp formatted with TimestampFormat. NoNewLine continues the current line. LogFile records text, CSV or JSON Lines; LogTimestamp and LogFormat control that destination independently. Redirected output is plain text, and jobs and remoting retain the structured rendering. New log files use UTF-8 without a byte order mark; existing logs keep their encoding.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleText [[-Text] <Object[]>] [-Color <Object[]>] [-BackgroundColor <Object[]>] [-StartTab <int>] [-LinesBefore <int>] [-LinesAfter <int>] [-StartSpaces <int>] [-LogFile <string>] [-TimestampFormat <string>] [-LogTimestamp <bool>] [-LogRetry <int>] [-Encoding <string>] [-ShowTimestamp] [-NoNewLine] [-HorizontalCenter] [-NoConsoleOutput] [-Markup <string>] [-Decoration <string[]>] [-Link <string>] [-Role <ThemeToken>] [-Gradient <Object[]>] [-PadLeft <int>] [-PadRight <int>] [-PadCenter <int>] [-PadCharacter <char>] [-Wrap] [-Width <int>] [-SplitLines <string>] [-LogFormat <string>] [-LogSeparator <string>] [-Level <string>] [-Category <string>] [<CommonParameters>]
```

## DESCRIPTION
Writes multicolour text to the console and, optionally, to a log file: Write-Color compatible, with markup, styles, gradients, padding, wrapping and structured logs.

Writes literal text pieces with Color and BackgroundColor, optional Role and Decoration, links, gradients and terminal-cell padding. ShowTimestamp adds a wall-clock timestamp formatted with TimestampFormat. NoNewLine continues the current line. LogFile records text, CSV or JSON Lines; LogTimestamp and LogFormat control that destination independently. Redirected output is plain text, and jobs and remoting retain the structured rendering. New log files use UTF-8 without a byte order mark; existing logs keep their encoding.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleText -Text 'Updating group: ', $Group -Color Gray, Green -StartTab 1
```

Write-Color style

### EXAMPLE 2
```powershell
Write-ConsoleText '1.', ' Option one' -Color Yellow, Green -PadRight 30 -LogFile .\menu.csv -LogFormat Csv -Level Info
```

A menu with padding and a log file in CSV

### EXAMPLE 3
```powershell
Write-ConsoleText '[info] ', "Path exists`n2. line`n3. line" -Color Yellow, White -ShowTimestamp -SplitLines "`n"
```

A multi-line message aligned under the time stamp

## PARAMETERS

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

### -Category
A category for the log record.

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

### -Encoding
The encoding of a new log file: utf8 without a byte order mark by default (Write-Color defaults to unicode, which is UTF-16 LE); Write-Color's names mean the same here (unicode, string and unknown are UTF-16 LE, bigendianunicode UTF-16 BE, utf32 UTF-32 LE), plus utf8BOM and utf8NoBOM. Appending to an existing file always keeps that file's encoding (detected from its byte order mark, or UTF-16 text without one), so a UTF-16 log from Write-Color stays UTF-16; when -Encoding names a different one, a warning says so.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: unknown, string, unicode, bigendianunicode, utf8, utf8BOM, utf8NoBOM, utf7, utf32, ascii, default, oem

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

### -HorizontalCenter
Centres each line in the window (measured in terminal cells, so emoji and CJK centre correctly).

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

### -Level
A level for the log record (Info, Warning, Error...).

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

### -LogFormat
The log format: Text (the Write-Color layout), Csv or JsonLines.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Csv, JsonLines

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LogRetry
Attempts to write the log file before giving up with a warning (default 2).

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

### -LogSeparator
The CSV separator (default a comma).

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

### -LogTimestamp
Whether log lines start with the time (default $true).

```yaml
Type: Boolean
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

### -NoConsoleOutput
Writes only to the log file.

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

### -PadCenter
Centres the text within this many cells.

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

### -PadCharacter
The padding character (default a space).

```yaml
Type: Char
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PadLeft
Pads the text on the left to this many cells (right-aligned).

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

### -PadRight
Pads the text on the right to this many cells.

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

### -Role
A theme role for pieces without their own colour (Pass, Fail, Warn, Info, Muted, Accent...).

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

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

### -StartSpaces
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

### -StartTab
Tabs before the text (eight cells each).

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
The width for -Wrap and -HorizontalCenter (default the window width).

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
