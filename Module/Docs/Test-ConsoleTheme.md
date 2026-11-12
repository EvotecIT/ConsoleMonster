---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/test-consoletheme/
schema: 2.0.0
---
# Test-ConsoleTheme
## SYNOPSIS
Checks a theme's text contrast, status colours, markers and critical fill.

## SYNTAX
### Name (Default)
```powershell
Test-ConsoleTheme [-Theme <string>] [-Background <string>] [-Strict] [-Detailed] [<CommonParameters>]
```

### Path
```powershell
Test-ConsoleTheme [-Path] <string> [-Background <string>] [-Strict] [-Detailed] [<CommonParameters>]
```

## DESCRIPTION
Checks the active theme, a named preset, or a JSON/PSD1 file without changing settings. Text needs WCAG contrast of 4.5:1 against its own fill or the page background. Pass, warning and fail need CIELAB distance 25 in normal vision and full-severity Machado protanopia, deuteranopia and tritanopia simulations. Markers must differ without colour. Critical may share Fail's hue when it has a distinct fill. The Terminal preset is measured against the startup palette when the terminal answers OSC 4/10/11. Otherwise the actual colours of palette indexes, terminal-default text and terminal-dependent dimming cannot be measured; Unknown prevents a confirmed pass but is not a failure. The output report has Passed and Results properties and a verdict-first default view showing problems and unknowns. Use -Detailed for every check. Dark and Light are not colour-blind-safe presets.

## EXAMPLES

### EXAMPLE 1
```powershell
Test-ConsoleTheme
```

Check the active theme.

### EXAMPLE 2
```powershell
Test-ConsoleTheme -Path ./team-theme.json -Strict
```

Check a team theme in CI; any failure stops the script.

### EXAMPLE 3
```powershell
(Test-ConsoleTheme -Theme Light -Background '#ffffff').Results | Where-Object Outcome -EQ Fail
```

Inspect individual measurements as objects.

### EXAMPLE 4
```powershell
Test-ConsoleTheme -Theme Dark -Detailed
```

Show every measurement.

## PARAMETERS

### -Background
The page colour, as a name or #rrggbb. Defaults to #0c0c0c for a dark preset and #ffffff for a light preset. A token's own background takes precedence. Palette-index backgrounds remain unknown.

```yaml
Type: String
Parameter Sets: Name, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detailed
Shows every measurement instead of only problems and unknowns. The Results property always contains every check.

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
A literal .json or restricted .psd1 theme file. Relative paths use the current PowerShell location.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Strict
Makes any Fail a terminating ConsoleThemeCheckFailed error with the report as its target. Unknown alone does not throw.

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
A built-in preset, catalogue scheme or family. Catalogue names accept · accessible. Omit both Theme and Path to check the active theme. Paired families and ColorBlindSafe select the detected background; explicit variant names choose one.

```yaml
Type: String
Parameter Sets: Name
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

- `ConsoleMonster.Styling.ThemeCheckReport`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/test-consoletheme/](https://consolemonster.dev/powershell/commands/test-consoletheme/)
