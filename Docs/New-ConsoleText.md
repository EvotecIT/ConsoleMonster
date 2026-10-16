---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleText
## SYNOPSIS
Creates a text block from Spectre-compatible markup.

Markup such as [bold], [red], [#ff8800 on navy] and theme tokens such as [pass], [warn] or [muted] is supported. Use -Plain to show brackets literally. Text wraps to the available width.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleText [-Text] <string> [-Plain] [-Alignment <HorizontalAlignment>] [-Token <ThemeToken>] [<CommonParameters>]
```

## DESCRIPTION
Creates a text block from Spectre-compatible markup.

Markup such as [bold], [red], [#ff8800 on navy] and theme tokens such as [pass], [warn] or [muted] is supported. Use -Plain to show brackets literally. Text wraps to the available width.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleText -Text 'Value'
```


## PARAMETERS

### -Alignment
Alignment: Left, Center or Right.

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

### -Plain
Show the text literally, without markup.

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
The text (markup unless -Plain).

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

### -Token
A theme token applied to the whole text (Muted, Accent, Pass, Fail, Warn...).

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6

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

- `ConsoleMonster.Widgets.TextBlock`

## RELATED LINKS

- None
