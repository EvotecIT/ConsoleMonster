---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Set-ConsoleTheme
## SYNOPSIS
Selects the theme used by every ConsoleMonster widget.

## SYNTAX
### Name (Default)
```powershell
Set-ConsoleTheme [-Theme] <string> [-Save] [-Accessible] [<CommonParameters>]
```

### Path
```powershell
Set-ConsoleTheme -Path <string> [-Save] [-Accessible] [<CommonParameters>]
```

## DESCRIPTION
Select a built-in preset or catalogue scheme, follow your terminal with Terminal, or load a validated JSON/PSD1 theme. Catalogue schemes use upstream colours faithfully by default; -Accessible chooses colours that pass ThemeCheck. The saved accessible name is Dracula · accessible, for example. Dark suits dark terminals, Light suits light backgrounds and HighContrast uses saturated primaries. ColorBlindSafe keeps pass, warning and fail distinct with red-green colour blindness (blue, yellow and underlined rose) and picks its light variant when it detects a light background; where the background cannot be detected (for example in Windows Terminal) it uses the dark variant, so choose ColorBlindSafeLight there. ColorBlindSafeLight and ColorBlindSafeDark choose one explicitly. To keep a theme in every session, add -Save: it is written to the settings file, which every session and every tool built on ConsoleMonster reads (Set-ConsoleSetting Theme does the same, and Show-ConsoleSetting previews themes before choosing). CONSOLEMONSTER_THEME, for example colorblind, still overrides the file. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

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
Set-ConsoleTheme -Theme ColorBlindSafe -Save
```

Keep the colour-blind-safe theme in every new session

### EXAMPLE 4
```powershell
Set-ConsoleTheme Dracula -Accessible -Save
```

Keep the accessible Dracula scheme in every session

### EXAMPLE 5
```powershell
# Requires an existing team-theme.psd1 file. Changes the active session theme.
            Set-ConsoleTheme -Path ./team-theme.psd1
```

Load an existing theme exported by Export-ConsoleTheme

## PARAMETERS

### -Accessible
Uses a catalogue scheme's accessible variant, or re-derives a version 3 BaseScheme file while preserving its explicit token overrides. Built-in presets do not have scheme variants.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
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

### -Save
Also writes the theme to the settings file, so every new session uses it.

```yaml
Type: SwitchParameter
Parameter Sets: Name, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Theme
A built-in preset, Terminal, or a catalogue scheme or family. Theme.Schemes lists the ten palettes. Solarized, Gruvbox, Catppuccin and Tokyo Night select light or dark for the detected background (dark when unknown). Explicit names such as Solarized Light, Catppuccin Latte and Tokyo Night Dark choose one variant. Add · accessible to a catalogue name, or use -Accessible.

```yaml
Type: String
Parameter Sets: Name
Aliases: None
Possible values:

Required: True
Position: 0
Default value: Dark
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

- [https://consolemonster.dev/powershell/commands/set-consoletheme/](https://consolemonster.dev/powershell/commands/set-consoletheme/)
