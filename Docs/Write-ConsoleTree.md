---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleTree
## SYNOPSIS
Writes nested hashtables as a console tree.

Same parameters as New-ConsoleTree; the tree is written immediately.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleTree [-Data] <IDictionary> [-Title <string>] [<CommonParameters>]
```

## DESCRIPTION
Writes nested hashtables as a console tree.

Same parameters as New-ConsoleTree; the tree is written immediately.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleTree -Data @{}
```


## PARAMETERS

### -Data
Nested hashtables describing the tree.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The root label; without it the top-level keys are the roots.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
