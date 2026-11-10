---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDatePicker
## SYNOPSIS
Creates a persistent civil-date or inclusive range picker.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDatePicker [-Key] <string> [[-Label] <string>] [-Value <Object>] [-Range] [-Selection <DateSelectionOptions>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
Uses Gregorian arithmetic with culture-aware labels. Arrows move the cursor, PageUp/PageDown change month, Ctrl+PageUp/PageDown change year, and Enter commits. In range mode Space anchors a range and Shift+arrows extend it. Bounds and unavailable dates reject a whole invalid range.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDatePicker -Key Window -Range -Value ([datetime]'2026-10-08')
```

Choose a maintenance range.

### EXAMPLE 2
```powershell
New-ConsoleDatePicker -Key Window -Range -Selection @{ Today = [datetime]'2026-10-08'; FirstDayOfWeek = 'Monday' }
```

Configure today and the first weekday with a hashtable.

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
Default value: $false
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

### -Range
Commits one inclusive DateRange.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Selection
Shared bounds, culture, today and marked-day model. Supply a hashtable such as @{ Today = [datetime]'2026-10-08'; FirstDayOfWeek = 'Monday' }; PowerShell converts its entries to the shared selection model.

```yaml
Type: DateSelectionOptions
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
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValidationMessage
Initial validation text displayed below the input.

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

### -Value
Initial DateTime or DateRange; a scalar range default means Start=End.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.DatePicker`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoledatepicker/](https://consolemonster.dev/powershell/commands/new-consoledatepicker/)
