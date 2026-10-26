---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Export-ConsoleDemo
## SYNOPSIS
Exports one embedded demo as a copyable PowerShell script.

Use Get-ConsoleDemo to list slugs. Saves the canonical script as UTF-8 with a BOM for PowerShell 5.1. The destination folder must exist. Existing files are preserved unless Force is explicit.

## SYNTAX
### __AllParameterSets
```powershell
Export-ConsoleDemo [-Slug] <string> -Path <string> [-Force] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Exports one embedded demo as a copyable PowerShell script.

Use Get-ConsoleDemo to list slugs. Saves the canonical script as UTF-8 with a BOM for PowerShell 5.1. The destination folder must exist. Existing files are preserved unless Force is explicit.

## EXAMPLES

### EXAMPLE 1
```powershell
Export-ConsoleDemo assessment -Path ./assessment.ps1
```

Save a demo to customize.

## PARAMETERS

### -Force
Allows replacing an existing destination file.

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

### -PassThru
Returns the saved file.

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
Literal filesystem destination for the script.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Slug
The exact gallery slug.

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
