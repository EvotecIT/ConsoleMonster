---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Get-ConsoleDemo
## SYNOPSIS
Lists gallery demos and returns their complete PowerShell source.

Returns each demo's title, category, tags, terminal size and Source. Slug accepts an exact name or wildcard pattern. Source is the complete script, ready to inspect or save with Set-Content.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleDemo [[-Slug] <string>] [<CommonParameters>]
```

## DESCRIPTION
Lists gallery demos and returns their complete PowerShell source.

Returns each demo's title, category, tags, terminal size and Source. Slug accepts an exact name or wildcard pattern. Source is the complete script, ready to inspect or save with Set-Content.

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
(Get-ConsoleDemo assessment).Source
```

Read a complete demo without running it.

## PARAMETERS

### -Slug
Slug or wildcard pattern to list. Export requires one exact slug.

```yaml
Type: String
Parameter Sets: __AllParameterSets
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.Management.Automation.PSObject`

## RELATED LINKS

- None
