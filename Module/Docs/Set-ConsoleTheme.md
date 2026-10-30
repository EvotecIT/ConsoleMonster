---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Set-ConsoleTheme
## SYNOPSIS
Selects the theme used by every ConsoleMonster widget.

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

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

Select a built-in preset or load a validated JSON/PSD1 theme. Dark suits dark terminals, Light suits light backgrounds, HighContrast uses saturated primaries and ColorBlindSafe uses blue/orange status contrast. Files use the built-in token names. PSD1 files are imported as restricted data and never executed. Invalid files leave the current theme unchanged.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleTheme -Theme Light
```

Use the light theme for this session

### EXAMPLE 2
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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
