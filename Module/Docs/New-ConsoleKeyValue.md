---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleKeyValue
## SYNOPSIS
Creates an aligned key/value list from a hashtable, an object's properties or Key/Value records pairs.

Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of Key/Value records calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Update-Console.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleKeyValue [[-WidgetId] <string>] [[-InputObject] <Object>] [[-Content] <scriptblock>] [-Markup] [-NumberColumn <ColumnFormat>] [-Gap <int>] [-MaxKeyFraction <double>] [-Columns <int>] [-KeyRole <ThemeToken>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

## DESCRIPTION
Creates an aligned key/value list from a hashtable, an object's properties or Key/Value records pairs.

Pairs come from a hashtable (use [ordered] to keep order), from an object's properties, or from a script block of Key/Value records calls when a pair needs a status marker, markup or a presenter. Keys are muted and aligned; values wrap. Values are shown literally unless -Markup is used. To add pairs later, pipe the list to Add-ConsoleItem; in a live dashboard, update pairs with Update-Console.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleKeyValue -InputObject ([ordered] @{ Domain = 'evotec.xyz'; Records = 14; DNSSEC = 'not signed' }) | Show-Console
```

A summary block

### EXAMPLE 2
```powershell
New-ConsoleKeyValue -Content {
    [pscustomobject]@{ Key = 'Version'; Value = '0.0.4 [muted]→[/] [bold]2.0.0[/]'; Markup = $true }
    [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
    [pscustomobject]@{ Key = 'Tests'; Value = '1 skipped on PowerShell 7'; Status = 'Warn' }
} | Show-Console
```

Pairs with status marks

### EXAMPLE 3
```powershell
New-ConsoleKeyValue -InputObject ([ordered] @{ Module = 'ConsoleMonster'; Frameworks = 'netstandard2.0, net472, net8.0' }) -Content {
    [pscustomobject]@{ Key = 'Signing'; Value = 'skipped'; Status = 'Skip' }
} | Show-Console
```

A hashtable and pairs together

## PARAMETERS

### -Columns
Preferred pairs per row; reduces automatically in narrow terminals.

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

### -Content
A script block that emits Key/Value records pairs (or hashtables), added after -InputObject.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Gap
Cells between the key column and values.

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
A hashtable or an object whose properties become the pairs. A record containing both Key and Value is an item descriptor, even when Value is null; Key by itself is ordinary data. A script block here is taken as -Content.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -KeyRole
Theme role for keys.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

Required: False
Position: named
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

### -MaxKeyFraction
Preferred key fraction when natural widths do not fit.

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

### -NumberColumn
Shared NumberColumn layout settings from New-ConsoleColumnFormat.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Presentation
Shared value-formatting culture, clock and time zone.

```yaml
Type: PresentationOptions
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live name used by Update-Console (default keyvalue).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Widgets.KeyValueList`

## RELATED LINKS

- None
