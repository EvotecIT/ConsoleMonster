---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleRule
## SYNOPSIS
Creates a horizontal rule, optionally with a title.

The line uses the border colour and the glyph tier's line character.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRule [[-Title] <Object>] [-Alignment <HorizontalAlignment>] [<CommonParameters>]
```

## DESCRIPTION
Creates a horizontal rule, optionally with a title.

The line uses the border colour and the glyph tier's line character.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleRule -Alignment 'Value'
```


## PARAMETERS

### -Alignment
Title alignment: Left, Center or Right.

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

### -Title
The title (markup allowed).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.Rule`

## RELATED LINKS

- None
