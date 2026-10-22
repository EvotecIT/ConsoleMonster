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
   Enable-ConsoleFormatting -Feature Prompt     # minimal shell prompt (never part of All)
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

{{< cm-demo id="errors" heading="h2" >}}

With `-Feature ErrorView`, errors that reach the console use the same view as `Format-ConsoleError`: the exception type, the message, the category, the lines of the script around the failing statement, and the call stack with framework frames folded. It follows `$ErrorView`: `DetailedView` adds the stack, and `CategoryView` keeps PowerShell's own view.

{{< cm-demo id="progress" heading="h2" >}}

With `-Feature Progress`, `Write-Progress` becomes an alias of `Write-ConsoleProgress` for the session, so existing loops get ConsoleMonster's progress area: one row per activity below the cursor, nested with `-ParentId`, with a bar, the percentage, the status and the elapsed time. Output written in between appears above the area, and the area disappears when every activity completes. `$ProgressPreference` is honoured. Progress written by compiled cmdlets such as `Copy-Item` is still drawn by PowerShell.

## Warning, verbose and debug colours

With `-Feature Streams`, PowerShell's own warning, verbose and debug messages (and errors on PowerShell 7.2 and later) take the active theme's colours, so they match the rest of the output and stay readable on a light background. Choose the theme first, or enable `Streams` again after `Set-ConsoleTheme`.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="The same script on Windows PowerShell 5.1 and PowerShell 7; switch the site's theme to see the light-theme run." >}}

Only the colours change. `Write-Warning`, `Write-Verbose` and `Write-Debug` stay PowerShell's commands, with their `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes, preferences, redirection and transcripts. PowerShell 7.2 and later set `$PSStyle.Formatting`; Windows PowerShell 5.1 and PowerShell 7.0 and 7.1 set the host's `$Host.PrivateData` colours, and hosts without them (remoting, headless) are left alone. `Disable-ConsoleFormatting -Feature Streams` puts back exactly the values it changed. For rich messages in your own scripts, with status symbols and markup, use [`Write-ConsoleStatus`](/powershell/commands/write-consolestatus/) or [`Write-ConsoleLog`](/powershell/commands/write-consolelog/).

## Minimal shell prompt

Opt in with `Prompt`; it is excluded from `All` and does not enable default output formatting:

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Name Dark  # use Light for a light terminal background
Enable-ConsoleFormatting -Feature Prompt
Enable-ConsoleFormatting -Feature Prompt -ProfileSnippet  # prints profile lines
```

The prompt shows the shortened path, git branch with `*` for index or working-tree changes, and a success or failure mark. A completed command taking **more than two seconds** also shows its duration, using PowerShell's own history timestamps. An old nonzero native exit code does not mark a later successful PowerShell command as failed. PSReadLine handlers and key bindings stay as they are.

Git is optional: one asynchronous probe runs per session, with a 100 ms child-process budget and a five-second cache. The prompt never waits for it. The first prompt can omit git until the next prompt; changed state can take five seconds to appear. Missing git, an unavailable repository or an expired probe omits that segment. Submodule contents are not scanned.

An existing oh-my-posh or Starship prompt is kept. Use `Enable-ConsoleFormatting -Feature Prompt -Force` to replace it explicitly. Other custom prompts are backed up, including their closure scope. `Disable-ConsoleFormatting -Feature Prompt`, disabling all enabled features without a feature argument, or `Remove-Module ConsoleMonster` restores the original. A prompt you install later is kept. `Disable-ConsoleFormatting -Feature All` leaves this explicit opt-in enabled.

Only the top-level local console prompt is themed. Assigned, piped, redirected, transcribed, recorded, job, remote and nested prompt calls use the original prompt. Native output, error records, preferences, `$?` and `$LASTEXITCODE` retain their behavior. Engines whose status or transcription state cannot be checked keep native output.

### The same renderer in .NET

The following frames use fixed illustrative path, branch and completed-command data. This complete C# program calls the renderer used by the PowerShell prompt; the PowerShell feature collects the live session data.

```bash
dotnet new console -n PromptExample
cd PromptExample
dotnet add package ConsoleMonster
# Replace Program.cs with the complete program below, then run it.
dotnet run
```

{{< cm-frame ids="shell-prompt-120,shell-prompt-60" caption="Fixed illustrative data: success first, then a failed 3.4-second command. Switch themes to compare light and dark." >}}

Next: [choose a theme](/docs/themes/) or [format everyday PowerShell output](/guides/everyday-powershell/).