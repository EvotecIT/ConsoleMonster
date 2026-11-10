---
title: Everyday PowerShell output
description: "Enable-ConsoleFormatting makes everyday output readable and sums it up: folder trees, processes, services, events, JSON, history, jobs, drives and certificates."
slug: everyday-powershell
card: formatting
order: 1
---

You do not have to change your commands to get readable output. One line turns it on for the session:

```powershell
Enable-ConsoleFormatting
```

Files and folders, processes, services, JSON and CSV records, commands, modules, history, jobs, drives, `Select-String` matches, ACLs, certificates, groups, measurements, durations, network replies, event log records, IP addresses, volumes, dictionaries and `Compare-Object` results are then shown as ConsoleMonster tables and records when they reach the console. Objects stay the same: pipelines into other commands, `Out-String`, redirected output, transcripts, jobs and remoting keep PowerShell's behaviour. `Disable-ConsoleFormatting` turns it off.

Typed records with their own PowerShell table view keep its columns and expressions. An explicit `Register-ConsoleView` registration takes precedence.

> `Enable-ConsoleFormatting` also styles `Format-Table`, `Format-List` and `Format-Wide` when output goes straight to the console. Use `-Exclude FormatCommands` to keep those commands native. For views on demand, use `Show-ConsoleTable` or `Show-Console -View List`.

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

## Summaries and folder trees

A line under the view sums it up, so the answer to "how much?" or "anything wrong?" is the last thing on screen:

```powershell
Get-ChildItem -Recurse
Get-Process
Get-Service
Get-WinEvent -LogName System -MaxEvents 200
Test-Connection example.com -Count 10
Get-History
```

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Summary lines under everyday views">

| View | The line under it |
| --- | --- |
| `Get-ChildItem` | Folders, files, the size of the files and the largest one. With `-Recurse` the listing is one tree, folders first, each with the size of the files listed under it. |
| `Get-Process` | How many, their memory together and the process using the most. Memory draws a bar against the largest. |
| `Get-Service` | Running and stopped, and automatic services that are not running, which the start type column also marks. |
| `Get-WinEvent` | Errors, warnings and information, the time they cover and a sparkline of when they happened. |
| `Test-Connection` | Replies, minimum, average and maximum latency and its shape, one line per destination. |
| `Get-History` | Commands, failures, the time they took and the slowest. Durations draw a bar. |
| Certificates, `Select-String`, `Get-Job` | Expired and expiring certificates; matches and files; jobs by state. |

</div>

Small results get no line, and only the console view has it: `Show-ConsoleTable`, `Out-String` and captured output stay as they were. `Get-Job` and `Get-PSDrive` have views of their own too: jobs with their state, start, duration and command; drives with used and free space and a usage bar that warns at 80% and fails at 95%.

Your own types can have the line. The script gets the objects shown together and returns facts: strings, or hashtables with `Value`, `Label`, `Caption`, `Presenter` and `Role`:

```powershell
Register-ConsoleView -TypeName Contoso.Server -Property Name, State -Summary {
    param($servers)
    @{ Value = $servers.Count; Label = 'servers' }
    $stopped = @($servers | Where-Object State -ne 'Running').Count
    if ($stopped) { @{ Value = $stopped; Label = 'stopped'; Role = 'Warn' } }
}
```

{{< cm-frame ids="everyday-summaries" script="true" >}}

## Commands, modules and history

```powershell
Get-Command Get-Date, Get-Command
Get-Module
Get-Module -ListAvailable
Get-History
```

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Command, module and history formatting comparison">

| View | Before | After |
| --- | --- | --- |
| Commands | Native command table, with long names competing for space | Name first, command-type chips, version and source; aliases show the command they point to. Results of 12 or more commands group by module. |
| Modules | Exported command names can dominate a row | Name, version, module type, exported command count and a shortened path. Multiple versions group under one module name. Paths keep `User ~/...` or their system installation location visible; the stored path stays complete. |
| History | Id and command line | Id, start time, elapsed duration, execution status and syntax-highlighted command text. Long commands end with an ellipsis. |

</div>

`Get-Module -ListAvailable` reports the exported-command metadata PowerShell supplies; the view never imports a module to discover more commands. History uses local start times (`HH:mm:ss`) and PowerShell's `ExecutionStatus`, including completed and failed commands.

## Search, permissions, certificates and measurements

```powershell
Select-String -Path ./logs/*.log -Pattern 'Error|Warning' -Context 1
Get-Acl -Path C:\Windows\win.ini
Get-ChildItem Cert:\CurrentUser\My
Get-Process | Group-Object ProcessName | Sort-Object Count -Descending
Get-ChildItem -File | Measure-Object -Property Length -Sum -Average
Measure-Command { Start-Sleep -Milliseconds 250 }
```

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Search, permission, certificate and measurement formatting comparison">

| View | Before | After | Where |
| --- | --- | --- | --- |
| `Select-String` | `path:line:text`, with `>` before matched lines | One table per file with line numbers; the matched text highlighted and `-Context` lines dimmed | macOS, Linux and Windows |
| `Get-Acl` | Path, owner and the rules as one long string | The path and owner as the title, then a row per rule: allow or deny with its marker, the account, the rights in words (`Read and execute`), what it applies to for folders and registry keys, and whether it is inherited | Windows |
| Certificates | Thumbprint and subject | Status (`Expired`, `Expiring` within 30 days, `Valid`), subject, issuer, expiry date, days left and thumbprint, under the store path for `Cert:\` | Windows for `Cert:\`; any certificate object elsewhere |
| `Group-Object` | Count, name and the grouped objects | Name and count with a bar scaled to the largest group | macOS, Linux and Windows |
| `Measure-Object` | Every statistic, empty or not | The statistics you asked for; one result is a key/value card | macOS, Linux and Windows |
| `TimeSpan` (`Measure-Command`, `New-TimeSpan`) | Days, hours, ticks and every total | A readable duration (`1 h 42 m`, `250 ms`) beside PowerShell's exact value | macOS, Linux and Windows |

</div>

The ACL view reads the rules with the security object's own methods, so it works the same before and after `Microsoft.PowerShell.Security` is loaded. The search, ACL and certificate objects stay exactly as the commands return them: `$acl.Access`, `$match.Line` and `$cert.NotAfter` are unchanged, and `Select-String -Raw` and `-Quiet` keep their native strings and booleans.

{{< cm-frame ids="everyday-commands-ps5,everyday-commands-ps7" caption="Formatted at a Windows PowerShell 5.1 and PowerShell 7 prompt with invented inventory records. Switch the site theme to see the light or dark run." >}}

Windows inventory commands show the fields you usually need first. Larger results include a summary under the table.

| Command | View and summary |
| --- | --- |
| `Get-Volume` | Drive, label, file system, health, capacity and a usage bar. The summary shows total capacity, free space and unhealthy volumes. |
| `Get-Disk` | Disk name, bus, partition style, health, operational status and size. The summary flags disks that need attention. |
| `Get-NetTCPConnection` | Local and remote endpoints, connection state, PID and the local process name when available. The summary counts connections by state and distinct listening ports. |
| `Get-ScheduledTask` | Path, task name and state, with counts by state. The view uses the returned task data. |
| `Get-LocalUser` | Enabled status, last logon, password expiry and description, with counts for enabled, disabled and never used accounts. |

`Get-InstalledPSResource` and `Get-InstalledModule` show names, versions, repositories, authors and available publication dates on supported platforms. Lists of five or more also show counts by repository.

## Network replies and addresses

```powershell
Test-Connection 127.0.0.1 -Count 2
[System.Net.Dns]::GetHostEntry('localhost')
[System.Net.Dns]::GetHostAddresses('localhost')
[System.Net.IPAddress]::Loopback
```

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Network formatting support across hosts">

| View | Before | After | Where |
| --- | --- | --- | --- |
| Ping replies | Native ping table | Destination, reply number, address, latency in milliseconds and success/failure colour; readable reasons such as `Timed out` and `Host unreachable`, with no invented failure latency | PowerShell 7 on macOS, Linux and Windows |
| .NET addresses and DNS entries | Addresses or the native DNS record display | Address and `IPv4`/`IPv6` family; host name, address list and aliases | macOS, Linux and Windows |
| `Resolve-DnsName` | Different native fields for each record kind | Name, type, TTL and the answer for A/AAAA, CNAME, MX, NS, PTR, TXT, SRV and SOA records | Windows, with DnsClient |
| `Get-NetIPAddress` | Native address fields | Interface, address, prefix, family and address state | Windows, with NetTCPIP |
| `Get-NetAdapter` | Native adapter table | Name, status, MAC address, link speed and description | Windows, with NetAdapter |
| `Test-NetConnection` | Native connection report | Computer, remote address/port, interface and ping/TCP outcomes | Windows, with NetTCPIP |

</div>

The Windows views register only on Windows. None of these views performs network requests, probes installed commands or loads optional networking modules: the commands you run supply the objects. `Enable-ConsoleFormatting -NoBuiltIn` leaves the native built-in displays in place, and `Disable-ConsoleFormatting` restores native output.

## Compare-Object

`Compare-Object` results are drawn as a comparison: the counts, then each item marked `-` when only the reference set has it, `+` when only the difference set has it and `=` when both have it (`-IncludeEqual`). With `-Property`, the first property names the item and the others show its values. This is the same view as [`Compare-ConsoleObject`](/docs/object-drift/). The objects still carry `SideIndicator`, so `Where-Object SideIndicator -eq '=>'`, `$diff = Compare-Object ...` and `Compare-Object ... | Out-File` work as before.

{{< cm-frame ids="compare-object-ps5,compare-object-ps7" script="true" >}}

To have Compare-ConsoleObject match objects by key and list which properties changed, call `Compare-ConsoleObject $before $after -Property Name`.

## Format-Table, Format-List and Format-Wide

The default includes the three Format commands. With formatting on, the commands you already type draw ConsoleMonster views, with their parameters (`-Property`, `-GroupBy`, `-AutoSize`, `-Wrap`, `-View`, `-Column`) mapped. Only output that goes straight to the console changes: `$x = ... | Format-Table`, `| Out-String`, `| Out-File`, `>`, jobs, remoting and transcripts keep PowerShell's formatting records and text.

```powershell
Enable-ConsoleFormatting
Enable-ConsoleFormatting                              # everyday defaults
Enable-ConsoleFormatting -Exclude FormatCommands      # leave Format-* native
Disable-ConsoleFormatting -Feature FormatCommands       # just this one off again
```

{{< cm-demo id="format-commands" heading="h3" >}}

## Command-line editor and native progress

`ReadLine` colors the already loaded PSReadLine editor from the active theme, including selection and available inline predictions. `Streams` also colors native progress: `$PSStyle.Progress.Style` on PowerShell 7.2+, or available host progress colors on Windows PowerShell 5.1. The user's progress view, editing mode, key bindings and prediction behavior stay as configured. Missing PSReadLine and unsupported color properties are skipped.

This example also enables the default formatting features and turns them off at the end.

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Theme Light
Enable-ConsoleFormatting -Include ReadLine, Streams
Write-Progress -Activity 'Backup' -Status 'Copying archive.zip' -PercentComplete 42
Write-Progress -Activity 'Backup' -Completed
Enable-ConsoleFormatting -Include ReadLine, Streams -ProfileSnippet
Disable-ConsoleFormatting
```

ReadLine is included in the default set. When runspaces share native preferences, the most recently applied installation supplies the colors. Disabling or removing that owner restores the latest surviving installation's colors; the last owner restores the first exact baseline, even after user edits. Re-enable after changing themes. Selecting only `ReadLine` and `Streams` keeps native `Write-*` commands and installs no `Out-Default` function. The defaults also keep every Write-* cmdlet native. The separate `Progress` feature explicitly opts into the native-metadata `Write-Progress` function adapter.
## Out-GridView without a desktop

```powershell
Enable-ConsoleFormatting -Include GridView
Get-Process | Out-GridView -Title 'Choose processes' -OutputMode Single
Disable-ConsoleFormatting -Feature GridView
```

`GridView` is an explicit opt-in. A working native `Out-GridView` is always kept. Where it is unavailable, the session function uses `Show-ConsoleGrid`: the same keyboard filtering, sorting and selection, with `-Title`, `-PassThru`, `-OutputMode None|Single|Multiple` and `-Wait`. Browse mode returns nothing; selection returns the original objects. The terminal view is synchronous, so it waits until closed even without `-Wait`.

{{< cm-frame ids="gridview-fallback-120,gridview-fallback-60" script="true" >}}

Without an interactive console, the grid prints its existing table fallback with a warning and returns no selections. Existing functions and aliases need `-Force`; disabling or removing the module restores them. Constants, `AllScope` aliases and caller-local aliases that cannot be replaced safely are kept with a warning even with `-Force`. Remove or rename them in their defining scope first if you want the fallback. A working desktop command and its captures, jobs and transcripts keep PowerShell's behavior.

The .NET equivalent uses `Grid`, the renderer behind `Show-ConsoleGrid`:

```csharp
using ConsoleMonster.Interactive;

Grid grid = new Grid("Choose nodes") { OutputMode = GridSelection.Single };
grid.AddColumn("Name").AddRow("api-east", "api-east");
GridResult selected = grid.Show();
```

## Views that ship with a module

A module can describe how its objects look without depending on ConsoleMonster: a `<ModuleName>.consoleviews.psd1` file beside its manifest names the columns, their presenters and the grouping, as plain data. ConsoleMonster reads it for loaded modules when formatting is on, and `Show-ConsoleTable` and the exports use the same columns.

{{< cm-demo id="module-views" heading="h3" >}}

For a command without a view, yours, another module's or a built-in one, [Restyle any command's output](/guides/restyle-commands/) walks through registering one: the type name, the columns and presenters, a summary line and keeping it.

## Reports and the clipboard

`Export-ConsoleFrame -Format Markdown`, `Html` or `FencedText` creates a document from ordinary objects or widgets, including inside scripts and on Windows PowerShell 5.1. `Export-ConsoleFrame -Format Markdown | Set-Clipboard` puts the document on the clipboard:

```powershell
Import-Module ConsoleMonster

Get-Service | Select-Object -First 8 Name, Status, StartType | New-ConsoleTable | Export-ConsoleFrame -Format Markdown | Set-Content -Path ./services.md -Encoding UTF8
Get-Service | Select-Object -First 8 Name, Status, StartType | Export-ConsoleFrame -Format Markdown | Set-Clipboard
```

[Coming from Format-Table](/guides/format-table/) shows the Markdown a table becomes.

## Warning, verbose and debug messages

The default `Streams` feature gives PowerShell's own warning, verbose and debug messages the theme's colours on Windows PowerShell 5.1 and PowerShell 7. Their `WARNING:` and `VERBOSE:` prefixes, preferences and redirection stay PowerShell's; [Help, errors and progress](/docs/help-and-defaults/#warning-verbose-and-debug-colours) has the details.

{{< cm-frame ids="streams-ps5,streams-ps7" caption="Before and after `-Include Streams`; the light-theme runs show when the site is light." >}}

## More from the same switch

- `Enable-ConsoleFormatting` adds default output, Format commands, the [error view and themed stream colours](/docs/help-and-defaults/), and ReadLine colours. `-Include Progress` adds the progress area. The native `Write-Progress` adapter requires a separate opt-in.
- `Register-ConsoleView` adds your own types in the session, including columns computed by script blocks; `Register-ConsoleView -Path` loads a `.consoleviews.psd1` file from anywhere.
- `Show-ConsoleTable`, `Show-Console -View List` and `Show-Console -View Tree` give the same views on demand, with column options; see [Coming from Format-Table](/guides/format-table/).
