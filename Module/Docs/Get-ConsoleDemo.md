---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Get-ConsoleDemo
## SYNOPSIS
Lists the copyable ConsoleMonster gallery demos .

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. Use Export-ConsoleDemo to save a script.

## SYNTAX
### __AllParameterSets
```powershell
Get-ConsoleDemo [[-Slug] <string>] [<CommonParameters>]
```

## DESCRIPTION
Lists the copyable ConsoleMonster gallery demos .

Returns the embedded gallery metadata, including slug, title, pitch, category, tags, terminal size, runtimes, add-ons, customization ideas, mode and snapshot count. Listing accepts a slug or wildcard pattern. Use Export-ConsoleDemo to save a script.

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
