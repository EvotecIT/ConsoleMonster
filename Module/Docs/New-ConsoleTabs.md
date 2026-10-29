---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleTabs
## SYNOPSIS
Creates retained tabs with keyboard and pointer navigation.

Combines pages from New-ConsoleTab in display order. Only the selected page receives input; switching pages retains each page's values and focus for the next visit.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTabs [[-Tab] <LayoutRegion[]>] [-SelectedIndex <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates retained tabs with keyboard and pointer navigation.

Combines pages from New-ConsoleTab in display order. Only the selected page receives input; switching pages retains each page's values and focus for the next visit.

## EXAMPLES

### EXAMPLE 1
```powershell
$pages | New-ConsoleTabs | Show-ConsoleApp
```

Compose two pages.

## PARAMETERS

### -SelectedIndex
Initial zero-based active page.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Tab
Pages returned by New-ConsoleTab, in display order.

```yaml
Type: LayoutRegion[]
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

- `ConsoleMonster.Widgets.Tabs`

## RELATED LINKS

- None
