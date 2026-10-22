---
title: Live log tails
description: Follow a file or Windows event log with level colours, source timestamps, literal highlighting and a scrollable history.
slug: live-tails
order: 7
group: live
card: tails-following-120
apis: [Watch-ConsoleLog, FileLogTail, ConsoleLogTail, LogTailView]
chips: [Files, Logs, PowerShell]
summary: "Follow files and local Windows event logs; pause on scroll and resume with End."
---

`Watch-ConsoleLog` follows completed log lines with level colours and a source timestamp column. Up, PgUp and Home pause following so you can read older entries. End returns to the newest entries. The footer tells you whether the view is following or paused and how many history entries were discarded.

## Run the synthetic example

1. Install and import the module.

   ```powershell
   Install-Module ConsoleMonster -Scope CurrentUser
   Import-Module ConsoleMonster
   ```

2. Save the [synthetic log](/examples/tails/synthetic.log) and [file.ps1](/examples/tails/file.ps1) in the same folder. The log contains 16 invented entries and no real service data.

3. Run the script. It ends after one source poll, which processes all 16 completed lines. To follow later appends, run the command below without `-Count`.

   ```powershell
   ./file.ps1
   Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Highlight database
   ```

The finite script and its complete C# equivalent produce these frames. PowerShell 5.1 and 7 are captured from the same native script and checked against the C# result.

{{< cm-frame ids="tails-following-120,tails-following-60" caption="The same synthetic file at 120 and 60 columns. Long messages are clipped to the terminal width." >}}

For C#, create an application, save [file.cs](/examples/tails/file.cs) as `Program.cs`, and put `synthetic.log` in the working directory. Then run it:

```text
dotnet new console -n LogTail
cd LogTail
dotnet add package ConsoleMonster
dotnet run
```

The shown C# program uses `Count = 1`, so it is finite in a headless process too. Omit `Count` to follow until quit or cancellation. Redirected output uses plain original lines.

## Pause, inspect and resume

| Key | Action |
| --- | --- |
| Up / PgUp / Home | Move toward older history and pause following. |
| Down / PgDn | Move toward newer history; reaching the newest resumes following. |
| End | Resume following at the newest entries. |
| q / Escape / Ctrl+C | Quit and restore the terminal. |

The complete program below embeds `LogTailView` in a `LiveSession`. It polls the file and passes input through `Navigate`; `Post` queues entries for the render thread. The capture tool supplies PgUp, then End, then q. Normal use reads your keyboard.

{{< cm-frame ids="tails-paused-120,tails-paused-60" caption="PgUp moves to older rows and changes the footer to Paused. New entries continue to be retained." >}}

{{< cm-frame ids="tails-resumed-120,tails-resumed-60" script="false" caption="End restores the newest rows and the Following state." >}}

Save [navigate.cs](/examples/tails/navigate.cs) as `Program.cs` in the same application to try the embedded view. The public examples need no recording harness.

## Select and highlight literal text

`-Filter` selects lines containing a literal, ordinal case-insensitive substring. `-Highlight` emphasizes a literal substring in the live view. Neither accepts a regular expression. Highlighting never changes plain output.

```powershell
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Filter ERROR -Count 1 -Plain
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Highlight DATABASE
```

The live renderer shows timestamp and level columns once, followed by the message. It omits recognized leading timestamp and level fields from the display message, while filtering and plain output retain the complete original line. At compact widths the source time uses `HH:mm:ss`; wider views include the date, milliseconds and offset. Unknown source timestamps leave the timestamp column blank; no current-time timestamp is invented.

## Follow a Windows event log

On Windows, select a local log your account can read:

```powershell
Watch-ConsoleLog -LogName Application -Tail 20 -Highlight database
```

This sample uses the platform's `Get-WinEvent` command. It is documentation only: the generated evidence on this page reads the synthetic file, without querying Windows logs.

## Polling, retention and plain output

`-Tail` defaults to **10** initial completed lines or events. `-Tail 0` starts with subsequent entries. `-Count` defaults to **0**, following until quit or cancellation; it counts source polls, including empty polls. `-Count 1` is a snapshot, not a one-line limit. `-Interval` defaults to **0.25 seconds**.

`-Capacity` defaults to **1000 retained history entries**. Updates use the existing live session's unbounded queue. Accepted updates are applied before history eviction; posts after shutdown follow the session's normal dropped-update rule. Capacity limits history after application, rather than the entire memory footprint. Scrolling pauses the viewport, not ingestion; eviction can remove the oldest retained history.

`-Plain`, piping, redirection and CI use raw line output and never acquire input. Timestamp and level prefixes and markup-like characters remain unchanged:

```powershell
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Count 1 | Set-Content ./snapshot.txt
```

The core `ConsoleLogTail.Run` invokes its reader synchronously on the caller's thread. The caller owns the reader; each read must return promptly. `LogTailOptions` exposes `Profile`, `Theme`, borrowed `Output` and `Input`, and a `WriteLine` callback for raw text in plain mode. `LogTailView` owns its `LogRegion`, which handles `ShowTimestamp`, `HighlightText` and `ScrollOffset`.

File tails recognize completed CR/LF/CRLF lines and UTF-8/UTF-16/UTF-32 byte-order marks. Without a BOM, `-Encoding` chooses the file fallback, defaulting to `utf8`; a recognized BOM takes precedence. The accepted names match `Write-ConsoleText`: `unknown`, `string`, `unicode`, `bigendianunicode`, `utf8`, `utf8BOM`, `utf8NoBOM`, `utf32`, `ascii`, `default`, and `oem`. This parameter applies only to `-Path`.

```powershell
Watch-ConsoleLog -Path ./legacy.log -Encoding unicode -Count 1 -Plain
```

Partial lines wait for their terminator and can use unbounded memory. Observed rotation or truncation restarts reading at the path; unread data in the old renamed file is not drained. An unobserved truncate/regrow with identical checkpoint bytes can look like an append. File identity supports Windows and x64/arm64 Linux/macOS.

Initialization scans the current file to find the requested completed history and retain any partial line, including with `-Tail 0`. Initial work grows with file size. Reads run synchronously, so cancellation is observed after an in-progress source read returns.

Next, put a tail beside other widgets in a [live session](/docs/live-sessions/), or use [logging integration](/docs/logging/) to send application events directly to a dashboard.
