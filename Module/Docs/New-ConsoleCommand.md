---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleCommand
## SYNOPSIS
Defines an in-app action for buttons, palettes and grid rows.

Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 requests cancellation while the app stays open. After a two-second grace period the app becomes available even when a native call is still stopping. That command cannot start again until its previous runspace exits; other commands remain available. Cancellation cannot undo effects already performed by an external operation.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCommand [-Key] <string> -ScriptBlock <scriptblock> [-Label <string>] [-Shortcut <string>] [-ConfirmPrompt <string>] [-ProgressDialog] [-ArgumentList <Object[]>] [-ImportModule <string[]>] [<CommonParameters>]
```

## DESCRIPTION
Defines an in-app action for buttons, palettes and grid rows.

Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 requests cancellation while the app stays open. After a two-second grace period the app becomes available even when a native call is still stopping. That command cannot start again until its previous runspace exits; other commands remain available. Cancellation cannot undo effects already performed by an external operation.

## EXAMPLES

### EXAMPLE 1
```powershell
$restart = New-ConsoleCommand -Key Restart -Label 'Preview restart' -Shortcut Ctrl+R -ConfirmPrompt 'Preview selected services?' -ScriptBlock { param($Row) Restart-Service -Name $Row.Name -WhatIf }; Get-Service | Show-ConsoleGrid -Command $restart
```

Preview restarting selected services after confirmation.

## PARAMETERS

### -ArgumentList
Explicit values serialized when defining the command and passed after Row.

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

### -ConfirmPrompt
Optional modal confirmation text. {Selection} expands to the focused row label or marked-row count. No is the initial answer.

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

### -ImportModule
Modules to import in the operation's isolated runspace.

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

### -Key
Stable command identity.

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
Visible command text; defaults to Key.

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

### -ProgressDialog
Shows a progress dialog with Cancel and Background, followed by the operation summary.

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

### -ScriptBlock
Operation source, receiving param($Row, ...explicit arguments). Closure variables are not inherited.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Shortcut
Optional key chord, for example Ctrl+R or F5.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.AppCommand`

## RELATED LINKS

- None
