---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleCommand
## SYNOPSIS
Defines an in-app action for buttons, palettes and grid rows.

Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 cancels work while the app stays open.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCommand [-Key] <string> -ScriptBlock <scriptblock> [-Label <string>] [-Shortcut <string>] [-ConfirmPrompt <string>] [-ArgumentList <Object[]>] [-ImportModule <string[]>] [<CommonParameters>]
```

## DESCRIPTION
Defines an in-app action for buttons, palettes and grid rows.

Creates a passive command definition. Work runs in an isolated runspace with explicit arguments and module imports, not the defining closure. The first script parameter receives a deserialized row snapshot. Selected or marked rows are captured before optional modal confirmation and run in order. Output, host messages, warnings, errors and progress stream into the app's log. F6 cancels work while the app stays open.

## EXAMPLES

### EXAMPLE 1
```powershell
$restart = New-ConsoleCommand -Key Restart -Label 'Restart service' -Shortcut Ctrl+R -ConfirmPrompt 'Restart selected services?' -ScriptBlock { param($Row) Restart-Service -Name $Row.Name -ErrorAction Stop }; Get-Service | Show-ConsoleGrid -Action $restart
```

Restart selected services after confirmation without closing the viewer.

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

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.AppCommand`

## RELATED LINKS

- None
