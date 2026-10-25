---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Read-ConsoleForm
## SYNOPSIS
Shows a form or wizard and returns the answers.

The form opens on the alternate screen (or in the scrollback with -Inline). Fields are edited in place; choice fields open a filterable list; dependent options load with a spinner. The review page shows every answer and the equivalent command (copy it, save the answers as a named preset, or run). The answers come back as one object with a property per visible field (secrets as SecureString unless -AsPlainText); -PassThru returns the full result (Submitted, Values, CommandLine) instead. Cancelling returns nothing. -AppName stores presets, and with -RememberLastChoices the last answers, per user under %APPDATA%\ConsoleMonster\<app>. In remoting and the ISE the fields are asked one by one; without a user the defaults are used, or an error names the first field that needs a value.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleForm [-Form] <Form> [-AppName <string>] [-RememberLastChoices] [-StorageRoot <string>] [-Inline] [-Preset <string>] [-PassThru] [-AsPlainText] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Shows a form or wizard and returns the answers.

The form opens on the alternate screen (or in the scrollback with -Inline). Fields are edited in place; choice fields open a filterable list; dependent options load with a spinner. The review page shows every answer and the equivalent command (copy it, save the answers as a named preset, or run). The answers come back as one object with a property per visible field (secrets as SecureString unless -AsPlainText); -PassThru returns the full result (Submitted, Values, CommandLine) instead. Cancelling returns nothing. -AppName stores presets, and with -RememberLastChoices the last answers, per user under %APPDATA%\ConsoleMonster\<app>. In remoting and the ISE the fields are asked one by one; without a user the defaults are used, or an error names the first field that needs a value.

## EXAMPLES

### EXAMPLE 1
```powershell
$answers = Read-ConsoleForm $form -AppName 'ShareTool' -RememberLastChoices
if ($answers) { New-AppShare -Name $answers.Name -Path $answers.Path }
```

Run a wizard and use the answers

## PARAMETERS

### -AppName
The app name presets and remembered choices are stored under.

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

### -AsPlainText
Returns secrets as plain strings.

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

### -Form
The form from New-ConsoleForm.

```yaml
Type: Form
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Inline
Draws the form in the scrollback instead of the alternate screen.

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

### -Mode
Forces how to ask: Rich, Line, Accessible or None.

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the full result (Submitted, Values, CommandLine) instead of the answers.

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

### -Preset
A preset to apply first: a built-in one (New-ConsoleForm -Preset) or one saved under -AppName.

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

### -RememberLastChoices
Remembers the submitted answers and offers them next time (needs -AppName).

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

### -StorageRoot
The folder that holds the app's storage folder (default: per user).

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

- `ConsoleMonster.Interactive.Form`

## OUTPUTS

- `System.Management.Automation.PSObject`
- `ConsoleMonster.Interactive.FormResult`

## RELATED LINKS

- None
