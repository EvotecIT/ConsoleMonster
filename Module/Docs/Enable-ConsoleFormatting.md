---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Enable-ConsoleFormatting
## SYNOPSIS
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process) and services (Get-Service), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.

## SYNTAX
### __AllParameterSets
```powershell
Enable-ConsoleFormatting [-Force] [-NoBuiltIn] [<CommonParameters>]
```

## DESCRIPTION
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process) and services (Get-Service), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting -Force
```


## PARAMETERS

### -Force
Replaces an existing Out-Default function that is not ConsoleMonster's.

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

### -NoBuiltIn
Only uses the types registered with Register-ConsoleView, not the built-in ones (files, processes, services).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
