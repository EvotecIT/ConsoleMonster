---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consoletable/
schema: 2.0.0
---
# Show-ConsoleTable
## SYNOPSIS
Shows objects as a table with fitted columns, readable values and optional grouping.

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
Pipe one object per row. Property is an ordered array: a string selects a property, and a hashtable configures a column. Name identifies the column and reads the like-named property by default. Expression can override the value with a different property name or with a script block that receives the row as $_.

Header changes the displayed heading. Presenter formats the cell value, such as rendering a byte count as a human-readable size. Priority affects which columns are kept when the grid has too little room. GroupBy adds group headings. ShowTotals sums numeric columns, and Wrap keeps long text readable. The renderer can use labelled cards for narrow output.

Show-ConsoleTable displays the table immediately. Use New-ConsoleTable to place a table in a layout or pass it to Export-ConsoleFrame. Enable-ConsoleFormatting opts into ConsoleMonster's display for ordinary prompt output.

## EXAMPLES

### EXAMPLE 1
```powershell
$tableOptions = @{
    Property = @('ProcessName', 'Id', 'WorkingSet64')
    Title = 'Running processes'
}
Get-Process | Select-Object -First 8 |
    Show-ConsoleTable @tableOptions
```

Display up to eight processes with three columns in the order given by Property. The last column reads WorkingSet64 and automatically displays the byte count in a readable size. The pipeline supplies the process objects, while the splat supplies the table options.

### EXAMPLE 2
```powershell
$inventory = @(
    [pscustomobject]@{ Site = 'East'; Server = 'WEB01'; Cores = 4 }
    [pscustomobject]@{ Site = 'East'; Server = 'SQL01'; Cores = 8 }
    [pscustomobject]@{ Site = 'West'; Server = 'WEB02'; Cores = 4 }
)
$tableOptions = @{
    ShowTotals = $true
    Property = @('Site', 'Server', 'Cores')
    GroupBy = 'Site'
}
$inventory | Show-ConsoleTable @tableOptions
```

Group the inventory by Site. Cores totals 16, with group subtotals of 12 for East and 4 for West. The sites appear in the group headings. Add ShowGroupColumn = $true to repeat Site in the rows.

### EXAMPLE 3
```powershell
$tableOptions = @{
    Property = @(
        'ProcessName'
        @{
            Name = 'MemoryMiB'
            Expression = { [math]::Round($_.WorkingSet64 / 1MB, 1) }
        }
    )
}
Get-Process | Select-Object -First 5 | Show-ConsoleTable @tableOptions
```

Calculate memory in MiB. The Property array mixes a plain property name with a calculated-column descriptor. ConsoleMonster evaluates Expression with $_ set to the process for that row. It divides WorkingSet64 by PowerShell's 1MB multiplier (1,048,576 bytes) and rounds the result to one decimal place.

### EXAMPLE 4
```powershell
$tableOptions = @{
    Wrap = $true
    Property = @('Task', 'Notes')
    Width = 60
    CardsBelow = 0
}
@(
    [pscustomobject]@{ Task = 'Backup'; Notes = 'Verified the report and checked the retained files.' }
    [pscustomobject]@{ Task = 'Review'; Notes = 'Ready for the next scheduled maintenance window.' }
) | Show-ConsoleTable @tableOptions
```

Wrap long notes in a 60-cell report. CardsBelow = 0 disables the width-based switch to cards. Wrap lets the Notes value continue on following lines in the grid.

### EXAMPLE 5
```powershell
$tableOptions = @{
    Property = @('Check', 'Status', 'Detail')
    StatusProperty = 'Status'
}
@(
    [pscustomobject]@{ Check = 'Storage'; Status = 'Pass'; Detail = '42 GB free' }
    [pscustomobject]@{ Check = 'Queue'; Status = 'Warn'; Detail = '12 items waiting' }
) | Show-ConsoleTable @tableOptions
```

Render a visible marker beside the Pass and Warn values. The StatusProperty value names the input property whose values should be interpreted as statuses. Property still controls which columns are present and their order.

### EXAMPLE 6
```powershell
$tableOptions = @{
    PassThru = $true
    Property = @('ProcessName', 'Id')
}
Get-Process | Select-Object -First 5 |
    Show-ConsoleTable @tableOptions |
    Measure-Object
```

Display a table and pass the original process objects along. Measure-Object counts those process objects. It receives up to five of them because PassThru preserves the input after display.

### EXAMPLE 7
```powershell
$events = @(
    [pscustomobject]@{ Id = 1014; Count = 52; Meaning = 'Name resolution timed out' }
    [pscustomobject]@{ Id = 7031; Count = 48; Meaning = 'Service terminated unexpectedly' }
)
$tableOptions = @{
    Title = 'Top event IDs'
    Width = 70
    CardsBelow = 0
    Property = @(
        @{ Name = 'Id'; Key = $true }
        @{ Name = 'Count'; Priority = 10 }
        @{ Name = 'Meaning'; Header = 'Description'; Priority = 1; MinWidth = 25 }
    )
}
$events | Show-ConsoleTable @tableOptions
```

Give an event-count report a short heading and reserve lower priority for a long description. This adapts the event-log-summary gallery's table of top event IDs. Header changes Meaning to Description while Name still reads Meaning. The key column identifies each row. Reduce Width to see how the grid treats the description when it cannot fit.

### EXAMPLE 8
```powershell
$records = @(
    [pscustomobject]@{ Name = 'Build'; RecordedAt = [datetimeoffset]'2026-10-21T08:00:00-04:00' }
    [pscustomobject]@{ Name = 'Test'; RecordedAt = [datetimeoffset]'2026-10-21T08:05:00-04:00' }
)
$tableOptions = @{
    Property = @('Name', @{ Name = 'RecordedAt'; Presenter = 'Date' })
    Presentation = @{ TimeZone = [TimeZoneInfo]::Utc }
}
$records | Show-ConsoleTable @tableOptions
```

Display the two recorded times as 12:00 and 12:05 UTC regardless of the local time zone. Presentation takes a PresentationOptions object. PowerShell converts the nested hashtable to that object when binding the splat. TimeZone affects absolute timestamps, while Now is the optional reference time for relative dates.

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
Default value: Rounded
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
Default value: 60
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
Default value: $false
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
Default value: 0
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
Default value: $false
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
Default value: $false
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
Default value: $false
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
Default value: $false
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
Default value: $false
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
Default value: 0
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
Default value: $false
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

- [https://consolemonster.dev/powershell/commands/show-consoletable/](https://consolemonster.dev/powershell/commands/show-consoletable/)
