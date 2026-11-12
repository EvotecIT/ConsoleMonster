---
title: Everyday formatting
description: "What Enable-ConsoleFormatting changes for everyday commands: files, processes, services, events, JSON, CSV, certificates and the Format commands."
slug: everyday-formatting
group: shells
order: 55
card: formatting
chips: [PowerShell, Formatting]
apis: [Enable-ConsoleFormatting, Disable-ConsoleFormatting, Get-ConsoleFormatting, Register-ConsoleView]
summary: "The view and summary line each everyday command gets, the features you can turn on or off, and what stays PowerShell's."
---

`Enable-ConsoleFormatting` gives everyday commands a ConsoleMonster view when their output reaches the console. This page lists what each command shows, the features behind the switch and what stays as PowerShell writes it. To turn it on and keep it on, follow [Everyday PowerShell output](/guides/everyday-powershell/).

{{< cm-frame ids="everyday-commands-ps5,everyday-commands-ps7" caption="Formatted at a Windows PowerShell 5.1 and PowerShell 7 prompt with invented inventory records. Switch the site theme to see the light or dark run." >}}

## What changes and what stays

Only output that goes straight to the console changes. Pipelines into other commands, assignment, `Out-String`, `Out-File`, redirection, transcripts, jobs and remoting keep PowerShell's objects and text. The objects themselves are never changed.

Typed records with their own PowerShell table view keep its columns and expressions. An explicit `Register-ConsoleView` registration takes precedence over a built-in view, and `-NoBuiltIn` keeps only your own.

## Features

| Feature | Default | What it does |
| --- | --- | --- |
| `Output` | On | Views for registered and built-in types. |
| `FormatCommands` | On | `Format-Table`, `Format-List` and `Format-Wide` drawn by ConsoleMonster; see [Coming from Format-Table](/guides/format-table/). |
| `Errors` | On | The [error view](/docs/help-and-defaults/). |
| `Streams` | On | Theme colours for warning, verbose, debug and native progress messages. |
| `ReadLine` | On | Theme colours for the PSReadLine editor, if it is loaded. |
| `Progress` | Off | `Write-Progress` drawn as ConsoleMonster's progress area. |
| `Help` | Off | Styled command help. |
| `GridView` | Off | `Out-GridView` in the terminal where the desktop grid is unavailable. |
| `Prompt` | Off | A short prompt with path, git state and the last command's duration. |

`-Include` adds features, `-Exclude` leaves them out, `Disable-ConsoleFormatting -Feature` turns one off, and `Get-ConsoleFormatting` lists what is on.

## Summary lines

A line under the view sums it up. Small results get no line, and only the console view has it: `Show-ConsoleTable`, `Out-String` and captured output stay as they were.

| View | The line under it |
| --- | --- |
| `Get-ChildItem` | Folders, files, their total size and the largest. With `-Recurse` the listing is one tree, folders first, each with the size of the files under it. |
| `Get-Process` | How many, their memory together and the process using the most. Memory draws a bar against the largest. |
| `Get-Service` | Running and stopped, and automatic services that are not running. |
| `Get-WinEvent` | Errors, warnings and information, the time they cover and a sparkline of when they happened. |
| `Test-Connection` | Replies, minimum, average and maximum latency, one line per destination. |
| `Get-History` | Commands, failures, the time they took and the slowest. |
| Certificates, `Select-String`, `Get-Job` | Expired and expiring certificates; matches and files; jobs by state. |

{{< cm-frame ids="everyday-summaries" script="true" >}}

Your own types can have a line too: `Register-ConsoleView -Summary` takes a script that returns facts. [Restyle any command's output](/guides/restyle-commands/) walks through it.

## JSON and CSV

Flat objects from `ConvertFrom-Json` and `Invoke-RestMethod` become tables. Small nested objects show their fields and short arrays their values (`Tags  blue, ready`); null shows `—` and empty collections `(none)`. An API envelope such as `{ Total: 3, Items: [...] }` shows its scalar fields first, then a table of the records.

| Limit | Value |
| --- | --- |
| Outer table | 50 rows, 24 fields |
| Nested table | 5 rows each, 24 nested rows per payload |
| Scalar array | 6 values |
| Object depth | Two levels below the root; deeper or wider than six keys shows `{…} 12 keys` |

Omitted items show a `+N more` hint. CSV keeps its header order and original strings; columns whose values all look numeric or date-like align right, and `0012` stays `0012`. Captured and exported data is always complete.

## Commands, modules and history

| View | Shows |
| --- | --- |
| `Get-Command` | Name, command-type chips, version and source; aliases show their target. Twelve or more commands group by module. |
| `Get-Module` | Name, version, type, exported command count and a shortened path. Versions group under one name. |
| `Get-History` | Id, start time, duration, status and highlighted command text. |
| `Get-Job`, `Get-PSDrive` | Jobs with state, start, duration and command; drives with used and free space and a usage bar that warns at 80% and fails at 95%. |
| `Get-InstalledPSResource`, `Get-InstalledModule` | Name, version, repository, author and publication date, with counts by repository from five items. |

## Search, permissions, certificates and measurements

| View | Shows | Where |
| --- | --- | --- |
| `Select-String` | One table per file with line numbers, the match highlighted and `-Context` lines dimmed | All platforms |
| `Get-Acl` | Path and owner as the title, then a row per rule: allow or deny, the account, the rights in words, what it applies to and whether it is inherited | Windows |
| Certificates | Status (`Expired`, `Expiring` within 30 days, `Valid`), subject, issuer, expiry, days left and thumbprint | Windows for `Cert:\`; any certificate object |
| `Group-Object` | Name and count with a bar against the largest group | All platforms |
| `Measure-Object` | Only the statistics you asked for | All platforms |
| `TimeSpan` | A readable duration (`1 h 42 m`, `250 ms`) beside the exact value | All platforms |

`Select-String -Raw` and `-Quiet` keep their native strings and booleans.

## Windows inventory and network

| Command | Shows |
| --- | --- |
| `Get-Volume` | Drive, label, file system, health, capacity and a usage bar; total, free and unhealthy volumes. |
| `Get-Disk` | Name, bus, partition style, health, status and size; disks that need attention. |
| `Get-NetTCPConnection` | Endpoints, state, PID and process name; counts by state and listening ports. |
| `Get-ScheduledTask` | Path, name and state, with counts by state. |
| `Get-LocalUser` | Enabled, last logon, password expiry and description, with enabled, disabled and never-used counts. |
| `Test-Connection` (PowerShell 7) | Destination, reply, address, latency and readable failures such as `Timed out`. All platforms. |
| .NET `IPAddress`, `Dns.GetHostEntry` | Address and family; host name, addresses and aliases. All platforms. |
| `Resolve-DnsName` | Name, type, TTL and the answer for A, AAAA, CNAME, MX, NS, PTR, TXT, SRV and SOA. |
| `Get-NetIPAddress`, `Get-NetAdapter`, `Test-NetConnection` | Interface and address; adapter status, MAC and speed; ping and TCP outcomes. |

Unless marked, these are Windows views and register only on Windows. No view sends network requests or loads a module: the command you run supplies the objects.

## Compare-Object

`Compare-Object` results show the counts, then each item marked `-` when only the reference set has it, `+` when only the difference set has it and `=` for both (`-IncludeEqual`). With `-Property`, the first property names the item. The objects still carry `SideIndicator`, so filters and files work as before. [`Compare-ConsoleObject`](/docs/object-drift/) matches by key and lists changed properties.

{{< cm-frame ids="compare-object-ps5,compare-object-ps7" script="true" >}}

## Messages and the command-line editor

`Streams` gives warning, verbose and debug messages the theme's colours on Windows PowerShell 5.1 and PowerShell 7; their prefixes, preferences and redirection stay PowerShell's. `ReadLine` colours the PSReadLine editor, including selection and inline predictions; editing mode, key bindings and prediction source stay yours. Re-run `Enable-ConsoleFormatting` after `Set-ConsoleTheme` to apply a new theme.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="Before and after `-Include Streams`; the light-theme runs show when the site is light." >}}

## Out-GridView without a desktop

```powershell
Enable-ConsoleFormatting -Include GridView
Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
Disable-ConsoleFormatting -Feature GridView
```

A working native `Out-GridView` is always kept. Where it is unavailable, `Out-GridView` opens `Show-ConsoleGrid`, with filtering, sorting and selection, and maps `-Title`, `-PassThru`, `-OutputMode` and `-Wait`. Selection returns the original objects. Without an interactive console it prints a table with a warning and returns nothing. An existing function or alias named `Out-GridView` is kept unless you add `-Force`.

{{< cm-frame ids="gridview-fallback-120,gridview-fallback-60" script="true" >}}

## Views that ship with a module

A module can describe its own views in a `<ModuleName>.consoleviews.psd1` file beside its manifest, with no dependency on ConsoleMonster. [Custom views](/docs/custom-views/) describes the file.

{{< cm-demo id="module-views" heading="h3" >}}

## Next

- [Everyday PowerShell output](/guides/everyday-powershell/): turn it on and keep it in your profile.
- [Restyle any command's output](/guides/restyle-commands/): a view for a command that has none.
- [Help, errors and progress](/docs/help-and-defaults/): the error view and the opt-in features.
- [Hosts, CI and transcripts](/docs/hosts-and-ci/): what each host receives.
