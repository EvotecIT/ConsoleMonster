---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Get-ConsoleSetting
## SYNOPSIS
Shows ConsoleMonster's settings: the value of each and where it came from.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleSetting [[-Name] <string[]>] [<CommonParameters>]
```

## DESCRIPTION
Lists the theme, colour, glyphs, frames (Border), spacing (Density), status markers, motion, mouse, accessibility, hyperlinks, language and culture settings with their configured value or built-in fallback and source: Session (Set-ConsoleSetting in this session), Environment (a CONSOLEMONSTER_* variable or NO_COLOR), File (the settings file) or Default. This describes preference overrides, not the final themed appearance: without a Density override, Comfortable with source Default can still render with Compact spacing from the theme. Session values beat environment variables, which beat the file, so CI and one-off runs keep working without touching it. The file is settings.json in %APPDATA%\ConsoleMonster on Windows and ~/.config/ConsoleMonster elsewhere; CONSOLEMONSTER_SETTINGS names another file, or none to ignore it. Invalid Density environment values and unusable file entries are ignored and reported as warnings.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-ConsoleSetting
```

Every setting and its source

### EXAMPLE 2
```powershell
Get-ConsoleSetting Theme
```

One setting

## PARAMETERS

### -Name
The settings to show; all of them when omitted.

```yaml
Type: String[]
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.ConsoleSetting`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/get-consolesetting/](https://consolemonster.dev/powershell/commands/get-consolesetting/)
