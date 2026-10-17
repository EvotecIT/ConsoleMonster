---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleKeyValue
## SYNOPSIS
Creates an aligned key/value list.

Keys come from a hashtable (use [ordered] to keep order) or from an object's properties. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. In a live dashboard, update pairs with Set-ConsoleValue.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleKeyValue [[-Data] <Object>] [-Markup] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates an aligned key/value list.

Keys come from a hashtable (use [ordered] to keep order) or from an object's properties. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. In a live dashboard, update pairs with Set-ConsoleValue.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleKeyValue -Name 'Name'
```


## PARAMETERS

### -Data
A hashtable or an object whose properties become the pairs.

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

### -Markup
Treat values as markup.

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

### -Name
The live name used by Set-ConsoleValue (default keyvalue).

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

- `ConsoleMonster.Widgets.KeyValueList`

## RELATED LINKS

- None
