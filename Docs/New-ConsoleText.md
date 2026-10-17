---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleText
## SYNOPSIS
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Style adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLog and Update-ConsoleLane, and with Write-ConsoleWidget. Without colour (NO_COLOR, redirected output) the text stays plain.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleText [[-Text] <Object[]>] [-Color <Object[]>] [-BackgroundColor <Object[]>] [-Markup <string>] [-Style <string[]>] [-Link <string>] [-Role <ThemeToken>] [-Gradient <Object[]>] [-Alignment <HorizontalAlignment>] [-Plain] [<CommonParameters>]
```

## DESCRIPTION
Creates rich text: multicolour words for table cells, calculated properties, titles, labels, log lines or output on its own.

-Text takes pieces that are shown literally (data is never interpreted); -Color and -BackgroundColor take one colour per piece, and pieces beyond the colours use the first colour, as Write-Color does. Colours are ConsoleColor names exactly as Write-Host shows them, theme roles (Pass, Fail, Warn, Info, Muted, Accent) that follow the light or dark theme, #RRGGBB, rgb(r,g,b) or r,g,b. -Markup adds Spectre-compatible markup ([bold lime]Ready[/]); -Style adds Bold, Dim, Italic, Underline, Strikethrough or Invert; -Link makes the text a hyperlink where the terminal supports it; -Gradient colours the text from one colour to another. The result works as a cell value (also from a calculated property), as the title or label of any widget, with Write-ConsoleLog and Update-ConsoleLane, and with Write-ConsoleWidget. Without colour (NO_COLOR, redirected output) the text stays plain.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleText -Alignment 'Value'
```


## PARAMETERS

### -Alignment
Alignment when the text is written as a widget: Left, Center or Right.

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
Aliases: B, BGC
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Color
Foreground colour per piece: ConsoleColor names (PowerShell mapping), theme roles, #RRGGBB, rgb(r,g,b) or r,g,b.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: C, ForegroundColor, FGC
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Gradient
Two colours: the text is coloured from the first to the second, per character.

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

### -Link
A hyperlink target for the whole text (OSC 8 in Windows Terminal and VS Code, plain text elsewhere).

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

### -Plain
Kept for compatibility: text is literal by default; use -Markup for markup.

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
Aliases: Token
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Style
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

### -Text
The text pieces, shown literally (strings, or rich text from New-ConsoleText).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: T
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object[]`

## OUTPUTS

- `ConsoleMonster.Text.RichText`

## RELATED LINKS

- None
