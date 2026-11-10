---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Set-ConsoleSetting
## SYNOPSIS
Changes a ConsoleMonster setting for this session, and with -Save for every session.

## SYNTAX
### __AllParameterSets
```powershell
Set-ConsoleSetting [-Name] <string> [-Value] <string> [-Save] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Sets Theme, Color, Glyphs, Border, Density, StatusMarkers, Motion, Mouse, Accessible, Hyperlinks, Language or Culture. Border, Density and StatusMarkers override the theme's own style: the frame around panels, tables and inputs, Comfortable or Compact spacing, and Symbols or Words ([OK] [FAIL] [WARN]) for statuses. Compact reduces default padding and gaps; explicit spacing stays as authored. The value applies at once to everything ConsoleMonster draws in this session, above any CONSOLEMONSTER_* environment variable. -Save also writes it to the settings file, so new sessions and other tools built on ConsoleMonster use it too; nothing needs to be added to your profile. Theme takes a built-in theme, a catalogue scheme or paired family, or the path of a .json or .psd1 theme file (see Export-ConsoleTheme). Store an accessible catalogue choice with its suffix, for example Dracula · accessible. Show-ConsoleSetting chooses settings with a live preview.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleSetting Theme ColorBlindSafe -Save
```

Use the colour-blind-safe theme from now on

### EXAMPLE 2
```powershell
Set-ConsoleSetting Motion Reduced; Set-ConsoleSetting Mouse Off
```

Keep animations still and leave the mouse to the terminal, for this session only

### EXAMPLE 3
```powershell
Set-ConsoleSetting Border Single -Save; Set-ConsoleSetting StatusMarkers Words -Save
```

Square frames and status words instead of symbols, from now on

### EXAMPLE 4
```powershell
# Requires an existing team-theme.json file, for example from Export-ConsoleTheme.
            Set-ConsoleSetting Theme ./team-theme.json -Save
```

Use a team theme file in every session

## PARAMETERS

### -Name
The setting: Theme, Color, Glyphs, Border, Density, StatusMarkers, Motion, Mouse, Accessible, Hyperlinks, Language or Culture.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Save
Also writes the value to the settings file, for every new session.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The value. Get-ConsoleSetting lists the values each setting takes; Theme also takes a theme file path; catalogue choices can end in · accessible. Language and Culture take a culture name such as pl or de-DE.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/set-consolesetting/](https://consolemonster.dev/powershell/commands/set-consolesetting/)
