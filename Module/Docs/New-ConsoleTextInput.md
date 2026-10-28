---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleTextInput
## SYNOPSIS
Creates a named single-line text input for Show-ConsoleApp.

Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

## SYNTAX
### Single (Default)
```powershell
New-ConsoleTextInput [-Key] <string> [[-Label] <string>] [-Value <string>] [-Placeholder <string>] [-MaxLength <int>] [-Mask <string>] [-Role <ThemeToken>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

### Multiline
```powershell
New-ConsoleTextInput [-Key] <string> [[-Label] <string>] -Multiline [-Value <string>] [-Placeholder <string>] [-MaxLength <int>] [-Height <int>] [-ScrollRow <int>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a named single-line text input for Show-ConsoleApp.

Emits a stable core TextInput widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTextInput -Key Query -Placeholder 'Filter' -OnChange { $State.Edits = 1 + $State.Edits }
```

Create an application input.

## PARAMETERS

### -Disabled
Disables editing and removes the input from keyboard focus traversal.

```yaml
Type: SwitchParameter
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Visible rows for a multiline input; default six.

```yaml
Type: Int32
Parameter Sets: Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state and final hashtable.

```yaml
Type: String
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
Visible label; defaults to the name using the core control's labels.

```yaml
Type: String
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mask
Single-line mask; an empty string hides the text entirely.

```yaml
Type: String
Parameter Sets: Single
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxLength
Maximum text length in UTF-16 units.

```yaml
Type: Int32
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Multiline
Creates a multiline text area.

```yaml
Type: SwitchParameter
Parameter Sets: Multiline
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnChange
Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.

```yaml
Type: ScriptBlock
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Placeholder
Muted hint while the value is empty.

```yaml
Type: String
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Role
The single-line entered-text role.

```yaml
Type: ThemeToken
Parameter Sets: Single
Aliases: None
Possible values: Text, Muted, Subtle, Accent, Accent2, Pass, Fail, Warn, Info, Skip, Running, Pending, Border, Title, Header, Highlight, Surface, SurfaceAccent, Series1, Series2, Series3, Series4, Series5, Series6, Focus

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScrollRow
The first visible multiline row.

```yaml
Type: Int32
Parameter Sets: Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

```yaml
Type: Int32
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValidationMessage
Initial validation text displayed below the input.

```yaml
Type: String
Parameter Sets: Single, Multiline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
Initial text; empty by default.

```yaml
Type: String
Parameter Sets: Single, Multiline
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

- `ConsoleMonster.Interactive.TextInput`
- `ConsoleMonster.Interactive.TextArea`

## RELATED LINKS

- None
