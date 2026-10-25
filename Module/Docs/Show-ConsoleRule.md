---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleRule
## SYNOPSIS
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleRule [[-Title] <Object>] [-Width <int>] [-Alignment <HorizontalAlignment>] [-AsString] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleRule 'Step 2: collect'
```

A section header

## PARAMETERS

### -Alignment
Alignment of the section title (default left, as on the rule builder).

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

### -AsString
Returns plain text without drawing.

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
Returns the rule widget without drawing.

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

### -Width
The width in cells (default the window), to match other output laid out with -Width.

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

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
