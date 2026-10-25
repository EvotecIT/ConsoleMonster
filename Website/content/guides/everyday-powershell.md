---
title: Everyday PowerShell output
description: "Enable-ConsoleFormatting makes everyday output readable: JSON, CSV, commands, modules, history and network replies alongside files, processes and hashtables."
slug: everyday-powershell
card: formatting
order: 1
---

You do not have to change your commands to get readable output. One line turns it on for the session:

```powershell
Enable-ConsoleFormatting
```

Files and folders, processes, services, JSON and CSV records, commands, modules, history, network replies, event log records, IP addresses, volumes and dictionaries are then shown as ConsoleMonster tables and records when they reach the console. Objects stay the same: pipelines into other commands, `Out-String`, redirected output, transcripts, jobs and remoting keep PowerShell's behaviour. `Disable-ConsoleFormatting` turns it off.

Typed records with their own PowerShell table view keep its columns and expressions. An explicit `Register-ConsoleView` registration takes precedence.

> `Enable-ConsoleFormatting` changes only output that reaches the console without a format command: `Format-Table`, `Format-List` and `Format-Wide` stay PowerShell's own unless you also turn on `-Feature FormatCommands` (below). For ConsoleMonster's views on demand, use `Show-ConsoleTable` or `Show-ConsoleList`.

To keep it on in every session, print the lines for your profile and paste them in (nothing is written for you):

```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

{{< cm-demo id="formatting" heading="h2" autoplay="true" >}}

{{< cm-demo id="shaping" heading="h2" >}}

Collections in a cell are joined (`DNS, GC, PDC`), with a `+N` marker for the items that do not fit, where Format-Table shows `{DNS, GC, PDC}` and cuts long lists short. Durations read as `42 s` or `4 h 00 m`, and status words get their symbol and colour. `-GroupBy` writes a labelled heading per group, with its count.

## JSON and CSV at the prompt

Flat objects from `ConvertFrom-Json` and `Invoke-RestMethod` become tables. Small nested objects show their fields, and short scalar arrays show their values: `Tags  blue, ready`. Null values show `—`, as in `Retry=—` or `Next  —`; empty collections and objects show `(none)`. Objects expand to two levels below the root; only deeper objects or objects with more than six keys use a summary such as `{…} 12 keys`.

An API envelope such as `{ Total: 3, Items: [...] }` shows its scalar metadata first, then an `Items` table with the actual records. Several record arrays each get a labelled table. Nested tables show up to five rows each, with a budget of 24 nested rows per displayed payload; scalar arrays show up to six values. Omitted items have a `+N more` hint, and long text ends with an ellipsis. The outer table still shows at most 50 rows and 24 fields. Narrow windows hide lower-priority columns with a hint or switch to cards.

```powershell
Enable-ConsoleFormatting
'{"Name":"api-01","Settings":{"Region":"west"},"Tags":["blue","ready"]}' | ConvertFrom-Json
```

Before: `Settings` reads `@{Region=west}` and `Tags` reads `{blue, ready}`. After:

```text
api-01
Settings  Region=west
Tags      blue, ready
```

CSV keeps the header order and the original strings. Columns whose nonempty values all look numeric or date-like align to the right; `0012` stays `0012`, and `12.50` keeps its trailing zero. Text columns align left.

```powershell
'Name,Count,When', 'api-01,0012,2026-10-24', 'api-02,123456,2026-10-25' | ConvertFrom-Csv
Import-Csv ./inventory.csv
```

Before: CSV numbers and dates align like other strings. After: `Count` and `When` align right, with `Name` on the left. JSON, REST, CSV and hand-written `[pscustomobject]` records share PowerShell's `PSCustomObject` type, so they use the same bounded display; no provenance tags or extra properties are added.

Capture and export still receive the complete data:

```powershell
$rows = 'Name,Count', 'api-01,0012', 'api-02,2048' | ConvertFrom-Csv
$rows | Select-Object Name, Count
$rows | Export-Csv ./inventory-copy.csv -NoTypeInformation
```

## Commands, modules and history

```powershell
Get-Command Get-Date, Get-Command
Get-Module
Get-Module -ListAvailable
Get-History
```

| View | Before | After |
| --- | --- | --- |
| Commands | Native command table, with long names competing for space | Name first, command-type chips, version and source; aliases show `alias -> target`. Results of 12 or more commands group by module. |
| Modules | Exported command names can dominate a row | Name, version, module type, exported command count and a shortened path. Multiple versions group under one module name. Paths keep `User ~/...` or their system installation location visible; the stored path stays complete. |
| History | Id and command line | Id, start time, elapsed duration, execution status and syntax-highlighted command text. Long commands end with an ellipsis. |

`Get-Module -ListAvailable` reports the exported-command metadata PowerShell supplies; the view never imports a module to discover more commands. History uses local start times (`HH:mm:ss`) and PowerShell's `ExecutionStatus`, including completed and failed commands.

## Network replies and addresses

```powershell
Test-Connection 127.0.0.1 -Count 2
[System.Net.Dns]::GetHostEntry('localhost')
[System.Net.Dns]::GetHostAddresses('localhost')
[System.Net.IPAddress]::Loopback
```

| View | Before | After | Where |
| --- | --- | --- | --- |
| Ping replies | Native ping table | Destination, reply number, address, latency in milliseconds and success/failure colour; readable reasons such as `Timed out` and `Host unreachable`, with no invented failure latency | PowerShell 7 on macOS, Linux and Windows |
| .NET addresses and DNS entries | Addresses or the native DNS record display | Address and `IPv4`/`IPv6` family; host name, address list and aliases | macOS, Linux and Windows |
| `Resolve-DnsName` | Different native fields for each record kind | Name, type, TTL and the answer for A/AAAA, CNAME, MX, NS, PTR, TXT, SRV and SOA records | Windows, with DnsClient |
| `Get-NetIPAddress` | Native address fields | Interface, address, prefix, family and address state | Windows, with NetTCPIP |
| `Get-NetAdapter` | Native adapter table | Name, status, MAC address, link speed and description | Windows, with NetAdapter |
| `Test-NetConnection` | Native connection report | Computer, remote address/port, interface and ping/TCP outcomes | Windows, with NetTCPIP |

The Windows views register only on Windows. None of these views performs network requests, probes installed commands or loads optional networking modules: the commands you run supply the objects. `Enable-ConsoleFormatting -NoBuiltIn` leaves the native built-in displays in place, and `Disable-ConsoleFormatting` restores native output.

## Format-Table, Format-List and Format-Wide

Scripts call the three Format commands too, so they get their own switch, separate from `-Feature All`. With it on, the commands you already type draw ConsoleMonster views, with their parameters (`-Property`, `-GroupBy`, `-AutoSize`, `-Wrap`, `-View`, `-Column`) mapped. Only output that goes straight to the console changes: `$x = ... | Format-Table`, `| Out-String`, `| Out-File`, `>`, jobs, remoting and transcripts keep PowerShell's formatting records and text.

```powershell
Enable-ConsoleFormatting -Feature FormatCommands
Enable-ConsoleFormatting -Feature All, FormatCommands   # everything at once (All alone leaves the Format commands as they are)
Disable-ConsoleFormatting -Feature FormatCommands       # just this one off again
```

{{< cm-demo id="format-commands" heading="h3" >}}

## Command-line editor and native progress

`ReadLine` colors the already loaded PSReadLine editor from the active theme, including selection and available inline predictions. `Streams` also colors native progress: `$PSStyle.Progress.Style` on PowerShell 7.2+, or available host progress colors on Windows PowerShell 5.1. The user's progress view, editing mode, key bindings and prediction behavior stay as configured. Missing PSReadLine and unsupported color properties are skipped.

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Feature ReadLine, Streams
Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42
Write-Progress -Activity 'Backup' -Completed
Enable-ConsoleFormatting -Feature ReadLine, Streams -ProfileSnippet
Disable-ConsoleFormatting -Feature ReadLine, Streams
```

ReadLine is included in `All`. When runspaces share native preferences, the most recently applied installation supplies the colors. Disabling or removing that owner restores the latest surviving installation's colors; the last owner restores the first exact baseline, even after user edits. Re-enable after changing themes. Selecting only `ReadLine` and `Streams` keeps native `Write-*` commands and installs no `Out-Default` function. All also keeps every Write-* cmdlet native. The separate `Progress` feature is excluded from All and explicitly opts into the native-metadata `Write-Progress` function adapter.
## Out-GridView without a desktop

```powershell
Enable-ConsoleFormatting -Feature GridView
Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
Disable-ConsoleFormatting -Feature GridView
```

`GridView` is excluded from `All`. A working native `Out-GridView` is always kept. Where it is unavailable, the session function uses `Show-ConsoleGrid`: the same keyboard filtering, sorting and selection, with `-Title`, `-PassThru`, `-OutputMode None|Single|Multiple` and `-Wait`. Browse mode returns nothing; selection returns the original objects. The terminal view is synchronous, so it waits until closed even without `-Wait`.

{{< cm-frame ids="gridview-fallback-120,gridview-fallback-60" script="true" >}}

Without an interactive console, the grid prints its existing table fallback with a warning and returns no selections. Existing functions and aliases need `-Force`; disabling or removing the module restores them. Constants, `AllScope` aliases and caller-local aliases that cannot be replaced safely are kept with a warning even with `-Force`. Remove or rename them in their defining scope first if you want the fallback. A working desktop command and its captures, jobs and transcripts keep PowerShell's behavior.

The .NET equivalent uses `Grid`, the renderer behind `Show-ConsoleGrid`:

```csharp
using ConsoleMonster.Interactive;

Grid grid = new Grid("Choose nodes") { Selection = GridSelection.Single };
grid.AddColumn("Name").AddRow("api-east", "api-east");
GridResult selected = grid.Show();
```

## Views that ship with a module

A module can describe how its objects look without depending on ConsoleMonster: a `<ModuleName>.consoleviews.psd1` file beside its manifest names the columns, their presenters and the grouping, as plain data. ConsoleMonster reads it for loaded modules when formatting is on, and `Show-ConsoleTable` and the exports use the same columns.

{{< cm-demo id="module-views" heading="h3" >}}

## Reports and the clipboard

`Export-ConsoleFrame -Format Markdown`, `Html` or `FencedText` creates a document from ordinary objects or widgets, including inside scripts and on Windows PowerShell 5.1. `Copy-ConsoleOutput -Format Markdown` puts the document on the clipboard:

```powershell
Import-Module ConsoleMonster

Get-Service | Select-Object -First 8 Name, Status, StartType | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Format Markdown | Set-Content -Path ./services.md -Encoding UTF8
Get-Service | Select-Object -First 8 Name, Status, StartType | Copy-ConsoleOutput -Format Markdown
```

[Coming from Format-Table](/guides/format-table/) shows the Markdown a table becomes.

## Warning, verbose and debug messages

`Enable-ConsoleFormatting -Feature Streams` (part of `-Feature All`) gives PowerShell's own warning, verbose and debug messages the theme's colours on Windows PowerShell 5.1 and PowerShell 7. Their `WARNING:` and `VERBOSE:` prefixes, preferences and redirection stay PowerShell's; [Help, errors and progress](/docs/help-and-defaults/#warning-verbose-and-debug-colours) has the details.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="Before and after `-Feature Streams`; the light-theme runs show when the site is light." >}}

## More from the same switch

- `Enable-ConsoleFormatting -Feature ErrorView`, `-Feature Progress` and `-Feature Streams` add the [error view, the progress area and themed warning, verbose and debug colours](/docs/help-and-defaults/); `-Feature All` adds default output, the error view, native stream/progress colors and ReadLine colors. The native `Write-Progress` adapter requires a separate opt-in.
- `Register-ConsoleView` adds your own types in the session, including columns computed by script blocks; `Register-ConsoleView -Path` loads a `.consoleviews.psd1` file from anywhere.
- `Show-ConsoleTable`, `Show-ConsoleList` and `Show-Console -View Tree` give the same views on demand, with column options; see [Coming from Format-Table](/guides/format-table/).
