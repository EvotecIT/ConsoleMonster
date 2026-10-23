---
title: Help, errors and progress
description: Show-ConsoleHelp renders PowerShell's own help; Enable-ConsoleFormatting adds an error view, a progress area and themed stream colours, each opt-in.
slug: help-and-defaults
order: 10
group: apps
card: help
apis: [Show-ConsoleHelp]
chips: [Errors, Progress]
summary: "Show-ConsoleHelp, the error view and the progress area, each opt-in."
---

These features change how PowerShell's own output looks for the session, never what commands return. Each is opt-in and undone by `Disable-ConsoleFormatting`.

> The default output of `Enable-ConsoleFormatting` changes only objects that reach the console without a format command: `Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless `-Feature FormatCommands` is on. That one is never part of `All`; see [Everyday PowerShell output](/guides/everyday-powershell/#format-table-format-list-and-format-wide). For ConsoleMonster's views on demand, use `Format-ConsoleTable` or `Format-ConsoleList`.

1. Turn it on for this session, choosing the features you want:

   ```powershell
   Import-Module ConsoleMonster
   Enable-ConsoleFormatting                     # default output for files, processes, services, dictionaries...
   Enable-ConsoleFormatting -Feature ErrorView  # errors that reach the console
   Enable-ConsoleFormatting -Feature Progress   # Write-Progress drawn by ConsoleMonster
   Enable-ConsoleFormatting -Feature Streams    # warning, verbose and debug messages in the theme's colours
   Enable-ConsoleFormatting -Feature ReadLine   # colors for the already loaded command-line editor
   Enable-ConsoleFormatting -Feature All        # output, errors, native stream/progress colors and ReadLine
   Enable-ConsoleFormatting -Feature FormatCommands  # Format-Table, Format-List and Format-Wide too (never part of All)
   Enable-ConsoleFormatting -Feature Errors      # compact host errors (never part of All)
   ```

2. To keep it on in every session, add the lines `-ProfileSnippet` prints to your profile. It only prints them; this appends them to `$PROFILE`, creating the file when there is none:

   ```powershell
   if (-not (Test-Path -Path $PROFILE)) {
       $null = New-Item -ItemType File -Path $PROFILE -Force
   }
   Enable-ConsoleFormatting -Feature All -ProfileSnippet | Add-Content -Path $PROFILE
   ```

3. Open a new session. `Disable-ConsoleFormatting` turns it off again for a session; remove the lines from `$PROFILE` to stop it for good.

{{< cm-demo id="help" heading="h2" >}}

`Show-ConsoleHelp` reads the help PowerShell has for a command (`Get-Help -Full`): cmdlet help files, comment-based help of functions and scripts, or the syntax PowerShell generates when there is none. It shows the synopsis, the syntax of each parameter set with optional parts told apart, the description, a parameter table, highlighted examples, inputs, outputs and links. Piped to `more`, `Out-Host -Paging` or a file, it writes plain lines.

### Use the view for native help

```powershell
Enable-ConsoleFormatting -Feature Help
Get-Help Get-Process -Full
Get-Process -?
help Get-Process -Examples
Disable-ConsoleFormatting -Feature Help
```

This opt-in feature uses the same view when native help goes straight to the console. `help` keeps paging, with
**Space next · q quit** in the footer. `-Full`, `-Detailed`, `-Examples` and `-Parameter` select the corresponding
sections; `-Online` and `-ShowWindow` keep PowerShell's behavior. About topics stay literal until Markdown support
ships. Help is excluded from `-Feature All`.

Pipelines, assignments, output variables, redirection, jobs, remoting, CI and transcripts keep PowerShell's own help
objects and bytes. For example, `Get-Help Get-Process | Out-String` remains native while the feature is enabled.
Disabling restores the previous help and output adapters. The
[contract suite](https://github.com/EvotecIT/ConsoleMonster/blob/master/Module/Tests/ConsoleMonster.HelpFormatting.Tests.ps1)
records the proof for Windows PowerShell 5.1 and PowerShell 7.

{{< cm-frame ids="help-formatting-native-ps7-120,help-formatting-get-help-ps7-120,help-formatting-get-help-ps7-60" caption="Native help before and after the opt-in, captured on PowerShell 7 at 120 and 60 columns." >}}

{{< cm-frame ids="help-formatting-help-ps7-120,help-formatting-help-ps7-60" caption="The first page of help, with the Space/q footer, at both widths." >}}
## Compact everyday errors

Enable the compact layout explicitly:

```powershell
Enable-ConsoleFormatting -Feature Errors
```

Subsequent commands typed at the prompt use the new layout when they fail. This one deliberately produces an error:

```powershell
[int]::Parse('production')
```

The message comes first, with only its label in the error tone. The original invocation is highlighted, a caret marks
the failing span, and the script, line and column come from `InvocationInfo`. Category and target stay visible. Inner
exceptions fold into a muted count, and PowerShell 7 adds a `Get-Error for details` hint.

{{< cm-frame ids="errors-120,errors-60" caption="The shared compact view at 120 and 60 columns. Its complete C# program compiles to produce these frames; switch the site theme for the light rendering." >}}

```powershell
Enable-ConsoleFormatting -Feature Errors -Detailed  # include stacks and causes
Disable-ConsoleFormatting -Feature Errors

# The same view on demand, including inside a catch block or an export.
try { [int]::Parse('production') } catch { $_ | Format-ConsoleError -Compact }
```

`Errors` is excluded from `All`. Error-producing cmdlets remain native, and `$Error`, `-ErrorVariable`, `2>`,
`try`/`catch`, `$?` and `$ErrorActionPreference` keep their PowerShell behavior. Piped, assigned, redirected, job,
remoting, CI and transcript output retains native rendering. `$ErrorView` stays unchanged; `CategoryView` and
`Get-Error` retain their native views. Bare `Disable-ConsoleFormatting` removes this opt-in along with the others.

{{< cm-demo id="errors" heading="h2" >}}

With `-Feature ErrorView`, errors that reach the console use the same view as `Format-ConsoleError`: the exception type, the message, the category, the lines of the script around the failing statement, and the call stack with framework frames folded. It follows `$ErrorView`: `DetailedView` adds the stack, and `CategoryView` keeps PowerShell's own view.

{{< cm-demo id="progress" heading="h2" >}}

The legacy `Progress` feature is excluded from `All`: All keeps `Write-Progress` native and styles it through `Streams`, preserving its View and restoring exact owned colors on disable. With an explicit `-Feature Progress`, `Write-Progress` becomes an alias of `Write-ConsoleProgress` for the session, so existing loops get ConsoleMonster's progress area: one row per activity below the cursor, nested with `-ParentId`, with a bar, the percentage, the status and the elapsed time. Output written in between appears above the area, and the area disappears when every activity completes. `$ProgressPreference` is honoured. Progress written by compiled cmdlets such as `Copy-Item` is still drawn by PowerShell.

## Warning, verbose and debug colours

With `-Feature Streams`, PowerShell's own warning, verbose and debug messages (and errors on PowerShell 7.2 and later) take the active theme's colours, so they match the rest of the output and stay readable on a light background. Choose the theme first, or enable `Streams` again after `Set-ConsoleTheme`.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="The same script on Windows PowerShell 5.1 and PowerShell 7; switch the site's theme to see the light-theme run." >}}

Only the colours change. `Write-Warning`, `Write-Verbose` and `Write-Debug` stay PowerShell's commands, with their `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes, preferences, redirection and transcripts. PowerShell 7.2 and later set `$PSStyle.Formatting`; Windows PowerShell 5.1 and PowerShell 7.0 and 7.1 set the host's `$Host.PrivateData` colours, and hosts without them (remoting, headless) are left alone. `Disable-ConsoleFormatting -Feature Streams` puts back exactly the values it changed. For rich messages in your own scripts, with status symbols and markup, use [`Write-ConsoleStatus`](/powershell/commands/write-consolestatus/) or [`Write-ConsoleLog`](/powershell/commands/write-consolelog/).
