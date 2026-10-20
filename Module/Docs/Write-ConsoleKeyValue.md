---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleKeyValue
## SYNOPSIS
Writes an aligned key/value list.

Same parameters as New-ConsoleKeyValue; the list is written immediately.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleKeyValue [[-Data] <Object>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-Markup] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Writes an aligned key/value list.

Same parameters as New-ConsoleKeyValue; the list is written immediately.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Item $PROFILE | Select-Object Name, Length, LastWriteTime | Write-ConsoleKeyValue
```

Show an object's properties

## PARAMETERS

### -Content
A script block that emits New-ConsoleKeyValueItem pairs (or hashtables), added after -Data.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Data
A hashtable or an object whose properties become the pairs (a script block here is taken as -Content).

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

### -InputObject
An object from the pipeline whose properties become the pairs.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
