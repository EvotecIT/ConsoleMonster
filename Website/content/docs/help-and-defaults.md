---
title: Help, errors and progress
description: Show-ConsoleHelp renders PowerShell's own help; Enable-ConsoleFormatting adds an error view and a progress area for the session, each opt-in.
slug: help-and-defaults
order: 8
---

These features change how PowerShell's own output looks for the session, never what commands return. Each is opt-in and undone by `Disable-ConsoleFormatting`.

```powershell
Enable-ConsoleFormatting                     # default output for files, processes, services, dictionaries...
Enable-ConsoleFormatting -Feature ErrorView  # errors that reach the console
Enable-ConsoleFormatting -Feature Progress   # Write-Progress drawn by ConsoleMonster
Enable-ConsoleFormatting -Feature All
Enable-ConsoleFormatting -ProfileSnippet     # prints the lines to keep it on in every session
```

{{< cm-demo id="help" heading="h2" >}}

`Show-ConsoleHelp` reads the help PowerShell has for a command (`Get-Help -Full`): cmdlet help files, comment-based help of functions and scripts, or the syntax PowerShell generates when there is none. It shows the synopsis, the syntax of each parameter set with optional parts told apart, the description, a parameter table, highlighted examples, inputs, outputs and links. Piped to `more`, `Out-Host -Paging` or a file, it writes plain lines.

{{< cm-demo id="errors" heading="h2" >}}

With `-Feature ErrorView`, errors that reach the console use the same view as `Format-ConsoleError`: the exception type, the message, the category, the lines of the script around the failing statement, and the call stack with framework frames folded. It follows `$ErrorView`: `DetailedView` adds the stack, and `CategoryView` keeps PowerShell's own view.

{{< cm-demo id="progress" heading="h2" >}}

With `-Feature Progress`, `Write-Progress` becomes an alias of `Write-ConsoleProgress` for the session, so existing loops get ConsoleMonster's progress area: one row per activity below the cursor, nested with `-ParentId`, with a bar, the percentage, the status and the elapsed time. Output written in between appears above the area, and the area disappears when every activity completes. `$ProgressPreference` is honoured. Progress written by compiled cmdlets such as `Copy-Item` is still drawn by PowerShell.