---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleList
## SYNOPSIS
Displays the list representation through the shared Show-Console implementation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## SYNTAX
### Objects (Default)
```powershell
Show-ConsoleList [[-InputObject] <psobject>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowEmpty] [-Depth <int>] [<CommonParameters>]
```

### Content
```powershell
Show-ConsoleList -Content <Object> [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowEmpty] [-Depth <int>] [<CommonParameters>]
```

## DESCRIPTION
Displays the list representation through the shared Show-Console implementation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Show-ConsoleList
```

Display pipeline input

## PARAMETERS

### -AsString
Returns plain text instead of displaying it.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the constructed widget.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
A widget, text or script block producing display content.

```yaml
Type: Object
Parameter Sets: Content
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Culture
The culture used for values.

```yaml
Type: CultureInfo
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Maximum nesting depth; default three.

```yaml
Type: Int32
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
Groups rows or records by a property or script block.

```yaml
Type: Object
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The object, widget or text to display.

```yaml
Type: PSObject
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Returns the original input after display.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
Property names, patterns or calculated column records.

```yaml
Type: Object[]
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowEmpty
Keeps empty fields in record views.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The view title, including markup.

```yaml
Type: Object
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Layout width; zero resolves the terminal with a 100-cell fallback.

```yaml
Type: Int32
Parameter Sets: Objects, Content
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
