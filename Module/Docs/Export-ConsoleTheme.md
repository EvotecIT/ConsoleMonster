---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Export-ConsoleTheme
## SYNOPSIS
Exports the current ConsoleMonster theme as a starting file for a user or team.

Writes every semantic token, foreground/background colour, explicit fallback tier and decoration as JSON or restricted PowerShell data. The current theme is unchanged. The file extension selects the format; the parent directory must already exist. JSON is UTF-8 without a BOM; PSD1 uses a UTF-8 BOM for Windows PowerShell 5.1.

## SYNTAX
### __AllParameterSets
```powershell
Export-ConsoleTheme [-Path] <string> [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Exports the current ConsoleMonster theme as a starting file for a user or team.

Writes every semantic token, foreground/background colour, explicit fallback tier and decoration as JSON or restricted PowerShell data. The current theme is unchanged. The file extension selects the format; the parent directory must already exist. JSON is UTF-8 without a BOM; PSD1 uses a UTF-8 BOM for Windows PowerShell 5.1.

## EXAMPLES

### EXAMPLE 1
```powershell
Export-ConsoleTheme -Path ./team-theme.json
            Set-ConsoleTheme -Path ./team-theme.json
```

Export the current theme, edit its token definitions and apply it.

### EXAMPLE 2
```powershell
Set-ConsoleTheme -Theme ColorBlindSafe
            Export-ConsoleTheme -Path ./accessible.psd1 -PassThru
```

Create a PowerShell data file from the accessible preset and return its file information.

## PARAMETERS

### -PassThru
Returns the written file as a FileInfo. Without this switch the command produces no pipeline output.

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

### -Path
The literal .json or .psd1 destination. An existing file is replaced after validation and ShouldProcess approval.

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

- `None`

## OUTPUTS

- `System.IO.FileInfo`

## RELATED LINKS

- None
