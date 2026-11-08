---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Set-ConsoleTheme
## SYNOPSIS
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds and HighContrast uses saturated primaries. ColorBlindSafe keeps pass, warning and fail distinct with red-green colour blindness (blue, yellow and underlined rose) and picks its light variant when it detects a light background; where the background cannot be detected (for example in Windows Terminal) it uses the dark variant, so choose ColorBlindSafeLight there. ColorBlindSafeLight and ColorBlindSafeDark choose one explicitly. To keep a theme in every session, add the Set-ConsoleTheme line to your profile (Enable-ConsoleFormatting -ProfileSnippet includes it) or set CONSOLEMONSTER_THEME, for example to colorblind. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

## SYNTAX
### Name (Default)
```powershell
Set-ConsoleTheme [-Theme] <string> [<CommonParameters>]
```

### Path
```powershell
Set-ConsoleTheme -Path <string> [<CommonParameters>]
```

## DESCRIPTION
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds and HighContrast uses saturated primaries. ColorBlindSafe keeps pass, warning and fail distinct with red-green colour blindness (blue, yellow and underlined rose) and picks its light variant when it detects a light background; where the background cannot be detected (for example in Windows Terminal) it uses the dark variant, so choose ColorBlindSafeLight there. ColorBlindSafeLight and ColorBlindSafeDark choose one explicitly. To keep a theme in every session, add the Set-ConsoleTheme line to your profile (Enable-ConsoleFormatting -ProfileSnippet includes it) or set CONSOLEMONSTER_THEME, for example to colorblind. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleTheme -Theme Light
```

Use the light theme for this session

### EXAMPLE 2
```powershell
Set-ConsoleTheme -Theme ColorBlindSafe
```

Use status colours that stay distinct with red-green colour blindness

### EXAMPLE 3
```powershell
# Requires an existing team-theme.psd1 file. Changes the active session theme.
            Set-ConsoleTheme -Path ./team-theme.psd1
```

Load an existing theme exported by Export-ConsoleTheme

## PARAMETERS

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

### -Theme
The built-in theme: Dark, Light, HighContrast, ColorBlindSafe (light or dark variant from the detected background), ColorBlindSafeLight or ColorBlindSafeDark.

```yaml
Type: String
Parameter Sets: Name
Aliases: None
Possible values: Dark, Light, HighContrast, ColorBlindSafe, ColorBlindSafeLight, ColorBlindSafeDark

Required: True
Position: 0
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
