---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Enable-ConsoleFormatting
## SYNOPSIS
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), JSON/REST and CSV records (PSCustomObject), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), PowerShell 7 ping replies, .NET DNS entries and IP addresses, Windows network objects, event log records (Get-WinEvent), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; JSON/CSV records show at most 50 rows and 24 fields with omission hints and nested summaries; numeric and date-like CSV strings align right without conversion. Commands group by module from 12 rows and show alias targets and command-type chips; modules group repeated names and show versions, shortened paths and exported command counts; history shows id, start time, duration, status and highlighted command text. Windows network views register only on Windows; ping and .NET DNS/IP views work across platforms. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. A custom Out-Default is kept unless you pass -Force for a feature that uses it. Streams, ReadLine, GridView and Prompt do not replace Out-Default.

## SYNTAX
### __AllParameterSets
```powershell
Enable-ConsoleFormatting [-Feature <ConsoleFormattingFeature[]>] [-Force] [-NoBuiltIn] [-ProfileSnippet] [-Detailed] [<CommonParameters>]
```

## DESCRIPTION
Shows the default output of common types through ConsoleMonster for this session (opt-in).

Files and folders (Get-ChildItem), processes (Get-Process), services (Get-Service), JSON/REST and CSV records (PSCustomObject), members (Get-Member), commands (Get-Command), modules (Get-Module), history (Get-History), PowerShell 7 ping replies, .NET DNS entries and IP addresses, Windows network objects, event log records (Get-WinEvent), volumes (Get-Volume) and dictionaries (@{ }), plus any type registered with Register-ConsoleView, are shown as ConsoleMonster tables and records when they reach the console. Members group by member type with highlighted signatures and muted types; JSON/CSV records show at most 50 rows and 24 fields with omission hints and nested summaries; numeric and date-like CSV strings align right without conversion. Commands group by module from 12 rows and show alias targets and command-type chips; modules group repeated names and show versions, shortened paths and exported command counts; history shows id, start time, duration, status and highlighted command text. Windows network views register only on Windows; ping and .NET DNS/IP views work across platforms. It works by defining an Out-Default function for the session that hands objects to Out-ConsoleDefault; objects stay the same, pipelines into other commands, Format-* and Out-String keep PowerShell's behaviour, and redirected output, transcripts' plain text, jobs and remoting are not affected. Disable-ConsoleFormatting removes the function. FormatCommands separately opts Format-Table, Format-List and Format-Wide into host-only rendering; it is excluded from All. It preserves native column selection and labels. Explicit properties, calculated expressions, grouping and named views take precedence over built-in presentation defaults. A custom Out-Default is kept unless you pass -Force for a feature that uses it. Streams, ReadLine, GridView and Prompt do not replace Out-Default.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting -Feature Output, FormatCommands; 'synthetic' | Get-Member -Name Length, Substring; Get-Command Get-Date | Format-Table Name, Source
```

Inspect members with the built-in view, while keeping an explicit native column selection

### EXAMPLE 2
```powershell
Enable-ConsoleFormatting -Feature GridView; Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
```

Use the terminal grid where Out-GridView is unavailable; working native commands stay unchanged

### EXAMPLE 3
```powershell
Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10
```

Turn it on for the session

### EXAMPLE 4
```powershell
Set-ConsoleTheme -Theme Dark; Enable-ConsoleFormatting -Feature Prompt
```

Use a minimal themed shell prompt, separately from default output formatting

### EXAMPLE 5
```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

The lines to keep it on in every session (printed, never written to the profile)

### EXAMPLE 6
```powershell
Enable-ConsoleFormatting -Feature All
```

Default output, the error view, native stream/progress and ReadLine colours

### EXAMPLE 7
```powershell
Set-ConsoleTheme Light; Enable-ConsoleFormatting -Feature Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose
```

Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour

### EXAMPLE 8
```powershell
Set-ConsoleTheme Dark; Enable-ConsoleFormatting -Feature ReadLine, Streams; Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42; Write-Progress -Activity 'Backup' -Completed
```

Theme the already loaded editor and native progress without replacing commands

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
What to turn on: Output (the default: registered and built-in types as ConsoleMonster views), ErrorView (errors that reach the console in ConsoleMonster's error view, following $ErrorView: DetailedView adds the stack, CategoryView keeps PowerShell's), Progress (Write-Progress drawn as ConsoleMonster's progress area; Write-Progress uses a reversible native-parameter function proxy), Streams (native stream and progress colours), ReadLine (syntax, selection and available inline prediction colours for already loaded PSReadLine), or All (Output, ErrorView, Streams and ReadLine). Streams sets PSStyle.Formatting Warning, Verbose, Debug and Error on PowerShell 7.2+, and PSStyle.Progress.Style, or available Host.PrivateData warning, verbose, debug and progress foreground/background colours on older hosts. Native progress keeps its View and layout. ReadLine never imports PSReadLine and does nothing when it is absent; unsupported colour properties are skipped. PowerShell keeps its prefixes, records, preferences, redirection and transcript behaviour. Streams and ReadLine never replace Write-* commands or install an Out-Default function. Progress is the separate native-progress rendering feature, excluded from All; All keeps Write-Progress native and styles it through Streams. Re-enable Streams or ReadLine after changing themes; Shared native properties follow the most recently applied installation. Disabling its owner restores the latest surviving installation; the last owner restores the first original values, including later user edits. Use Show-ConsoleStatusLine or Write-ConsoleLogEntry for rich script messages. FormatCommands explicitly opts Format-Table, Format-List and Format-Wide into direct-host-only rendering, preserving native captures, jobs, remoting and transcripts; it is excluded from All. Help and Errors are separate direct-host opt-ins, excluded from All. Help renders native Get-Help, help and -? with paging. Errors uses the compact view; -Detailed includes stacks. GridView supplies the terminal grid only where Out-GridView is unavailable; a working native command is kept. Prompt adds a minimal shell prompt while preserving native fallback; oh-my-posh and Starship need Force. Features add up across calls; Disable-ConsoleFormatting turns them off.

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

### -Force
Replaces an existing Out-Default function, Write-Progress alias, or an Out-GridView function/alias when the native grid is unavailable (Disable-ConsoleFormatting puts it back). A working native Out-GridView is always kept. Prompt requires Force to replace oh-my-posh or Starship.

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
Only takes over the types registered with Register-ConsoleView, not the built-in ones (files, processes, services, dictionaries). The built-in views still pick the columns of Show-ConsoleTable and the other commands.

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
