---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleBreadcrumb
## SYNOPSIS
Creates a breadcrumb for Show-ConsoleApp: a path such as Settings › Network › Proxy, its own or that of an outline's current entry.

## SYNTAX
### Segment (Default)
```powershell
New-ConsoleBreadcrumb [-Key] <string> [-Segments] <string[]> [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [<CommonParameters>]
```

### Outline
```powershell
New-ConsoleBreadcrumb [-Key] <string> -Outline <Object> [-Ancestors <string[]>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [<CommonParameters>]
```

## DESCRIPTION
The last segment is where you are. With -Outline it follows that outline's current entry, after any -Ancestors segments, and opening a heading in it opens that outline entry. When the row is too narrow the leading segments give way to an ellipsis. It is a focus stop that initial focus skips: Left/Right choose a segment and Enter or a click opens it; the final state holds the last segment opened.

## EXAMPLES

### EXAMPLE 1
```powershell
$sections = New-ConsoleOutline -Key Sections -Items 'General', @{ Title = 'Proxy'; Level = 2 }, 'Logging' -SelectedIndex 1
Show-ConsoleApp -Content {
    New-ConsoleBreadcrumb -Key Where -Ancestors 'Settings' -Outline $sections
    $sections
}
```

Show where the reader is in an outline.

## PARAMETERS

### -Ancestors
Segments shown before the outline's headings, such as the screen or tab name.

```yaml
Type: String[]
Parameter Sets: Outline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Disabled
Removes the breadcrumb from keyboard focus traversal and pointer input.

```yaml
Type: SwitchParameter
Parameter Sets: Segment, Outline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state; the value is the last segment opened.

```yaml
Type: String
Parameter Sets: Segment, Outline
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnChange
Runs once each time a segment is opened, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.

```yaml
Type: ScriptBlock
Parameter Sets: Segment, Outline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Outline
The outline whose current entry the breadcrumb shows (New-ConsoleOutline output).

```yaml
Type: Object
Parameter Sets: Outline
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Segments
The segments, outermost first.

```yaml
Type: String[]
Parameter Sets: Segment
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

```yaml
Type: Int32
Parameter Sets: Segment, Outline
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

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.Breadcrumb`

## RELATED LINKS

- None
