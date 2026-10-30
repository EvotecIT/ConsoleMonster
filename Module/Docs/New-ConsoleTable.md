---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleTable
## SYNOPSIS
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Property descriptors accept presenter names or configured C# ICellPresenter instances. Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## SYNTAX
### Objects (Default)
```powershell
New-ConsoleTable [[-InputObject] <psobject[]>] [-Property <Object[]>] [-GroupBy <Object>] [-ShowTotals] [-CardsBelow <int>] [-NoSort] [-Wrap] [-ShowGroupColumn] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-MaxRows <int>] [<CommonParameters>]
```

### Live
```powershell
New-ConsoleTable [[-WidgetId] <string>] [[-InputObject] <psobject[]>] -Live [-Property <Object[]>] [-GroupBy <Object>] [-ShowTotals] [-CardsBelow <int>] [-NoSort] [-Wrap] [-ShowGroupColumn] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-KeyProperty <string>] [-Table <Table>] [-MaxRows <int>] [-SortBy <Object>] [-Descending] [<CommonParameters>]
```

### Bind
```powershell
New-ConsoleTable -Bind <scriptblock> [-Property <Object[]>] [-GroupBy <Object>] [-ShowTotals] [-CardsBelow <int>] [-NoSort] [-Wrap] [-ShowGroupColumn] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-MaxRows <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Property descriptors accept presenter names or configured C# ICellPresenter instances. Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## EXAMPLES

### EXAMPLE 1
```powershell
@(
                [pscustomobject]@{ Name = 'web01'; Status = 'Pass'; Region = 'East' }
                [pscustomobject]@{ Name = 'web02'; Status = 'Warn'; Region = 'West' }
            ) | New-ConsoleTable -Property Name, Status, Region -StatusProperty Status -Title 'Servers' | Show-Console
```

Build a table with a status column

### EXAMPLE 2
```powershell
$items = @(
    [pscustomobject]@{ Name = 'web01'; Status = 'Pass' }
    [pscustomobject]@{ Name = 'web02'; Status = 'Warn' }
)
Show-ConsoleApp -Content {
    New-ConsoleTextInput -Key Query
    New-ConsoleTable -Bind { $items | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status -StatusProperty Status
}
```

Filter objects from current application state

## PARAMETERS

### -Bind
Application-dependent object producer. Receives param($State) and injected $State as a native hashtable on the owning input thread. Emit objects, not widgets; columns and formatting use the same builder as ordinary tables.

```yaml
Type: ScriptBlock
Parameter Sets: Bind
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Border
Border: Rounded, Square, Double, Minimal or None.

```yaml
Type: TableBorder
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None, Rule

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
A muted caption below the table.

```yaml
Type: Object
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CardsBelow
Uses cards below this width; zero keeps a grid.

```yaml
Type: Int32
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Descending
For Live, sorts larger values first.

```yaml
Type: SwitchParameter
Parameter Sets: Live
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Expand
Fill the available width. The text columns share the spare width in proportion to their widths, while number, date and status columns keep their natural width; a column hashtable's Weight (or Flexible) decides the share instead, and Width or MaxWidth holds.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
Groups rows by a property or script block.

```yaml
Type: Object
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The objects to show, one row each.

```yaml
Type: PSObject[]
Parameter Sets: Objects, Live
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -KeyProperty
For Live, the input property used as the stable row key; omitted uses the first column.

```yaml
Type: String
Parameter Sets: Live
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Live
Creates a keyed live table using the same column and row configuration.

```yaml
Type: SwitchParameter
Parameter Sets: Live
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRows
For Live, keeps this many recent rows; zero keeps all.

```yaml
Type: Int32
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoHeaderSeparator
Underline the header instead of drawing a separator row under it.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoSort
Keeps groups in input order.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
Properties to show, in order.

```yaml
Type: Object[]
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowGroupColumn
Keeps the grouping property in the table columns.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowTotals
Adds totals for numeric columns.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SortBy
For Live, the column index or header used for sorting.

```yaml
Type: Object
Parameter Sets: Live
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusProperty
A property whose values are statuses (pass/fail/warn/skip/running).

```yaml
Type: String
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Table
For Live, a table model supplying columns and presentation options.

```yaml
Type: Table
Parameter Sets: Live
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title above the table.

```yaml
Type: Object
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The live widget identifier; default table.

```yaml
Type: String
Parameter Sets: Live
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wrap
Wraps long cell values without limiting their line count.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Live, Bind
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

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Table`
- `ConsoleMonster.Interactive.AppBinding`

## RELATED LINKS

- None
