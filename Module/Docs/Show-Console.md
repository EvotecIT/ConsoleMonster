---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-Console
## SYNOPSIS
Shows objects, text and widgets in a readable view that fits the terminal.

Pipe objects to choose a view automatically, or use View for a table, list, object tree, source code, JSON, error or Markdown. Property chooses fields; Width sets the available space. Use New-ConsoleChart, New-ConsoleRule and New-ConsoleTree to construct those widgets. AsWidget returns a view for layout or Export-ConsoleFrame; PassThru returns the original objects after showing them.

## SYNTAX
### Objects (Default)
```powershell
Show-Console [[-InputObject] <psobject>] [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-ErrorLayout <string>] [-MaxInner <int>] [-NoPager] [<CommonParameters>]
```

### Path
```powershell
Show-Console -Path <string> [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-ErrorLayout <string>] [-MaxInner <int>] [-NoPager] [<CommonParameters>]
```

### Content
```powershell
Show-Console -Content <Object> [-View <string>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-AsWidget] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [-ShowEmpty] [-Depth <int>] [-MaxItems <int>] [-MaxNodes <int>] [-Exclude <string[]>] [-Language <string>] [-LineNumbers] [-ErrorLayout <string>] [-MaxInner <int>] [-NoPager] [<CommonParameters>]
```

## DESCRIPTION
Shows objects, text and widgets in a readable view that fits the terminal.

Pipe objects to choose a view automatically, or use View for a table, list, object tree, source code, JSON, error or Markdown. Property chooses fields; Width sets the available space. Use New-ConsoleChart, New-ConsoleRule and New-ConsoleTree to construct those widgets. AsWidget returns a view for layout or Export-ConsoleFrame; PassThru returns the original objects after showing them.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | Select-Object -First 5 | Show-Console
```

Show a few running processes

### EXAMPLE 2
```powershell
Get-Process | Select-Object -First 3 | Show-Console -View List -Property ProcessName, Id, WorkingSet64
```

Read selected fields as a list

### EXAMPLE 3
```powershell
Get-Item . | Show-Console -View Tree -Depth 1 -Property Name, FullName, LastWriteTime
```

Inspect a file object's members with a bounded tree

### EXAMPLE 4
```powershell
Show-Console -View Code -Language PowerShell -LineNumbers -Content {
    'Get-Process | Select-Object -First 5'
}
```

Show a PowerShell snippet with line numbers

### EXAMPLE 5
```powershell
Show-Console -Content {
    New-ConsolePanel -Title 'Build summary' -Content {
        New-ConsoleRows -Gap 1 -Content {
            New-ConsoleText 'All checks passed' -Role Pass
            New-ConsoleRule -Title 'Ready to review'
        }
    }
}
```

Compose a panel with native PowerShell script blocks

### EXAMPLE 6
```powershell
Get-Process | Select-Object -First 3 |
                Show-Console -View List -Property ProcessName, Id -AsWidget |
                Export-ConsoleFrame -Format Text
```

Return a plain-text document for another pipeline command

## PARAMETERS

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

### -Border
Table border style; default Rounded.

```yaml
Type: TableBorder
Parameter Sets: Objects, Path, Content
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

### -ErrorLayout
Error detail: Compact, Standard or Detailed.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Compact, Standard, Detailed

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

### -View
Auto, Table, List, Tree, Code, Json, Error or Markdown. Tree inspects object members; New-ConsoleTree builds node hierarchies.

```yaml
Type: String
Parameter Sets: Objects, Path, Content
Aliases: None
Possible values: Auto, Table, List, Tree, Code, Json, Error, Markdown

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
