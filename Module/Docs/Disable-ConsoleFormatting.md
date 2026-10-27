---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Disable-ConsoleFormatting
## SYNOPSIS
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.

Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress function is restored, and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.

## SYNTAX
### __AllParameterSets
```powershell
Disable-ConsoleFormatting [-Feature <ConsoleFormattingFeature[]>] [<CommonParameters>]
```

## DESCRIPTION
Turns off the ConsoleMonster default formatting (Enable-ConsoleFormatting), all of it or some features.

Without -Feature everything Enable-ConsoleFormatting turned on is turned off and the session is as it was: the Out-Default function goes (or the user's own function replaced with -Force comes back), the Write-Progress function is restored, and $ErrorView was never changed. With -Feature only those parts are turned off. Streams and ReadLine release their native colour settings. Shared properties keep the latest surviving installation; the last owner restores the first exact baseline, including subsequent edits to owned properties. The Out-Default function stays while any feature that uses it is on; Streams and ReadLine alone need no function. Registrations stay for the next Enable-ConsoleFormatting.

## EXAMPLES

### EXAMPLE 1
```powershell
Disable-ConsoleFormatting
```

Back to PowerShell's formatting

### EXAMPLE 2
```powershell
Disable-ConsoleFormatting -Feature Progress
```

Keep the views, go back to PowerShell's progress

## PARAMETERS

### -Feature
What to turn off: Output, ErrorView, Progress, Streams, ReadLine, FormatCommands, Help, Errors, GridView, Prompt or All (Output, ErrorView, Streams and ReadLine). Streams and ReadLine restore exactly the saved native colour properties, even if they were edited while enabled; other properties stay untouched. Without -Feature every enabled feature is removed, including explicit Progress, FormatCommands, Help, Errors, GridView and Prompt.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Output, ErrorView, Progress, FormatCommands, Streams, Help, Errors, ReadLine, All, GridView, Prompt

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

- `None`

## RELATED LINKS

- None
