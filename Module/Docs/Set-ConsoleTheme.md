---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Set-ConsoleTheme
## SYNOPSIS
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

## SYNTAX
### Name (Default)
```powershell
Set-ConsoleTheme [-Name] <string> [<CommonParameters>]
```

### Path
```powershell
Set-ConsoleTheme -Path <string> [<CommonParameters>]
```

## DESCRIPTION
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleTheme -Name Light
```

Use the light theme

### EXAMPLE 2
```powershell
Set-ConsoleTheme -Path ./team-theme.psd1
```

Load a team theme exported as JSON or a PowerShell data file.

## PARAMETERS

### -Name
The built-in theme: Dark, Light, HighContrast or ColorBlindSafe.

```yaml
Type: String
Parameter Sets: Name
Aliases: None
Possible values: Dark, Light, HighContrast, ColorBlindSafe

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A literal .json or .psd1 theme file path. Relative paths use the current PowerShell location. All values are validated before applying the theme.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
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

- `None`

## RELATED LINKS

- None
