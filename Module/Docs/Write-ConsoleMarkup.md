---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleMarkup
## SYNOPSIS
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], the 256 xterm colour names of Spectre markup ([red], [cadetblue], [grey50], [deepskyblue3]), [#ff8800], [rgb(1,2,3)], [white on navy], links ([link=https://example.com]text[/] or [link]https://example.com[/], clickable in Windows Terminal and VS Code) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them ([[ and ]]) or with [ConsoleMonster.Text.Markup]::Escape($value). A tag that is not a known style is removed and its text kept when a [/] closes it; an unclosed unknown tag stays literal. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleMarkup [-Text] <string> [-NoNewLine] [<CommonParameters>]
```

## DESCRIPTION
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], the 256 xterm colour names of Spectre markup ([red], [cadetblue], [grey50], [deepskyblue3]), [#ff8800], [rgb(1,2,3)], [white on navy], links ([link=https://example.com]text[/] or [link]https://example.com[/], clickable in Windows Terminal and VS Code) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them ([[ and ]]) or with [ConsoleMonster.Text.Markup]::Escape($value). A tag that is not a known style is removed and its text kept when a [/] closes it; an unclosed unknown tag stays literal. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleMarkup -Text 'Value'
```


## PARAMETERS

### -NoNewLine
Leaves the line open, as Write-Host -NoNewline does. Redirected output still gets plain text.

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

### -Text
The markup.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.String`

## OUTPUTS

- `None`

## RELATED LINKS

- None
