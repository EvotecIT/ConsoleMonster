---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleRule
## SYNOPSIS
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleRule [[-Title] <Object>] [-Width <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes a horizontal rule, optionally with a title.

A quick section separator for scripts.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleRule 'Step 2: collect'
```

A section header

## PARAMETERS

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
