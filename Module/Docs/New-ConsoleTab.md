---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTab
## SYNOPSIS
Creates a retained tab page for New-ConsoleTabs.

Collects the page widgets once and preserves their input values when another tab is selected. Pass the page to New-ConsoleTabs to mount it in a full-screen application.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTab [[-Content] <Object>] -Title <string> [<CommonParameters>]
```

## DESCRIPTION
Creates a retained tab page for New-ConsoleTabs.

Collects the page widgets once and preserves their input values when another tab is selected. Pass the page to New-ConsoleTabs to mount it in a full-screen application.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTab -Title Services -Content { New-ConsoleTextInput -Key Filter }
```

Create a page.

## PARAMETERS

### -Content
Widgets or a script block that emits the page's retained inputs.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Title
Unique tab label.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Widgets.LayoutRegion`

## RELATED LINKS

- None
