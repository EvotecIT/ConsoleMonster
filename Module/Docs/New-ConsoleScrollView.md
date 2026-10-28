---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleScrollView
## SYNOPSIS
Creates a clipped vertical viewport for a small widget tree inside an app.

Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleScrollView [[-Content] <Object>] [-ScrollOffset <int>] [-MaxContentHeight <int>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Creates a clipped vertical viewport for a small widget tree inside an app.

Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleScrollView -Content { $details } -Height 12
```

A scrollable detail pane.

## PARAMETERS

### -Content
Widgets, text, objects, or a script block that emits them.

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

### -Fill
Use the available height.

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
Fixed viewport height in cells.

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

### -MaxContentHeight
Maximum composed rows for ordinary content; use virtualized lists or grids for larger data.

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

### -ScrollOffset
First visible content row.

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

### -Weight
Share of the parent layout.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
