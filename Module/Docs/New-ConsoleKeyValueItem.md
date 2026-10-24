---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleKeyValueItem
## SYNOPSIS
Creates one key/value pair, with an optional status or presenter, for New-ConsoleKeyValue.

Use it inside the script block of New-ConsoleKeyValue or Show-ConsoleList when a pair needs more than a hashtable gives: a status marker before the value (-Status), markup in the value (-Markup), or a value formatted by a presenter (-Presenter Bytes, Duration, Date...). Values are shown literally unless -Markup is used. To add pairs to a list made earlier, pipe the list to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleKeyValueItem [-Key] <string> [[-Value] <Object>] [-Status <StatusKind>] [-Markup] [-Presenter <string>] [-Unit <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates one key/value pair, with an optional status or presenter, for New-ConsoleKeyValue.

Use it inside the script block of New-ConsoleKeyValue or Show-ConsoleList when a pair needs more than a hashtable gives: a status marker before the value (-Status), markup in the value (-Markup), or a value formatted by a presenter (-Presenter Bytes, Duration, Date...). Values are shown literally unless -Markup is used. To add pairs to a list made earlier, pipe the list to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleKeyValue -Content {
    New-ConsoleKeyValueItem 'Module' '[bold]ConsoleMonster[/]' -Markup
    New-ConsoleKeyValueItem 'Signing' 'skipped' -Status Skip
    New-ConsoleKeyValueItem 'Tests' '1 skipped on PowerShell 7' -Status Warn
} | Show-Console
```

Facts with status marks

### EXAMPLE 2
```powershell
New-ConsoleKeyValue -Content {
    New-ConsoleKeyValueItem 'Package size' 1482113 -Presenter Bytes
    New-ConsoleKeyValueItem 'Built in' (New-TimeSpan -Seconds 48) -Presenter Duration
} | Show-Console
```

Values formatted by presenters

## PARAMETERS

### -Key
The key (plain text).

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

### -Markup
Treat the value as markup.

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

### -Presenter
How the value is shown: Bytes, Duration, Date, Number, Boolean, Status, Progress, DataBar, Link, Markup or Text.

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

### -Status
A status marker before the value: Pass, Fail, Warn, Skip, Info, Running or Pending (PowerShell's words such as Success, Warning and Error work too).

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Unit
A unit for the Number presenter, for example ms.

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
The value. Shown literally unless -Markup is used; with -Presenter it keeps its type and is formatted by that presenter.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.KeyValueItem`

## RELATED LINKS

- None
