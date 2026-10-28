---
title: Help, errors and progress
description: Show PowerShell help, errors, progress and native stream colours with ConsoleMonster.
slug: help-and-defaults
order: 10
group: apps
card: help
apis: [Show-ConsoleHelp]
chips: [Errors, Progress]
summary: "Show-ConsoleHelp, the error view and the progress area, with optional help and progress."
---

These features change how PowerShell's own output looks for the session, never what commands return. `Disable-ConsoleFormatting` restores the session.

`Enable-ConsoleFormatting` enables Output, FormatCommands, Errors, Streams and ReadLine. Explicit `Format-Table`, `Format-List` and `Format-Wide` retain PowerShell's column choices and only change output sent straight to the console. Use `-Include Help` to add an opt-in and `-Exclude FormatCommands` to keep those commands native. Advanced: `-Feature` replaces the defaults with exactly the selected features. For an explicit view, use `Show-ConsoleTable` or `Show-ConsoleObject`.

1. Choose the default set or individual features:

   ```powershell
   Import-Module ConsoleMonster
   Enable-ConsoleFormatting                            # everyday defaults
   Get-ConsoleFormatting                               # see what is enabled
   Enable-ConsoleFormatting -Exclude FormatCommands    # keep native Format-* commands
   Enable-ConsoleFormatting -Feature Streams, ReadLine # minimal selection
   Enable-ConsoleFormatting -CompactErrors             # opt in to compact errors
   Enable-ConsoleFormatting -Include Help              # opt in to styled help
   Enable-ConsoleFormatting -Include Progress          # opt in to the progress adapter
   Enable-ConsoleFormatting -Include Prompt            # opt in to the prompt
   ```

2. To keep it on in every session, add the lines `-ProfileSnippet` prints to your profile. It only prints them; this appends them to `$PROFILE`, creating the file when there is none:

   ```powershell
   if (-not (Test-Path -Path $PROFILE)) {
       $null = New-Item -ItemType File -Path $PROFILE -Force
   }
   Enable-ConsoleFormatting -ProfileSnippet | Add-Content -Path $PROFILE
   ```

3. Open a new session. `Disable-ConsoleFormatting` turns it off again for a session; remove the lines from `$PROFILE` to stop it for good.

{{< cm-demo id="help" heading="h2" >}}

`Show-ConsoleHelp` reads the help PowerShell has for a command (`Get-Help -Full`): cmdlet help files, comment-based help of functions and scripts, or the syntax PowerShell generates when there is none. It shows the synopsis, the syntax of each parameter set with optional parts told apart, the description, a parameter table, highlighted examples, inputs, outputs and links. Piped to `more`, `Out-Host -Paging` or a file, it writes plain lines.

### Use the view for native help

```powershell
Enable-ConsoleFormatting -Include Help
Get-Help Get-Process -Full
Get-Process -?
help Get-Process -Examples
Disable-ConsoleFormatting -Feature Help
```

This opt-in feature uses the same view when native help goes straight to the console. `help` keeps paging, with
**Space next · q quit** in the footer. `-Full`, `-Detailed`, `-Examples` and `-Parameter` select the corresponding
sections; `-Online` and `-ShowWindow` keep PowerShell's behavior. About topics use the
[Markdown reader](/docs/markdown/), while captured output retains PowerShell's original text. Help is not enabled by default.

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
Enable-ConsoleFormatting -CompactErrors
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
Enable-ConsoleFormatting -CompactErrors -Detailed  # include stacks and causes
Disable-ConsoleFormatting -Feature Errors

# The same view on demand, including inside a catch block or an export.
try { [int]::Parse('production') } catch { $_ | Show-Console -Compact -View Error }
```

Compact errors are optional. Error-producing cmdlets remain native, and `$Error`, `-ErrorVariable`, `2>`,
`try`/`catch`, `$?` and `$ErrorActionPreference` keep their PowerShell behavior. Piped, assigned, redirected, job,
remoting, CI and transcript output retains native rendering. `$ErrorView` stays unchanged; `CategoryView` and
`Get-Error` retain their native views. Bare `Disable-ConsoleFormatting` removes this opt-in along with the others.

{{< cm-demo id="errors" heading="h2" >}}

With the default `Errors` feature, errors that reach the console use the same view as `Show-Console -View Error`: the exception type, the message, the category, the lines of the script around the failing statement, and the call stack with framework frames folded. It follows `$ErrorView`: `DetailedView` adds the stack, and `CategoryView` keeps PowerShell's own view.

{{< cm-demo id="progress" heading="h2" >}}

The default keeps `Write-Progress` native and styles it through `Streams`, preserving its View and restoring exact owned colors on disable. With `-Include Progress`, `Write-Progress` uses the native-metadata ConsoleMonster function adapter for the session, so existing loops get ConsoleMonster's progress area: one row per activity below the cursor, nested with `-ParentId`, with a bar, the percentage, the status and the elapsed time. Output written in between appears above the area, and the area disappears when every activity completes. `$ProgressPreference` is honoured. Progress written by compiled cmdlets such as `Copy-Item` is still drawn by PowerShell.

## Warning, verbose and debug colours

With the default `Streams` feature, PowerShell's own warning, verbose and debug messages (and errors on PowerShell 7.2 and later) take the active theme's colours, so they match the rest of the output and stay readable on a light background. Choose the theme first, or enable `Streams` again after `Set-ConsoleTheme`.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="The same script on Windows PowerShell 5.1 and PowerShell 7; switch the site's theme to see the light-theme run." >}}

Only the colours change. `Write-Warning`, `Write-Verbose` and `Write-Debug` stay PowerShell's commands, with their `WARNING:`, `VERBOSE:` and `DEBUG:` prefixes, preferences, redirection and transcripts. PowerShell 7.2 and later set `$PSStyle.Formatting`; Windows PowerShell 5.1 and PowerShell 7.0 and 7.1 set the host's `$Host.PrivateData` colours, and hosts without them (remoting, headless) are left alone. `Disable-ConsoleFormatting -Feature Streams` puts back exactly the values it changed. For rich messages in your own scripts, with status symbols and markup, use [`Show-ConsoleStatusLine`](/powershell/commands/show-consolestatusline/) or [`Write-ConsoleLogEntry`](/powershell/commands/write-consolelogentry/).

## Minimal shell prompt

Add `Prompt` to the default output formatting:

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Theme Dark  # use Light for a light terminal background
Enable-ConsoleFormatting -Include Prompt
Enable-ConsoleFormatting -Include Prompt -ProfileSnippet  # prints profile lines
```

The prompt shows the shortened path, git branch with `*` for index or working-tree changes, and a success or failure mark. A completed command taking **more than two seconds** also shows its duration, using PowerShell's own history timestamps. An old nonzero native exit code does not mark a later successful PowerShell command as failed. PSReadLine handlers and key bindings stay as they are.

Git is optional: one asynchronous probe runs per session, with a 100 ms child-process budget and a five-second cache. The prompt never waits for it. The first prompt can omit git until the next prompt; changed state can take five seconds to appear. Missing git, an unavailable repository or an expired probe omits that segment. Submodule contents are not scanned.

An existing oh-my-posh or Starship prompt is kept. Use `Enable-ConsoleFormatting -Include Prompt -Force` to replace it explicitly. Other custom prompts are backed up, including their closure scope. `Disable-ConsoleFormatting -Feature Prompt`, disabling all enabled features without a feature argument, or `Remove-Module ConsoleMonster` restores the original. A prompt you install later is kept.

Only the top-level local console prompt is themed. Assigned, piped, redirected, transcribed, recorded, job, remote and nested prompt calls use the original prompt. Native output, error records, preferences, `$?` and `$LASTEXITCODE` retain their behavior. Engines whose status or transcription state cannot be checked keep native output.

### The same renderer in .NET

This complete C# program calls the renderer the PowerShell prompt uses, with a fixed illustrative path, branch and finished commands; the PowerShell feature collects the live session's data instead.

{{< cm-try key="prompt" ids="shell-prompt-120,shell-prompt-60" project="PromptExample" caption="Success first, then a failed 3.4-second command, at 120 and 60 columns. Switch the site theme to compare light and dark." >}}

## Command-line help for .NET hosts

In PowerShell, use the native command surface first:

```powershell
Show-ConsoleHelp Get-ChildItem
```

A .NET command-line host can project its parser metadata into `HelpTopic.CommandLine` and render it with `HelpView`.
ConsoleMonster owns presentation; the host keeps argument parsing, command selection and execution. No parser package
is required by ConsoleMonster.

`HelpCommand` describes commands and optional groups. `HelpValue` describes literal value IDs, descriptions and optional
muted notes. `HelpSyntaxParameter.Named = false` and `HelpParameter.Named = false` describe positional arguments.
The existing parameter convention omits one renderer-added dash: use `Name = "-output"` for `--output`, while aliases
such as `-o` are literal. Labels follow the render profile's language; set `ValuesHeading` only for a custom heading.

{{< cm-frame ids="cli-help-120,cli-help-60" caption="The complete .NET example compiles against ConsoleMonster and renders at 120 and 60 columns in both themes." >}}

## Related

[Choose a theme](/docs/themes/) or [format everyday PowerShell output](/guides/everyday-powershell/).
