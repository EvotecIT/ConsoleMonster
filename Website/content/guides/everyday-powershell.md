---
title: Everyday PowerShell output
description: "Enable-ConsoleFormatting makes ordinary output readable for the session: files, processes, hashtables, arrays and groups, before and after."
slug: everyday-powershell
order: 1
---

You do not have to change your commands to get readable output. One line turns it on for the session:

```powershell
Enable-ConsoleFormatting
```

Files and folders, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries are then shown as ConsoleMonster tables and records when they reach the console. Objects stay the same: pipelines into other commands, `Out-String`, redirected output, transcripts, jobs and remoting keep PowerShell's behaviour. `Disable-ConsoleFormatting` turns it off.

> `Enable-ConsoleFormatting` changes only output that reaches the console without a format command: `Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own. For ConsoleMonster's views, use `Format-ConsoleTable` or `Format-ConsoleList`.

To keep it on in every session, print the lines for your profile and paste them in (nothing is written for you):

```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

{{< cm-demo id="formatting" heading="h2" autoplay="true" >}}

{{< cm-demo id="shaping" heading="h2" >}}

Collections in a cell are joined (`DNS, GC, PDC`), with a `+N` marker for the items that do not fit, where Format-Table shows `{DNS, GC, PDC}` and cuts long lists short. Durations read as `42 s` or `4 h 00 m`, an empty collection is a dash, and status words get their symbol and colour. `-GroupBy` writes a labelled heading per group, with its count.

## More from the same switch

- `Enable-ConsoleFormatting -Feature ErrorView` and `-Feature Progress` add the [error view and the progress area](/docs/help-and-defaults/).
- `Register-ConsoleView` adds your own types: a view per type name with the columns you choose.
- `Format-ConsoleTable`, `Format-ConsoleList` and `Format-ConsoleTree` give the same views on demand, with column options; see [Coming from Format-Table](/guides/format-table/).