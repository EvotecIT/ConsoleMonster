---
title: Everyday PowerShell output
description: "Enable-ConsoleFormatting makes ordinary output readable for the session: files, processes, hashtables, arrays and groups, the Format commands on request, module views and Markdown reports, before and after."
social_card_image: /site/cards/formatting.svg
meta.social_title: "Everyday PowerShell output"
slug: everyday-powershell
order: 1
---

You do not have to change your commands to get readable output. One line turns it on for the session:

```powershell
Enable-ConsoleFormatting
```

Files and folders, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries are then shown as ConsoleMonster tables and records when they reach the console. Objects stay the same: pipelines into other commands, `Out-String`, redirected output, transcripts, jobs and remoting keep PowerShell's behaviour. `Disable-ConsoleFormatting` turns it off.

> `Enable-ConsoleFormatting` changes only output that reaches the console without a format command: `Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless you also turn on `-Feature FormatCommands` (below). For ConsoleMonster's views on demand, use `Format-ConsoleTable` or `Format-ConsoleList`.

To keep it on in every session, print the lines for your profile and paste them in (nothing is written for you):

```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

{{< cm-demo id="formatting" heading="h2" autoplay="true" >}}

{{< cm-demo id="shaping" heading="h2" >}}

Collections in a cell are joined (`DNS, GC, PDC`), with a `+N` marker for the items that do not fit, where Format-Table shows `{DNS, GC, PDC}` and cuts long lists short. Durations read as `42 s` or `4 h 00 m`, an empty collection is a dash, and status words get their symbol and colour. `-GroupBy` writes a labelled heading per group, with its count.

## Format-Table, Format-List and Format-Wide

Scripts call the three Format commands too, so they get their own switch, separate from `-Feature All`. With it on, the commands you already type draw ConsoleMonster views, with their parameters (`-Property`, `-GroupBy`, `-AutoSize`, `-Wrap`, `-View`, `-Column`) mapped. Only output that goes straight to the console changes: `$x = ... | Format-Table`, `| Out-String`, `| Out-File`, `>`, jobs, remoting and transcripts keep PowerShell's formatting records and text.

```powershell
Enable-ConsoleFormatting -Feature FormatCommands
Enable-ConsoleFormatting -Feature All, FormatCommands   # everything at once
Disable-ConsoleFormatting -Feature FormatCommands       # just this one off again
```

{{< cm-demo id="format-commands" heading="h3" >}}

## Views that ship with a module

A module can describe how its objects look without depending on ConsoleMonster: a `<ModuleName>.consoleviews.psd1` file beside its manifest names the columns, their presenters and the grouping, as plain data. ConsoleMonster reads it for loaded modules when formatting is on, and `Format-ConsoleTable` and the exports use the same columns.

{{< cm-demo id="module-views" heading="h3" >}}

## Reports and the clipboard

`-As Markdown`, `-As Html` and `-As Text` on the Format-Console commands return a document instead of drawing, also inside scripts and on Windows PowerShell 5.1, and `Copy-ConsoleOutput` puts one on the clipboard:

```powershell
Import-Module ConsoleMonster

Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -As Markdown | Set-Content -Path ./services.md
Get-Service | Select-Object -First 8 Name, Status, StartType | Copy-ConsoleOutput -As Markdown
```

[Coming from Format-Table](/guides/format-table/) shows the Markdown a table becomes.

## More from the same switch

- `Enable-ConsoleFormatting -Feature ErrorView` and `-Feature Progress` add the [error view and the progress area](/docs/help-and-defaults/).
- `Register-ConsoleView` adds your own types in the session, including columns computed by script blocks; `Register-ConsoleView -Path` loads a `.consoleviews.psd1` file from anywhere.
- `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree` give the same views on demand, with column options; see [Coming from Format-Table](/guides/format-table/).