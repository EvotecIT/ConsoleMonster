---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Enable-ConsoleFormatting
## SYNOPSIS
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. If you already have your own Out-Default function, it is kept and output formatting is not enabled unless you pass -Force. Prompt is a separate opt-in, excluded from All: a shortened path, cached git branch and dirty mark, last-command status and completed-history duration only above two seconds. Captures, transcripts, recordings, jobs, remoting and nested prompts retain the original prompt. PSReadLine handlers stay untouched. Disable or module removal restores the original prompt; oh-my-posh and Starship require Force.

## SYNTAX
### __AllParameterSets
```powershell
Enable-ConsoleFormatting [-Feature <ConsoleFormattingFeature[]>] [-Force] [-NoBuiltIn] [-ProfileSnippet] [<CommonParameters>]
```

## DESCRIPTION
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), commands (Get-Command), modules (Get-Module), event log records (Get-WinEvent), IP addresses (Get-NetIPAddress), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; nothing else changes: objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. If you already have your own Out-Default function, it is kept and output formatting is not enabled unless you pass -Force. Prompt is a separate opt-in, excluded from All: a shortened path, cached git branch and dirty mark, last-command status and completed-history duration only above two seconds. Captures, transcripts, recordings, jobs, remoting and nested prompts retain the original prompt. PSReadLine handlers stay untouched. Disable or module removal restores the original prompt; oh-my-posh and Starship require Force.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10
```

Turn it on for the session

### EXAMPLE 2
```powershell
Set-ConsoleTheme -Name Dark; Enable-ConsoleFormatting -Feature Prompt
```

Use a minimal themed shell prompt, separately from default output formatting

### EXAMPLE 3
```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

The lines to keep it on in every session (printed, never written to the profile)

### EXAMPLE 4
```powershell
Enable-ConsoleFormatting -Feature All
```

Default output, the error view, progress and native stream colours

### EXAMPLE 5
```powershell
Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose
```

Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour

## PARAMETERS

### -Feature
What to turn on: Output (the default: registered and built-in types as ConsoleMonster views), ErrorView (errors that reach the console in ConsoleMonster's error view, following $ErrorView: DetailedView adds the stack, CategoryView keeps PowerShell's), Progress (Write-Progress drawn as ConsoleMonster's progress area; Write-Progress becomes an alias of Write-ConsoleProgress), Streams (native stream colours), or All (Output, ErrorView, Progress and Streams). Streams sets PSStyle.Formatting Warning, Verbose, Debug and Error on PowerShell 7.2+, or available Host.PrivateData warning, verbose and debug foreground/background colours on older hosts. PowerShell keeps its prefixes, records, preferences, redirection and transcript behaviour. No Write-* stream command is replaced. Re-enable Streams after changing themes; Disable restores the original values of only the properties ConsoleMonster set, including later user edits. Use Write-ConsoleStatus or Write-ConsoleLog for rich script messages. FormatCommands explicitly opts Format-Table, Format-List and Format-Wide into direct-host-only rendering, preserving native captures, jobs, remoting and transcripts; it is excluded from All. Prompt (excluded from All) opts into a minimal shell prompt with shortened path, cached git branch and dirty mark, last-command status and history duration only above two seconds. Captures, transcripts, recordings, remoting and nested prompts keep the original prompt. PSReadLine handlers stay untouched; oh-my-posh and Starship require Force. Disable or module removal restores the original prompt. Features add up across calls; Disable-ConsoleFormatting turns them off.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Output, ErrorView, Progress, FormatCommands, Streams, All, Prompt

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Replaces an existing Out-Default function or Write-Progress alias that is not ConsoleMonster's, or an oh-my-posh or Starship prompt with Feature Prompt. Disable-ConsoleFormatting restores the original.

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
