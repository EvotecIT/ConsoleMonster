---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleBarItem
## SYNOPSIS
Creates one bar of a bar chart, or one segment of a stacked bar, with its own tone.

Use it inside the script block of New-ConsoleBarChart when a bar or a segment needs its own colour: -Role takes a theme role (Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted, Series1 to Series6). Without -Role the chart's colour is used, and a stacked bar picks the next series colour. The label is shown literally; rich text from New-ConsoleText keeps its colours. To add bars to a chart made earlier, pipe the chart to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleBarItem [-Label] <Object> [-Value] <double> [-Role <ThemeToken>] [-Key <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates one bar of a bar chart, or one segment of a stacked bar, with its own tone.

Use it inside the script block of New-ConsoleBarChart when a bar or a segment needs its own colour: -Role takes a theme role (Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted, Series1 to Series6). Without -Role the chart's colour is used, and a stacked bar picks the next series colour. The label is shown literally; rich text from New-ConsoleText keeps its colours. To add bars to a chart made earlier, pipe the chart to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleBarChart -Stacked -Content {
    New-ConsoleBarItem 'Passed' 412 -Role Pass
    New-ConsoleBarItem 'Warnings' 51 -Role Warn
    New-ConsoleBarItem 'Failed' 23 -Role Fail
} | Show-Console
```

Results as a stacked bar

### EXAMPLE 2
```powershell
New-ConsoleBarChart -Unit ' ms' -Content {
    New-ConsoleBarItem 'Cloudflare' 12
    New-ConsoleBarItem 'Quad9' 64 -Role Warn
} | Show-Console
```

One bar picked out

## PARAMETERS

### -Key
Stable item identity for updates; defaults to the visible label.

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

### -Label
The label (shown literally; rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Role
The colour as a theme role: Pass, Fail, Warn, Skip, Info, Accent, Accent2, Muted or Series1 to Series6.

```yaml
Type: ThemeToken
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The value.

```yaml
Type: Double
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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.ChartItem`

## RELATED LINKS

- None
