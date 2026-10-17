---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Assert-ConsoleOutput
## SYNOPSIS
Compares console output with a golden file (for Pester and other tests).

Renders a widget at a fixed width, glyph tier and culture (invariant by default), so the result does not depend on the machine, and compares the text with a golden file. The first run writes the file and fails so it can be reviewed; a difference fails with the first differing line, both versions and the column where they part. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite golden files after an intended change. Strings are compared as they are. In Pester the thrown error fails the test; see the README for a Should operator built on it.

## SYNTAX
### __AllParameterSets
```powershell
Assert-ConsoleOutput [-InputObject] <Object> [-GoldenPath] <string> [-Width <int>] [-Height <int>] [-Glyphs <GlyphTier>] [-Culture <cultureinfo>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Compares console output with a golden file (for Pester and other tests).

Renders a widget at a fixed width, glyph tier and culture (invariant by default), so the result does not depend on the machine, and compares the text with a golden file. The first run writes the file and fails so it can be reviewed; a difference fails with the first differing line, both versions and the column where they part. Set CONSOLEMONSTER_UPDATE_GOLDEN=1 to rewrite golden files after an intended change. Strings are compared as they are. In Pester the thrown error fails the test; see the README for a Should operator built on it.

## EXAMPLES

### EXAMPLE 1
```powershell
Assert-ConsoleOutput -InputObject 'Value' -GoldenPath 'C:\Path'
```


## PARAMETERS

### -Culture
The culture for numbers and dates (default: invariant).

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

### -Glyphs
The glyph tier (default Full).

```yaml
Type: GlyphTier
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Ascii, Basic, Full

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GoldenPath
The golden file.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Height in cells (default: as tall as the widget measures).

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
A widget, or text to compare as it is.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Writes the rendered text to the pipeline when it matches.

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

### -Width
Width in cells (default 80).

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

- `System.String`

## RELATED LINKS

- None
