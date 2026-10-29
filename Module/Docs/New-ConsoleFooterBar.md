---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleFooterBar
## SYNOPSIS
Creates a reusable footer with key hints, messages and linked segments.

Hint maps key names to their labels. Segment accepts FooterSegment values or hashtables with Text, Link and Role. The footer is a widget and does not read input or start a session.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleFooterBar [[-Hint] <IDictionary>] [-Message <Object>] [-Right <Object>] [-Segment <Object[]>] [-KeepLast] [-HintsSource <Func[IEnumerable[KeyHint]]>] [-FixedSlots] [-Indent <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates a reusable footer with key hints, messages and linked segments.

Hint maps key names to their labels. Segment accepts FooterSegment values or hashtables with Text, Link and Role. The footer is a widget and does not read input or start a session.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleFooterBar -Hint ([ordered] @{ Enter = 'Accept'; Esc = 'Cancel' }) | Show-Console
```

Show navigation hints.

## PARAMETERS

### -FixedSlots
Keeps all context entries in fixed slots and shortens their labels to fit.

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

### -Hint
An ordered dictionary of key names and action labels.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -HintsSource
Reads context hints on the app input thread; unavailable KeyHint entries remain dimmed.

```yaml
Type: Func`1
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Indent
Initial indentation in terminal cells.

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

### -KeepLast
Keeps the last hint when space runs out.

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

### -Message
A message shown instead of key hints.

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

### -Right
Text at the right of the footer.

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

### -Segment
FooterSegment objects or Text/Link/Role hashtables.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.FooterBar`

## RELATED LINKS

- None
