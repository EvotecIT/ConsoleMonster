---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleSplitter
## SYNOPSIS
Creates two panes with a divider that the mouse and keyboard can move.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleSplitter [[-First] <Object>] [[-Second] <Object>] [-Vertical] [-Ratio <double>] [-Key <string>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
The panes sit side by side, or top and bottom with -Vertical. Dragging the divider resizes them. Ctrl+Shift+Left/Right (Ctrl+Shift+Up/Down when vertical) also resizes when the focused input and configured keys leave those chords free; text editing and existing bindings take precedence. With -Key, the app state holds the ratio under that key, so a binding that rebuilds the splitter keeps it and a callback can read or set it. A vertical splitter is as tall as its panes; -Fill makes it use the available height.

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

### EXAMPLE 2
```powershell
New-ConsoleSplitter -Key inventory -First { New-ConsoleText 'web01' } -Second {
                New-ConsoleText 'State: Online' -Role Pass
            } | Show-Console
```

Compose an inventory splitter

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
Default value: $false
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
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state; the value is the first pane's share, from 0.05 to 0.95.

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
Default value: 0.5
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
Default value: $false
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
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolesplitter/](https://consolemonster.dev/powershell/commands/new-consolesplitter/)
