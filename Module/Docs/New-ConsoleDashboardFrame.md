---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDashboardFrame
## SYNOPSIS
Creates a dashboard frame: a summary with a title line, meters, a needs-attention list, sections, a legend and a footer.

For a script that builds its own summary. The frame prints once and lays itself out for the terminal's width: the facts follow the title and the states sit at its right, meters fill a row in the order given, Needs attention lists problems worst first, summary sections pack beside it, detail sections print below at full width, then the legend and the footer. Below 80 columns meters stack and sections take the whole width. Meters, states, attention items and sections are records or hashtables, the way New-ConsoleTabs takes pages. A module that owns summary objects declares its frame in C# and registers it as their default display instead.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDashboardFrame [-Title] <string> [-Fact <string[]>] [-State <Object[]>] [-ObservedAt <DateTimeOffset>] [-Meter <Object[]>] [-Attention <Object[]>] [-AttentionNoun <string>] [-AttentionOutOf <long>] [-Section <Object[]>] [-Legend <Object>] [-FooterCount <string[]>] [-FooterHint <FrameHint>] [-Length <ConsoleViewLength>] [<CommonParameters>]
```

## DESCRIPTION
Creates a dashboard frame: a summary with a title line, meters, a needs-attention list, sections, a legend and a footer.

For a script that builds its own summary. The frame prints once and lays itself out for the terminal's width: the facts follow the title and the states sit at its right, meters fill a row in the order given, Needs attention lists problems worst first, summary sections pack beside it, detail sections print below at full width, then the legend and the footer. Below 80 columns meters stack and sections take the whole width. Meters, states, attention items and sections are records or hashtables, the way New-ConsoleTabs takes pages. A module that owns summary objects declares its frame in C# and registers it as their default display instead.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDashboardFrame -Title 'Server estate' -Fact 'contoso.example', '48 servers' -State @{ State = 'Failing'; Count = 1; Word = 'failing' } -Meter @(
    @{ Label = 'Servers healthy'; Healthy = 44; Warning = 3; Failing = 1 }
    @{ Label = 'Backups current'; Healthy = 46; Whole = 48 }
) -Attention @{ State = 'Failing'; Identity = 'SQL-03'; Reason = 'Disk D: 97% full'; Values = '97%' } -AttentionNoun server -AttentionOutOf 48 -Section @{
    Title = 'Sites'; Content = { New-ConsoleKeyValue -InputObject ([ordered] @{ London = 22; Amsterdam = 16 }) }
} | Show-Console
```

A small estate summary.

## PARAMETERS

### -Attention
Needs-attention items: records with State, Identity, Reason, and optionally Order (worst first when given) and Values (short values beside the item).

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

### -AttentionNoun
What the attention list counts, in the singular, as in '3 of 48 servers' (default item).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AttentionOutOf
The total the attention list counts against.

```yaml
Type: Int64
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Fact
Facts after the title, such as the scope and counts.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FooterCount
Counts in the footer, such as '48 servers'.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FooterHint
The footer hint: None (default, for a frame the script builds), EveryRow or EveryProperty (for frames shown for pipeline objects).

```yaml
Type: FrameHint
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: EveryRow, EveryProperty, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Legend
Legend entries: an ordered dictionary of state to word, or records with State and Word.

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

### -Length
An explicit length (Summary, Standard or Full); without it the session preference or Standard.

```yaml
Type: ConsoleViewLength
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Summary, Standard, Full

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Meter
Headline meters in priority order: records with Label and Kind (Composition, Ratio, Count, Fact or Missing). Composition takes Healthy, Warning, Failing, Unknown and NotEvaluated; Ratio takes Healthy, Whole and Rest; Count takes a number Value; Fact takes a text Value; Count and Fact also take Fact and State; Missing takes Reason. Without Kind, Whole means Ratio, Healthy means Composition, a number Value means Count, other Value means Fact, and no value means Missing.

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

### -ObservedAt
When the data was observed; the title line shows how long ago.

```yaml
Type: DateTimeOffset
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Section
Sections: records with Title and Content (widgets, text or a script block), and optionally Placement (Summary or Detail), Facts, Meta, States, MinWidth, MaxWidth, Grow and FullWidth.

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

### -State
States at the right of the title line: records or hashtables with State (Healthy, Warning, Failing, Unknown or NotEvaluated), Count and Word.

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

### -Title
The literal title, usually the view name.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
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

- `ConsoleMonster.Frames.DashboardFrame`

## RELATED LINKS

- None
