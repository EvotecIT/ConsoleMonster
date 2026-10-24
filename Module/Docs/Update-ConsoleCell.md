---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleCell
## SYNOPSIS
Sets the state of cells of a cell map (New-ConsoleCellMap) in the running dashboard.

-Index names the cells by position, from 0 (in a grid with -Columns, row * columns + column); -Status is their new state: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success and Error work too). An index past the end grows the map. -Add appends cells in the -Status state instead, and -Count sets the number of cells (new ones are pending). Safe to call from any runspace or thread.

## SYNTAX
### Set (Default)
```powershell
Update-ConsoleCell [[-WidgetId] <string>] [-Index] <int[]> [-Status] <StatusKind> [-Session <LiveSession>] [<CommonParameters>]
```

### Add
```powershell
Update-ConsoleCell [[-WidgetId] <string>] -Add [-Status <StatusKind>] [-Times <int>] [-Session <LiveSession>] [<CommonParameters>]
```

### Count
```powershell
Update-ConsoleCell [[-WidgetId] <string>] -Count <int> [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Sets the state of cells of a cell map (New-ConsoleCellMap) in the running dashboard.

-Index names the cells by position, from 0 (in a grid with -Columns, row * columns + column); -Status is their new state: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success and Error work too). An index past the end grows the map. -Add appends cells in the -Status state instead, and -Count sets the number of cells (new ones are pending). Safe to call from any runspace or thread.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleCell -WidgetId checks -Index 7 -Status Running
Update-ConsoleCell -WidgetId checks -Index 7 -Status Pass
```

A check starts and passes on the second server of a grid with five checks per row

### EXAMPLE 2
```powershell
Update-ConsoleCell -WidgetId files -Count 40
Update-ConsoleCell -WidgetId files -Index 0, 1, 2 -Status Pass
```

Grow a map as items are found, then mark them

## PARAMETERS

### -Add
Appends cells in the -Status state.

```yaml
Type: SwitchParameter
Parameter Sets: Add
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Count
Sets the number of cells: new ones are pending, extra ones are removed (at least 1).

```yaml
Type: Int32
Parameter Sets: Count
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Index
The cells to set, by position from 0.

```yaml
Type: Int32[]
Parameter Sets: Set
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: Set, Add, Count
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The new state (for -Add, the state of the new cells; default Pending).

```yaml
Type: StatusKind
Parameter Sets: Set, Add
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Times
How many cells -Add appends (default 1).

```yaml
Type: Int32
Parameter Sets: Add
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The cell map name (default cells).

```yaml
Type: String
Parameter Sets: Set, Add, Count
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

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
