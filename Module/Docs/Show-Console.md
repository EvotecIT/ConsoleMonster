---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-Console
## SYNOPSIS
Displays pipeline input in its automatic or explicit representation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## SYNTAX
### Objects (Default)
```powershell
Show-Console [[-InputObject] <psobject>] [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-ChildrenProperty <string>] [-Children <scriptblock>] [-LabelProperty <string>] [-DetailProperty <string>] [-MaxItems <int>] [-MaxNodes <int>] [-Include <string[]>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-Highlight <string[]>] [-FirstLine <int>] [-MaxLines <int>] [-NoTitle] [-Type <ChartType>] [-Label <Object>] [-Sort <PipelineChartSort>] [-Bins <int>] [-Unit <string>] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-Compact] [-MaxInner <int>] [-NoSource] [-NoPager] [-Alignment <HorizontalAlignment>] [-LineRole <ThemeToken>] [-LineStyle <BoxStyle>] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Path
```powershell
Show-Console -Path <string> [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-ChildrenProperty <string>] [-Children <scriptblock>] [-LabelProperty <string>] [-DetailProperty <string>] [-MaxItems <int>] [-MaxNodes <int>] [-Include <string[]>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-Highlight <string[]>] [-FirstLine <int>] [-MaxLines <int>] [-NoTitle] [-Type <ChartType>] [-Label <Object>] [-Sort <PipelineChartSort>] [-Bins <int>] [-Unit <string>] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-Compact] [-MaxInner <int>] [-NoSource] [-NoPager] [-Alignment <HorizontalAlignment>] [-LineRole <ThemeToken>] [-LineStyle <BoxStyle>] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

### Content
```powershell
Show-Console -Content <Object> [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsString] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-ChildrenProperty <string>] [-Children <scriptblock>] [-LabelProperty <string>] [-DetailProperty <string>] [-MaxItems <int>] [-MaxNodes <int>] [-Include <string[]>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-Highlight <string[]>] [-FirstLine <int>] [-MaxLines <int>] [-NoTitle] [-Type <ChartType>] [-Label <Object>] [-Sort <PipelineChartSort>] [-Bins <int>] [-Unit <string>] [-ShowFrameworkFrames] [-MaxFrames <int>] [-NoStack] [-Compact] [-MaxInner <int>] [-NoSource] [-NoPager] [-Alignment <HorizontalAlignment>] [-LineRole <ThemeToken>] [-LineStyle <BoxStyle>] [-TitleIndent <Int32>] [-WrapHeaders] [-KeepAllColumns] [-GroupHeadersAsRows] [-RecordViewRows <int>] [-ShowKeyField] [-NoHiddenColumns] [-NoStableWidths] [-NoHeader] [-AlwaysCards] [-RowTitles <string[]>] [-ShowSubtotals] [-TotalLabel <string>] [-WrapLines <int>] [-TotalRowCount <Int32>] [-Presentation <PresentationOptions>] [<CommonParameters>]
```

## DESCRIPTION
Displays pipeline input in its automatic or explicit representation.

Table and List are idiomatic shortcuts to the same view construction and output path. AsWidget and AsString capture the result; export uses Export-ConsoleFrame.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Show-Console
```

Display pipeline input

## PARAMETERS

### -Alignment
The rule title alignment; default Left.

```yaml
Type: HorizontalAlignment
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Left, Center, Right

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AlwaysCards
Uses record cards at every width.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Bins
Histogram bins; default ten.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None

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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Children
Produces children for a hierarchy node.

```yaml
Type: ScriptBlock
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChildrenProperty
The property containing a hierarchy node children.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Compact
Uses the compact everyday error layout.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
Maximum nesting depth; default three.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DetailProperty
The hierarchy detail property.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exclude
Object members omitted without reading their getters.

```yaml
Type: String[]
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FirstLine
The first code line number; default one.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Highlight
Line numbers or ranges to highlight.

```yaml
Type: String[]
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Include
Object members to include.

```yaml
Type: String[]
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The chart label property or expression; default Name.

```yaml
Type: Object
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LabelProperty
The hierarchy label property.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Language
Code language; default Auto.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineNumbers
Shows code line numbers.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineRole
Theme role for a rule line; default Border.

```yaml
Type: ThemeToken
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LineStyle
Optional rule line glyph style.

```yaml
Type: BoxStyle
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Rounded, Single, Double, Heavy, Ascii, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxFrames
Maximum application frames per error; default eight.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxInner
Maximum inner errors; default four.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxItems
Maximum inspected items per collection; default 100.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxLines
Maximum displayed code lines; default 2000.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxNodes
Maximum hierarchy nodes; default 1000.

```yaml
Type: Int32
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoPager
Disables default-on Markdown paging.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoSource
Suppresses default-on source excerpts in error views.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoStack
Hides error stack frames.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoTitle
Hides a code file title.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A file for Code, Json or Markdown.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Presentation
Typed culture, clock and time-zone configuration for values.

```yaml
Type: PresentationOptions
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowEmpty
Keeps empty fields in record views.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ShowFrameworkFrames
Shows framework stack frames in error views.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Sort
Chart ordering.

```yaml
Type: PipelineChartSort
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: None, Ascending, Descending

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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Type
Numeric chart representation: Bar, Histogram or Sparkline. Line, Stacked and Scatter charts take series; build them with New-ConsoleChart.

```yaml
Type: ChartType
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Bar, Histogram, Sparkline

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
The chart numeric unit.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -View
Auto, Table, List, Tree, Chart, Code, Json, Error, Markdown or Rule.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Auto, Table, List, Tree, Chart, Code, Json, Error, Markdown, Rule

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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
Parameter Sets: Objects, Path, Content
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
