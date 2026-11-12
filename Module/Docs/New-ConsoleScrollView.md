---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolescrollview/
schema: 2.0.0
---
# New-ConsoleScrollView
## SYNOPSIS
Creates a clipped vertical viewport for a small widget tree inside an app.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleScrollView [[-Content] <Object>] [-ScrollOffset <int>] [-MaxContentHeight <int>] [-Weight <double>] [-Height <int>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Mouse wheel scrolls the viewport. Tab navigation reveals focused inputs. Lists, grids and logs keep their own row windows for large data.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleScrollView -Content {
    1..20 | ForEach-Object { New-ConsoleText "Detail line $_" }
} -Height 12
```

Build a scrollable detail pane

### EXAMPLE 2
```powershell
New-ConsoleScrollView -Height 4 -Content {
                foreach ($server in 'web01', 'web02', 'db01', 'db02', 'dc01') { New-ConsoleText $server }
            } | Show-Console
```

Compose an inventory scroll view

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
Default value: $false
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
Default value: 0
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
Default value: 16384
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
Default value: 0
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

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolescrollview/](https://consolemonster.dev/powershell/commands/new-consolescrollview/)
