---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Get-ConsoleDemo
## SYNOPSIS
Lists the copyable ConsoleMonster gallery demos or exports one PowerShell script.

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. With -Path, an exact slug exports the canonical self-contained script as UTF-8; ConsoleMonster must be installed to run it. The parent directory must exist. Existing files are preserved unless -Force is supplied.

## SYNTAX
### List (Default)
```powershell
Get-ConsoleDemo [[-Slug] <string>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Export
```powershell
Get-ConsoleDemo [-Slug] <string> -Path <string> [-Force] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Lists the copyable ConsoleMonster gallery demos or exports one PowerShell script.

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. With -Path, an exact slug exports the canonical self-contained script as UTF-8; ConsoleMonster must be installed to run it. The parent directory must exist. Existing files are preserved unless -Force is supplied.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-ConsoleDemo | Select-Object Slug, Title, Pitch
```

Browse the gallery.

### EXAMPLE 2
```powershell
Get-ConsoleDemo 'welcome-*'
```

List the welcome demos.

### EXAMPLE 3
```powershell
Get-ConsoleDemo assessment -Path ./assessment.ps1
            ./assessment.ps1
```

Export a demo to customize and run.

## PARAMETERS

### -Force
Allows replacing the destination file when exporting.

```yaml
Type: SwitchParameter
Parameter Sets: Export
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Literal filesystem destination for the script. Returns the exported FileInfo.

```yaml
Type: String
Parameter Sets: Export
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Slug
Slug or wildcard pattern to list. Export requires one exact slug.

```yaml
Type: String
Parameter Sets: List, Export
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: True
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Management.Automation.PSObject`
- `System.IO.FileInfo`

## RELATED LINKS

- None
