---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleTable
## SYNOPSIS
Shows objects as a table with fitted columns, readable values and optional grouping.

Pipe objects and choose columns with Property, including calculated properties. GroupBy adds group headings, ShowTotals sums numeric columns, and Wrap keeps long text readable. Narrow output uses labelled cards below CardsBelow. This command displays the table immediately. Use New-ConsoleTable to place a table in a layout or pass it to Export-ConsoleFrame. Enable-ConsoleFormatting is the separate opt-in for ordinary prompt output.

## SYNTAX
### Objects (Default)
```powershell
Show-ConsoleTable [[-InputObject] <psobject>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [<CommonParameters>]
```

### Content
```powershell
Show-ConsoleTable -Content <Object> [-Property <Object[]>] [-GroupBy <Object>] [-Title <Object>] [-Caption <Object>] [-Width <int>] [-Culture <cultureinfo>] [-PassThru] [-ShowTotals] [-MaxRows <int>] [-Border <TableBorder>] [-CardsBelow <int>] [-NoHeaderSeparator] [-Expand] [-StatusProperty <string>] [-ShowGroupColumn] [-NoSort] [-Wrap] [<CommonParameters>]
```

## DESCRIPTION
Shows objects as a table with fitted columns, readable values and optional grouping.

Pipe objects and choose columns with Property, including calculated properties. GroupBy adds group headings, ShowTotals sums numeric columns, and Wrap keeps long text readable. Narrow output uses labelled cards below CardsBelow. This command displays the table immediately. Use New-ConsoleTable to place a table in a layout or pass it to Export-ConsoleFrame. Enable-ConsoleFormatting is the separate opt-in for ordinary prompt output.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | Select-Object -First 8 |
                Show-ConsoleTable -Property ProcessName, Id, WorkingSet64 -Title 'Running processes'
```

Show processes with the columns you need

### EXAMPLE 2
```powershell
$inventory = @(
    [pscustomobject]@{ Site = 'East'; Server = 'WEB01'; Cores = 4 }
    [pscustomobject]@{ Site = 'East'; Server = 'SQL01'; Cores = 8 }
    [pscustomobject]@{ Site = 'West'; Server = 'WEB02'; Cores = 4 }
)
$inventory | Show-ConsoleTable -Property Site, Server, Cores -GroupBy Site -ShowTotals
```

Group a small inventory and total its numeric columns

### EXAMPLE 3
```powershell
Get-Process | Select-Object -First 5 | Show-ConsoleTable -Property ProcessName, @{
                Name = 'MemoryMB'
                Expression = { [math]::Round($_.WorkingSet64 / 1MB, 1) }
            }
```

Calculate a memory column in megabytes

### EXAMPLE 4
```powershell
@(
                [pscustomobject]@{ Task = 'Backup'; Notes = 'Verified the report and checked the retained files.' }
                [pscustomobject]@{ Task = 'Review'; Notes = 'Ready for the next scheduled maintenance window.' }
            ) | Show-ConsoleTable -Property Task, Notes -Width 60 -CardsBelow 0 -Wrap
```

Wrap long notes in a narrow report

### EXAMPLE 5
```powershell
@(
                [pscustomobject]@{ Check = 'Storage'; Status = 'Pass'; Detail = '42 GB free' }
                [pscustomobject]@{ Check = 'Queue'; Status = 'Warn'; Detail = '12 items waiting' }
            ) | Show-ConsoleTable -Property Check, Status, Detail -StatusProperty Status
```

Give health states a visible status marker

### EXAMPLE 6
```powershell
Get-Process | Select-Object -First 5 |
                Show-ConsoleTable -Property ProcessName, Id -PassThru |
                Measure-Object
```

Display objects and keep them in the pipeline

## PARAMETERS

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
