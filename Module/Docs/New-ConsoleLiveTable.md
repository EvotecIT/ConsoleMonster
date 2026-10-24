---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLiveTable
## SYNOPSIS
Creates a table whose rows change while a dashboard runs.

Rows have stable keys and are added, updated, removed or cleared with Update-ConsoleLiveTable. Column widths only grow, so rows never jitter. -Column takes header texts or column hashtables (Alignment, Width, Flexible, Markup and the other column options of Show-ConsoleTable). -MaxRows keeps only the most recent rows; -SortBy keeps rows sorted by a column.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLiveTable [[-WidgetId] <string>] [-Column <Object[]>] [-Table <Table>] [-Title <Object>] [-Border <TableBorder>] [-MaxRows <int>] [-SortBy <Object>] [-Descending] [<CommonParameters>]
```

## DESCRIPTION
Creates a table whose rows change while a dashboard runs.

Rows have stable keys and are added, updated, removed or cleared with Update-ConsoleLiveTable. Column widths only grow, so rows never jitter. -Column takes header texts or column hashtables (Alignment, Width, Flexible, Markup and the other column options of Show-ConsoleTable). -MaxRows keeps only the most recent rows; -SortBy keeps rows sorted by a column.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLiveTable -WidgetId jobs -Column 'Job', 'State', 'Duration' -MaxRows 20
```

The 20 most recent jobs, newest state first

### EXAMPLE 2
```powershell
New-ConsoleLiveTable -WidgetId slowest -Column 'Check', 'Server', @{ Name = 'Took'; Alignment = 'Right' } -Border Minimal
New-ConsoleLiveTable -WidgetId failures -Column 'Server', 'Check', @{ Name = 'Actual'; Flexible = $true; Markup = $true } -MaxRows 5
```

The slowest checks with a right-aligned time, and failures whose last column takes the spare width and reads markup

## PARAMETERS

### -Border
The border style.

```yaml
Type: TableBorder
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Column
The columns, in order: header texts, or hashtables with Name (the header) and the column options of Show-ConsoleTable, such as Alignment (Right for numbers and times), Width, MinWidth, MaxWidth, Flexible (takes the spare width), Overflow, Presenter and Markup (string values are markup, [fail]9 days[/]).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Descending
Sorts from the largest value down.

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

### -MaxRows
Keeps only the most recently added rows (0 keeps all).

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

### -SortBy
The column (header or index) to keep rows sorted by.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Table
A table that defines the columns and options instead (for example from New-ConsoleTable); its rows are replaced.

```yaml
Type: Table
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title above the table (markup).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live name used by Update-ConsoleLiveTable (default table).

```yaml
Type: String
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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.LiveTable`

## RELATED LINKS

- None
