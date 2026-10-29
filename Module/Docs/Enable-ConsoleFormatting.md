---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Enable-ConsoleFormatting
## SYNOPSIS
Enables the everyday console views, native format commands and theme colours.

Three ways to use ConsoleMonster output: Automatic: enable formatting, then use ordinary commands for known and registered types. Explicit: pipe any objects to Show-ConsoleTable to choose columns, a title and grouping, or to Show-ConsoleObject to explore their members. These commands do not change session formatting; Show-ConsoleObject -StaticSnapshot avoids interactive input in scripts. Native Format commands: FormatCommands styles direct console output while keeping PowerShell's column choices; it is included by default; use -Exclude FormatCommands to leave it native. You do not need both automatic and explicit formatting. The -Feature parameter requires ConsoleMonster 2.1.0 or later.

By default, Output, FormatCommands, Errors, Streams and ReadLine are enabled. Format-Table, Format-List and Format-Wide keep PowerShell's column choices and change only direct console output; pipelines, captures, files, jobs, remoting and transcripts remain native. Use Include to add opt-in features and Exclude to remove features from the default set. Feature replaces the default set with exactly the selected features. Prompt, Help, Progress and GridView require Include or Feature. CompactErrors opts into the compact error layout. Get-ConsoleFormatting reports what is enabled. Disable-ConsoleFormatting restores the session.

Files, processes, services, structured records, commands, modules, history, IP addresses, volumes, versions, dictionaries and registered types get built-in or registered views. Explicit Show-Console commands remain available when you want to choose the view yourself. Custom Out-Default functions are kept unless Force is specified; Prompt does not replace oh-my-posh or Starship without Force.

## SYNTAX
### Default (Default)
```powershell
Enable-ConsoleFormatting [-Include <ConsoleFormattingFeature[]>] [-Exclude <ConsoleFormattingFeature[]>] [-CompactErrors] [-Force] [-NoBuiltIn] [-ProfileSnippet] [-Detailed] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Feature
```powershell
Enable-ConsoleFormatting -Feature <ConsoleFormattingFeature[]> [-CompactErrors] [-Force] [-NoBuiltIn] [-ProfileSnippet] [-Detailed] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Enables the everyday console views, native format commands and theme colours.

Three ways to use ConsoleMonster output: Automatic: enable formatting, then use ordinary commands for known and registered types. Explicit: pipe any objects to Show-ConsoleTable to choose columns, a title and grouping, or to Show-ConsoleObject to explore their members. These commands do not change session formatting; Show-ConsoleObject -StaticSnapshot avoids interactive input in scripts. Native Format commands: FormatCommands styles direct console output while keeping PowerShell's column choices; it is included by default; use -Exclude FormatCommands to leave it native. You do not need both automatic and explicit formatting. The -Feature parameter requires ConsoleMonster 2.1.0 or later.

By default, Output, FormatCommands, Errors, Streams and ReadLine are enabled. Format-Table, Format-List and Format-Wide keep PowerShell's column choices and change only direct console output; pipelines, captures, files, jobs, remoting and transcripts remain native. Use Include to add opt-in features and Exclude to remove features from the default set. Feature replaces the default set with exactly the selected features. Prompt, Help, Progress and GridView require Include or Feature. CompactErrors opts into the compact error layout. Get-ConsoleFormatting reports what is enabled. Disable-ConsoleFormatting restores the session.

Files, processes, services, structured records, commands, modules, history, IP addresses, volumes, versions, dictionaries and registered types get built-in or registered views. Explicit Show-Console commands remain available when you want to choose the view yourself. Custom Out-Default functions are kept unless Force is specified; Prompt does not replace oh-my-posh or Starship without Force.

## EXAMPLES

### EXAMPLE 1
```powershell
Enable-ConsoleFormatting; 'synthetic' | Get-Member -Name Length, Substring; Get-Command Get-Date | Format-Table Name, Source
```

Inspect members with the built-in view, while keeping an explicit native column selection

### EXAMPLE 2
```powershell
Enable-ConsoleFormatting -Include GridView; Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
```

Use the terminal grid where Out-GridView is unavailable; working native commands stay unchanged

### EXAMPLE 3
```powershell
Enable-ConsoleFormatting; Get-ChildItem; Get-Process | Select-Object -First 10
```

Turn it on for the session

### EXAMPLE 4
```powershell
Set-ConsoleTheme -Theme Dark; Enable-ConsoleFormatting -Include Prompt
```

Add a minimal themed shell prompt to the default formatting

### EXAMPLE 5
```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

The lines to keep it on in every session (printed, never written to the profile)

### EXAMPLE 6
```powershell
Enable-ConsoleFormatting -Exclude FormatCommands
```

Exclude native format command styling from the default set

### EXAMPLE 7
```powershell
Set-ConsoleTheme Light; Enable-ConsoleFormatting -Include Streams; Write-Warning 'Disk space is low'; Write-Verbose 'Checking backup' -Verbose
```

Use the active theme for native stream messages, keeping PowerShell's prefixes and stream behaviour

### EXAMPLE 8
```powershell
Set-ConsoleTheme Dark; Enable-ConsoleFormatting -Include ReadLine, Streams; Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42; Write-Progress -Activity 'Backup' -Completed
```

Theme the already loaded editor and native progress without replacing commands

## PARAMETERS

### -CompactErrors
Uses the compact message-first error layout. Errors must be enabled.

```yaml
Type: SwitchParameter
Parameter Sets: Default, Feature
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detailed
Includes script and .NET stack frames in the compact error layout. Requires CompactErrors.

```yaml
Type: SwitchParameter
Parameter Sets: Default, Feature
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exclude
Turns on the default set except these features, and disables any excluded feature already enabled. Cannot be combined with Feature.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: Default
Aliases: None
Possible values: None, Output, Errors, Progress, FormatCommands, Streams, Help, ReadLine, GridView, Prompt

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Feature
Choose only these features: Output, FormatCommands, Errors, Streams, ReadLine, Prompt, Help, Progress or GridView. The default without Feature enables Output, FormatCommands, Errors, Streams and ReadLine. Feature replaces the default set with exactly these features; use Include for defaults plus opt-ins. Use Disable-ConsoleFormatting to restore the session. FormatCommands keeps native column selections and changes only direct host output. Streams and ReadLine change native colours without replacing Write-* commands. Progress replaces Write-Progress only when explicitly selected. Help, GridView and Prompt are explicit opt-ins.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: Feature
Aliases: None
Possible values: None, Output, Errors, Progress, FormatCommands, Streams, Help, ReadLine, GridView, Prompt

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force
Replaces an existing Out-Default function, Write-Progress alias, or an Out-GridView function/alias when the native grid is unavailable (Disable-ConsoleFormatting puts it back). A working native Out-GridView is always kept. Prompt requires Force to replace oh-my-posh or Starship.

```yaml
Type: SwitchParameter
Parameter Sets: Default, Feature
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Include
Adds these opt-in features to the default set. Can be combined with Exclude, but not Feature.

```yaml
Type: ConsoleFormattingFeature[]
Parameter Sets: Default
Aliases: None
Possible values: None, Output, Errors, Progress, FormatCommands, Streams, Help, ReadLine, GridView, Prompt

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
Parameter Sets: Default, Feature
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
Parameter Sets: Default, Feature
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
