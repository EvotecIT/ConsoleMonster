---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleTable
## SYNOPSIS
Displays the table representation through the shared Show-Console implementation.

Three ways to use ConsoleMonster output: Automatic: Enable-ConsoleFormatting makes ordinary output readable for known and registered types. Explicit: pipe any objects to Show-ConsoleTable to choose columns, a title and grouping, or to Show-ConsoleObject to explore their members. These commands do not change session formatting; Show-ConsoleObject -StaticSnapshot avoids interactive input in scripts. Native Format-Table, Format-List and Format-Wide: FormatCommands restyles direct console output while keeping PowerShell's column choices; it is included by default use -Exclude FormatCommands to leave it native. You do not need both automatic and explicit formatting. The -Feature parameter requires ConsoleMonster 2.1.0 or later.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## SYNTAX
### Objects (Default)
```powershell
Show-ConsoleTable [[-InputObject] <psobject>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Content
```powershell
Show-ConsoleTable -Content <Object> [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

## DESCRIPTION
Displays the table representation through the shared Show-Console implementation.

Three ways to use ConsoleMonster output: Automatic: Enable-ConsoleFormatting makes ordinary output readable for known and registered types. Explicit: pipe any objects to Show-ConsoleTable to choose columns, a title and grouping, or to Show-ConsoleObject to explore their members. These commands do not change session formatting; Show-ConsoleObject -StaticSnapshot avoids interactive input in scripts. Native Format-Table, Format-List and Format-Wide: FormatCommands restyles direct console output while keeping PowerShell's column choices; it is included by default use -Exclude FormatCommands to leave it native. You do not need both automatic and explicit formatting. The -Feature parameter requires ConsoleMonster 2.1.0 or later.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Show-ConsoleTable
```

Display pipeline input

## PARAMETERS

### -AlwaysCards
Uses record cards at every width.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsString
Returns plain text instead of displaying it.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the constructed widget.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Border
Table border style; default Rounded.

```yaml
Type: TableBorder
Parameter Sets: Objects, Content
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None, Rule

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
A table caption.

```yaml
Type: Object
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CardsBelow
Table card breakpoint; default 60 cells.

```yaml
Type: Int32
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
A widget, text or script block producing display content.

```yaml
Type: Object
Parameter Sets: Content
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Culture
The culture used for values.

```yaml
Type: CultureInfo
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Expand
Expands a table to the available width.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
Groups rows or records by a property or script block.

```yaml
Type: Object
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The object, widget or text to display.

```yaml
Type: PSObject
Parameter Sets: Objects
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxRows
Maximum displayed rows or chart groups; zero shows all.

```yaml
Type: Int32
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoHeaderSeparator
Underlines headers rather than drawing a separator.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoSort
Preserves the input order of groups.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the original input after display.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
Property names, patterns or calculated column records.

```yaml
Type: Object[]
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowGroupColumn
Keeps the grouped property in table rows.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowTotals
Shows a totals row for numeric columns.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusProperty
The property shown through the status presenter.

```yaml
Type: String
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The view title, including markup.

```yaml
Type: Object
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Layout width; zero resolves the terminal with a 100-cell fallback.

```yaml
Type: Int32
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Wrap
Wraps long table values or code lines.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WrapHeaders
Wraps column headings.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Content
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
Parameter Sets: Objects, Content
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
