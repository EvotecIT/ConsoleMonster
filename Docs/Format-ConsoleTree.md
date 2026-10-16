---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Format-ConsoleTree
## SYNOPSIS
Shows the structure of pipeline objects as a tree (or an inspector view) and writes it to the host.

Works for any object: PSCustomObjects, .NET objects, hashtables and dictionaries, nested to -Depth levels. Collections list up to -MaxItems items followed by "+N more"; reference cycles and the depth limit are marked instead of recursing forever. -Include and -Exclude filter member names (wildcards). With -AsInspector the object is shown as a record view: nested objects as indented sections and uniform collections as nested tables. Redirected output is plain text.

## SYNTAX
### __AllParameterSets
```powershell
Format-ConsoleTree [-InputObject <psobject>] [-Depth <int>] [-MaxItems <int>] [-Include <string[]>] [-Exclude <string[]>] [-Title <string>] [-AsInspector] [-Width <int>] [-Culture <cultureinfo>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Shows the structure of pipeline objects as a tree (or an inspector view) and writes it to the host.

Works for any object: PSCustomObjects, .NET objects, hashtables and dictionaries, nested to -Depth levels. Collections list up to -MaxItems items followed by "+N more"; reference cycles and the depth limit are marked instead of recursing forever. -Include and -Exclude filter member names (wildcards). With -AsInspector the object is shown as a record view: nested objects as indented sections and uniform collections as nested tables. Redirected output is plain text.

## EXAMPLES

### EXAMPLE 1
```powershell
Format-ConsoleTree -AsInspector
```


## PARAMETERS

### -AsInspector
Shows a record view (sections and nested tables) instead of a tree.

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

### -Culture
The culture for numbers and dates, for example en-US or pl-PL (labels stay English). Defaults to CONSOLEMONSTER_CULTURE, then the current culture, as PowerShell's own formatting does.

```yaml
Type: CultureInfo
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
How deep nested objects are followed (default 3).

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

### -Exclude
Member names to leave out (wildcards).

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Include
Member names to show (wildcards); others are left out.

```yaml
Type: String[]
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
The objects to show (pipeline input).

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

### -MaxItems
The most items listed per collection before "+N more" (default 25).

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

### -PassThru
Also writes the input objects to the pipeline.

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
The root label (default: the object's type name).

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
