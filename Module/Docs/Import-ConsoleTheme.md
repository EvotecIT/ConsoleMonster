---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Import-ConsoleTheme
## SYNOPSIS
Imports a terminal colour scheme as a version 3 ConsoleMonster theme.

Reads a single Windows Terminal scheme object (or a settings.json with one schemes entry), an XML iTerm2 .itermcolors file, or a flat base16 YAML scheme. The generated JSON embeds the scheme so its roles are derived when loaded. Accessible first chooses status colours from the palette, then adjusts OKLCH lightness where needed. The command writes the file and returns Test-ConsoleTheme's report and verdict lines, including adjusted tokens; failing checks never prevent import. It does not change the active theme or settings.

## SYNTAX
### __AllParameterSets
```powershell
Import-ConsoleTheme [-Path] <string> [-Accessible] [-OutFile <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Imports a terminal colour scheme as a version 3 ConsoleMonster theme.

Reads a single Windows Terminal scheme object (or a settings.json with one schemes entry), an XML iTerm2 .itermcolors file, or a flat base16 YAML scheme. The generated JSON embeds the scheme so its roles are derived when loaded. Accessible first chooses status colours from the palette, then adjusts OKLCH lightness where needed. The command writes the file and returns Test-ConsoleTheme's report and verdict lines, including adjusted tokens; failing checks never prevent import. It does not change the active theme or settings.

## EXAMPLES

### EXAMPLE 1
```powershell
# Requires an existing Windows Terminal scheme JSON file.
            Import-ConsoleTheme -Path ./my-scheme.json -OutFile ./my-theme.json
```

Import a Windows Terminal scheme and inspect its verdict.

### EXAMPLE 2
```powershell
# Requires an existing sRGB iTerm2 .itermcolors file.
            Import-ConsoleTheme -Path ./my-scheme.itermcolors -Accessible -OutFile ./my-theme.json
            Set-ConsoleTheme -Path ./my-theme.json
```

Import an accessible variant, then use it.

## PARAMETERS

### -Accessible
Chooses the accessible variant and records tokens adjusted for contrast or colour-vision separation.

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

### -OutFile
The destination JSON theme file. Defaults to the source directory and base name with .theme.json. The source file cannot be overwritten.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A literal .json, .itermcolors, .yaml or .yml scheme file. Relative paths use the current PowerShell location.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Styling.ThemeCheckReport`

## RELATED LINKS

- None
