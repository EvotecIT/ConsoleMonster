---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleRows
## SYNOPSIS
Stacks console widgets vertically.

Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRows [-Content] <scriptblock> [-Gap <int>] [-Weight <double>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Stacks console widgets vertically.

Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleRows -Content { }
```


## PARAMETERS

### -Content
Scriptblock that emits the child widgets.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Fill
Take the remaining height of a live dashboard.

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

### -Gap
Blank rows between children.

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

### -Weight
Share of the space this group takes inside its parent.

```yaml
Type: Double
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

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
