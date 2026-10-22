---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Enable-ConsoleFormatting
## SYNOPSIS
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.

## SYNTAX
### __AllParameterSets
```powershell
Enable-ConsoleFormatting [-Feature <ConsoleFormattingFeature[]>] [-Force] [-NoBuiltIn] [-ProfileSnippet] [-Detailed] [<CommonParameters>]
```

## DESCRIPTION
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. If you already have your own Out-Default function, it is kept and nothing is enabled unless you pass -Force.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10
```

Turn it on for the session

### EXAMPLE 2
```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

The lines to keep it on in every session (printed, never written to the profile)

### EXAMPLE 3
```powershell
Enable-ConsoleFormatting -Feature All
```

Default output, the error view, progress and native stream colours

### EXAMPLE 4
```powershell
Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose
```

Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour

### EXAMPLE 5
```powershell
Enable-ConsoleFormatting -Feature Errors; Enable-ConsoleFormatting -Feature Errors -Detailed -ProfileSnippet
```

Compact errors at the prompt; use -Detailed to include stacks. All does not enable this feature.

## PARAMETERS

### -Detailed
Includes script and .NET stack frames in the opt-in Errors layout. Used only with -Feature Errors; the default layout folds inner exceptions and hides stacks.

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

### -Feature
Errors is a separate opt-in for message-first errors on a direct, local, non-transcribed console. It preserves $ErrorView, CategoryView and Get-Error; -Detailed adds stacks. It never replaces an error-producing cmdlet or changes error records, preferences or redirection.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Output, ErrorView, Progress, FormatCommands, Streams, All, Errors

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Replaces an existing Out-Default function or Write-Progress alias that is not ConsoleMonster's (Disable-ConsoleFormatting puts it back).

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
Only takes over the types registered with Register-ConsoleView, not the built-in ones (files, processes, services, dictionaries). The built-in views still pick the columns of Format-ConsoleTable and the other commands.

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

### -ProfileSnippet
Prints the lines to add to your PowerShell profile so every session starts with ConsoleMonster's formatting, with the options you passed, and changes nothing: the profile is never edited and formatting is not enabled. Pipe them to Add-Content $PROFILE yourself if you want them.

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
