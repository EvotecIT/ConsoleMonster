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
   Enable-ConsoleFormatting -Feature All        # all four
   Enable-ConsoleFormatting -Feature FormatCommands  # Format-Table, Format-List and Format-Wide too (never part of All)
   ```

2. To keep it on in every session, add the lines `-ProfileSnippet` prints to your profile. It only prints them; this appends them to `$PROFILE`, creating the file when there is none:

   ```powershell
   if (-not (Test-Path -Path $PROFILE)) { $null = New-Item -ItemType File -Path $PROFILE -Force }
   Enable-ConsoleFormatting -Feature All -ProfileSnippet | Add-Content -Path $PROFILE
   ```

3. Open a new session. `Disable-ConsoleFormatting` turns it off again for a session; remove the lines from `$PROFILE` to stop it for good.

{{< cm-demo id="help" heading="h2" >}}

`Show-ConsoleHelp` reads the help PowerShell has for a command (`Get-Help -Full`): cmdlet help files, comment-based help of functions and scripts, or the syntax PowerShell generates when there is none. It shows the synopsis, the syntax of each parameter set with optional parts told apart, the description, a parameter table, highlighted examples, inputs, outputs and links. Piped to `more`, `Out-Host -Paging` or a file, it writes plain lines.

{{< cm-demo id="errors" heading="h2" >}}

With `-Feature ErrorView`, errors that reach the console use the same view as `Format-ConsoleError`: the exception type, the message, the category, the lines of the script around the failing statement, and the call stack with framework frames folded. It follows `$ErrorView`: `DetailedView` adds the stack, and `CategoryView` keeps PowerShell's own view.

{{< cm-demo id="progress" heading="h2" >}}

With `-Feature Progress`, `Write-Progress` becomes an alias of `Write-ConsoleProgress` for the session, so existing loops get ConsoleMonster's progress area: one row per activity below the cursor, nested with `-ParentId`, with a bar, the percentage, the status and the elapsed time. Output written in between appears above the area, and the area disappears when every activity completes. `$ProgressPreference` is honoured. Progress written by compiled cmdlets such as `Copy-Item` is still drawn by PowerShell.

## Warning, verbose and debug colours

With `-Feature Streams`, PowerShell's own warning, verbose and debug messages (and errors on PowerShell 7.2 and later) take the active theme's colours, so they match the rest of the output and stay readable on a light background. Choose the theme first, or enable `Streams` again after `Set-ConsoleTheme`.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="The same script on Windows PowerShell 5.1 and PowerShell 7; switch the site's theme to see the light-theme run." >}}

Only the colours change. `Write-Warning`, `Write-Verbose` and `Write-Debug` stay PowerShell's commands, with their `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes, preferences, redirection and transcripts. PowerShell 7 sets `$PSStyle.Formatting`; Windows PowerShell 5.1 sets the host's `$Host.PrivateData` colours, and hosts without them (remoting, headless) are left alone. `Disable-ConsoleFormatting -Feature Streams` puts back exactly the values it changed. For rich messages in your own scripts, with status symbols and markup, use [`Write-ConsoleStatus`](/powershell/commands/write-consolestatus/) or [`Write-ConsoleLog`](/powershell/commands/write-consolelog/).