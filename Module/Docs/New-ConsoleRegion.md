---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleRegion
## SYNOPSIS
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Set-ConsoleRegion -Name with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRegion [-Name] <string> [[-Content] <scriptblock>] [-Widget <IWidget>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Creates a named region whose content can be replaced while a dashboard runs.

Put the region anywhere in a layout (inside New-ConsolePanel keeps the frame), then call Set-ConsoleRegion -Name with a new widget from the dashboard script. The swap is queued like every update, lands between frames and redraws only what changed.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePanel -Title 'Result' { New-ConsoleRegion -Name main { New-ConsoleSpinner -Label 'Querying' } }
```

A panel whose content switches from a spinner to a table

## PARAMETERS

### -Content
A script block whose widgets are the first content.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Fill
Takes the remaining height.

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

### -Height
A fixed height in rows.

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

### -Name
The region name used by Set-ConsoleRegion.

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

### -Weight
The share of the remaining space in a row or column layout.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Widget
A widget as the first content.

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `ConsoleMonster.Layout.IWidget`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
