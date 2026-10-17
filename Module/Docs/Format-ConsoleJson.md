---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleJson
## SYNOPSIS
Shows JSON with syntax highlighting: JSON text, or objects converted with ConvertTo-Json.

A thin convenience over Format-ConsoleCode -Language Json. Strings are taken as JSON text (pipeline strings are joined, so Get-Content works); other objects are collected and converted with ConvertTo-Json -Depth (default 4) first.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleJson [[-InputObject] <Object>] [-Depth <int>] [-LineNumbers] [-MaxLines <int>] [-Title <string>] [-Width <int>] [-AsWidget] [<CommonParameters>]
```

## DESCRIPTION
Shows JSON with syntax highlighting: JSON text, or objects converted with ConvertTo-Json.

A thin convenience over Format-ConsoleCode -Language Json. Strings are taken as JSON text (pipeline strings are joined, so Get-Content works); other objects are collected and converted with ConvertTo-Json -Depth (default 4) first.

## EXAMPLES

### EXAMPLE 1
```powershell
Format-ConsoleJson -AsWidget
```


## PARAMETERS

### -AsWidget
Returns the view instead of writing it.

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

### -Depth
The depth for ConvertTo-Json (default 4).

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

### -InputObject
JSON text or objects (pipeline input).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -LineNumbers
Shows line numbers.

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

### -MaxLines
The most lines shown (default 2000).

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

### -Title
A panel title, or none.

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

### -Width
Lays the output out for this width instead of the terminal's.

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

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
