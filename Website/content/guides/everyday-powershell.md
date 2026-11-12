---
title: Everyday PowerShell output
description: "Enable-ConsoleFormatting makes everyday output readable and sums it up: folder trees, processes, services, events, JSON, history, jobs, drives and certificates."
slug: everyday-powershell
card: formatting
order: 13
group: tasks
---

The commands you already type can show readable tables, records and a summary line, without changing a single command. One line turns it on for the session:

```powershell
Enable-ConsoleFormatting
```

Files, processes, services, events, JSON and CSV records, history, jobs, drives, certificates, `Select-String` matches and `Compare-Object` results then show as ConsoleMonster views when they reach the console. The objects stay the same: pipelines, variables, `Out-String`, files, transcripts, jobs and remoting get exactly what PowerShell gives them.

{{< cm-demo id="formatting" heading="h2" autoplay="true" >}}

## Before and after

```powershell
'{"Name":"api-01","Settings":{"Region":"west"},"Tags":["blue","ready"]}' | ConvertFrom-Json
```

Before, `Settings` reads `@{Region=west}` and `Tags` reads `{blue, ready}`. After:

```text
api-01
Settings  Region=west
Tags      blue, ready
```

Collections in a cell are joined (`DNS, GC, PDC`) with a `+N` marker for what does not fit, durations read as `42 s` or `4 h 00 m`, and status words get their symbol and colour. `Get-ChildItem`, `Get-Process`, `Get-Service` and `Get-WinEvent` end with a line that sums up the result: sizes, memory, stopped services, errors and warnings.

{{< cm-demo id="shaping" heading="h2" >}}

## Keep it on in every session

1. Print the lines for your profile. Nothing is written for you:

   ```powershell
   Enable-ConsoleFormatting -ProfileSnippet
   ```

2. Open your profile, creating it if it does not exist yet:

   ```powershell
   if (-not (Test-Path $PROFILE)) { New-Item -Path $PROFILE -ItemType File -Force }
   notepad $PROFILE
   ```

   Any editor works; on macOS and Linux use `code $PROFILE` or `nano $PROFILE`.

3. Paste the printed lines, save, and start a new PowerShell session.

The same lines work in Windows PowerShell 5.1 and PowerShell 7. Each has its own `$PROFILE`, so add them to both if you use both.

## Turn parts off

Each part can be left out, in the profile line or later in the session:

| To | Run |
| --- | --- |
| Keep `Format-Table`, `Format-List` and `Format-Wide` native | `Enable-ConsoleFormatting -Exclude FormatCommands` |
| Show only the views you register yourself | `Enable-ConsoleFormatting -NoBuiltIn` |
| Turn one feature off for this session | `Disable-ConsoleFormatting -Feature Streams` |
| See what is on | `Get-ConsoleFormatting` |
| Go back to PowerShell's output | `Disable-ConsoleFormatting` |

`-Include` adds the opt-in features: `GridView` (Out-GridView in the terminal where the desktop grid is unavailable), `Progress`, `Help` and `Prompt`. Add the same parameters to `-ProfileSnippet` to print matching profile lines.

For a view on demand, without the switch, use `Show-ConsoleTable` or `Show-Console -View List`. To export a view as Markdown or HTML, see [Coming from Format-Table](/guides/format-table/).

## Next

- [Everyday formatting reference](/docs/everyday-formatting/): every built-in view, its summary line and the features.
- [Restyle any command's output](/guides/restyle-commands/): a view for a command that has none.
- [Coming from Format-Table](/guides/format-table/): the Format commands and column options.
- [Choose a theme](/docs/themes/): the colours the views use.
