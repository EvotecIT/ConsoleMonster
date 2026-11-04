---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleSplitter
## SYNOPSIS
Creates two panes with a divider that the mouse and keyboard can move.

The panes sit side by side, or top and bottom with -Vertical. Dragging the divider resizes them, and so do Ctrl+Shift+Left/Right (Ctrl+Shift+Up/Down when vertical) while an input in either pane has focus. Inside Show-ConsoleApp screens, a splitter with -Key keeps its ratio for each screen, also when a binding rebuilds it.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSplitter [[-First] <Object>] [[-Second] <Object>] [-Vertical] [-Ratio <double>] [-Key <string>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Creates two panes with a divider that the mouse and keyboard can move.

The panes sit side by side, or top and bottom with -Vertical. Dragging the divider resizes them, and so do Ctrl+Shift+Left/Right (Ctrl+Shift+Up/Down when vertical) while an input in either pane has focus. Inside Show-ConsoleApp screens, a splitter with -Key keeps its ratio for each screen, also when a binding rebuilds it.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp -Content {
    New-ConsoleSplitter -Key Panes -Ratio 0.35 -Fill -First {
        New-ConsoleSelect -Key Server -Choices 'web-01', 'web-02', 'db-01'
    } -Second {
        New-ConsoleBinding -Content { param($State) "Details for $($State.Server)" }
    }
}
```

Show a list beside its details and let the reader widen either side.

## PARAMETERS

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

### -First
The left pane, or the top pane with -Vertical: widgets, text or a script block that emits them.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Fixed height in cells.

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

### -Key
Name the ratio is remembered under on each app screen.

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

### -Ratio
The first pane's share of the space, from 0.05 to 0.95.

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

### -Second
The right pane, or the bottom pane with -Vertical.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Vertical
Stacks the panes top and bottom with a horizontal divider.

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

- `None`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- None
