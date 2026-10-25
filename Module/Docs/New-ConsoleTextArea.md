---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTextArea
## SYNOPSIS
Creates a named multiline text input for Show-ConsoleApp.

Emits a stable core TextArea widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTextArea [-Key] <string> [[-Label] <string>] [-Value <string>] [-Placeholder <string>] [-MaxLength <int>] [-Height <int>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [<CommonParameters>]
```

## DESCRIPTION
Creates a named multiline text input for Show-ConsoleApp.

Emits a stable core TextArea widget. Place inputs in the outer layout, outside dependent bindings. The final state contains the input's committed value.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleTextArea -Key Notes -Height 4 -Value 'Draft'
```

Create an application input.

## PARAMETERS

### -Disabled
Disables editing and removes the input from keyboard focus traversal.

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

### -Height
Number of visible text rows; defaults to six.

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

### -Key
Unique key in the application's state and final hashtable.

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

### -Label
Visible label; defaults to the name using the core control's labels.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxLength
Maximum text length in UTF-16 units.

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

### -OnChange
Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
Initial multiline text; empty by default.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.TextArea`

## RELATED LINKS

- None
