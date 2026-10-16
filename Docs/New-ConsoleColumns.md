---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleColumns
## SYNOPSIS
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleColumns [-Content] <scriptblock> [-Gap <int>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Places console widgets side by side.

Children share the width equally unless they were created with -Weight. Children in a row share their height. On narrow terminals the columns stack vertically instead of squeezing content.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleColumns -Content { }
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
Blank columns between children (default 1).

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

### -Height
Fixed height in rows for this row of columns.

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

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
