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
New-ConsoleTable [[-InputObject] <psobject[]>] [-Property <Object[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-Columns <Object[]>] [-MaxRows <int>] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Live
```powershell
New-ConsoleTable [[-WidgetId] <string>] [[-InputObject] <psobject[]>] -Live [-Property <Object[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-Columns <Object[]>] [-KeyProperty <string>] [-Table <Table>] [-MaxRows <int>] [-SortColumn <Object>] [-SortDescending] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Bind
```powershell
New-ConsoleTable -Bind <scriptblock> [-Property <Object[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [-Columns <Object[]>] [-MaxRows <int>] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

## DESCRIPTION
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Property descriptors accept presenter names or configured C# ICellPresenter instances. Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | New-ConsoleTable -Title 'Services' | Show-Console
```

Services as a table with a status column

### EXAMPLE 2
```powershell
Show-ConsoleApp -Content { New-ConsoleTextInput -Key Query; New-ConsoleTable -Bind { $items | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status -StatusProperty Status }
```

Filter objects from current application state.

## PARAMETERS

### -AlwaysCards
Uses record cards at every width.

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

### -Columns
Explicit column descriptors: header strings or the same column records accepted by Property.

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

### -GroupHeadersAsRows
Renders group headings as full-width rows.

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

### -KeepAllColumns
Renders consecutive grids when all columns cannot fit.

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

### -NoHeader
Suppresses the default-on header row.

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

### -NoHiddenColumns
Suppresses the default-on hidden-column footer.

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

### -NoStableWidths
Allows column widths to shrink between renders.

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

### -Presentation
Typed culture, clock and time-zone configuration for values.

```yaml
Type: PresentationOptions
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

### -RecordViewRows
Uses labelled records at or below this row count; zero disables.

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

### -RowTitles
Optional per-row titles for record and card views.

```yaml
Type: String[]
Parameter Sets: Objects, Live, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowKeyField
Repeats the full key in record and card layouts.

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

### -ShowSubtotals
Shows group subtotals.

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

### -SortColumn
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

### -SortDescending
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

### -TitleIndent
Optional title indentation in cells.

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

### -TotalLabel
Text for the totals row.

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

### -TotalRowCount
Total count when the input contains a subset.

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

### -WrapHeaders
Wraps column headings.

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

### -WrapLines
Maximum wrapped lines per value; zero has no limit.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Table`
- `ConsoleMonster.Interactive.AppBinding`

## RELATED LINKS

- None
