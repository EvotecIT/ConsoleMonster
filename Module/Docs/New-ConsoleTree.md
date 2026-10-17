---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTree
## SYNOPSIS
Creates a console tree from nested hashtables.

Each key becomes a node. A nested hashtable (or ordered dictionary) becomes child nodes; an array becomes leaf nodes; any other value is shown as a muted note after the key. Use [ordered] to keep order.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTree [-Data] <IDictionary> [-Title <Object>] [<CommonParameters>]
```

## DESCRIPTION
Creates a console tree from nested hashtables.

Each key becomes a node. A nested hashtable (or ordered dictionary) becomes child nodes; an array becomes leaf nodes; any other value is shown as a muted note after the key. Use [ordered] to keep order.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTree -Data @{}
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
Type: Object
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

- `ConsoleMonster.Widgets.Tree`

## RELATED LINKS

- None
