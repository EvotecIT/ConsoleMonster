---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDialog
## SYNOPSIS
Creates a typed modal form for the running app's dialog service.

Construction does not read input. AppDialogs.Show hosts the form on the owning application thread and returns DialogResult of AppState through its completion callback.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDialog [[-Content] <Object>] -Title <string> [-AcceptText <string>] [-CancelText <string>] [-DefaultAccept] [-InitialFocus <string>] [-MaxWidth <int>] [-Validate <Func[AppState,string]>] [<CommonParameters>]
```

## DESCRIPTION
Creates a typed modal form for the running app's dialog service.

Construction does not read input. AppDialogs.Show hosts the form on the owning application thread and returns DialogResult of AppState through its completion callback.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDialog -Title Options -InitialFocus Destination -Content { New-ConsoleTextInput -Key Destination }
```

Define fields for an app-owned dialog.

## PARAMETERS

### -AcceptText
Affirmative button label.

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

### -CancelText
Cancellation button label.

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

### -Content
Retained widgets or a script block that emits fields.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -DefaultAccept
Focuses the affirmative button initially instead of Cancel.

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

### -InitialFocus
Field key to focus instead of the default button.

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

### -MaxWidth
Maximum width in terminal cells.

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

### -Title
Visible heading.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Validate
App-thread validator returning an error string or null.

```yaml
Type: Func`2
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

- `System.Object`

## OUTPUTS

- ``ConsoleMonster.Interactive.FormDialog`1[[ConsoleMonster.Interactive.AppState, ConsoleMonster, Version=2.1.0.0, Culture=neutral, PublicKeyToken=null]]``

## RELATED LINKS

- None
