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

`Watch-ConsoleLog` follows a log file or a Windows event log as lines complete, with level colours, a source timestamp column and a history you can scroll back through while new lines keep arriving.

## Try it

Save the [synthetic log](/examples/tails/synthetic.log) (16 invented entries, no real service data) next to the program. Both programs read it for one poll and stop, so they also run headless; drop `-Count` (or `Count`) to keep following.

{{< cm-try key="tails" scenarios="Follow a file:tails-following-120|Pause and resume:tails-paused-120,tails-resumed-120" project="LogTail" file="file.ps1" caption="Following: the newest entries with the footer keys. Pause and resume: PgUp pauses on older entries, End returns to the newest. Windows PowerShell 5.1 and PowerShell 7 run the same script and are checked against the C# result." >}}

## How it works

The tail reads only completed lines, recognises each line's timestamp and level, and posts the entries to a live view; the original lines are kept for filtering and plain output.

- **Columns.** The live view shows the timestamp and level once, then the message, without the leading timestamp and level fields. Source timestamps use `HH:mm:ss`, with `yyyy-MM-dd` added when the retained history spans calendar days; `-TimestampFormat 'yyyy-MM-dd HH:mm:ss zzz'` (`LogTailOptions.TimestampFormat` and `LogTailView.TimestampFormat` in C#) sets an explicit format. An unknown source timestamp stays blank: no current time is invented.
- **Pausing.** Up, PgUp and Home pause following so you can read older entries. The footer turns to the warning colour and counts the lines that arrive meanwhile; End resets the count and returns to the newest entries. Discarded history appears only when there is some. Long messages are clipped to the terminal width.
- **In your own view.** The Pause and resume program embeds `LogTailView` in a `LiveSession`: it polls the file, passes keys through `Navigate`, and `Post` queues entries for the render thread. On this page the keys (PgUp, End, q) are scripted; when you run it, they are yours.

| Key | Action |
| --- | --- |
| Up / PgUp / Home | Move toward older history and pause following. |
| Down / PgDn | Move toward newer history; reaching the newest resumes following. |
| End | Resume following at the newest entries. |
| q / Escape / Ctrl+C | Quit and restore the terminal. |

## Select and highlight literal text

`-Filter` selects lines containing a literal, ordinal case-insensitive substring. `-Level` keeps lines at a level or worse: `-Level Warn` keeps warnings and errors. `-Highlight` emphasizes a literal substring in the live view. None of them accepts a regular expression. Highlighting never changes plain output.

```powershell
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Filter ERROR -Count 1 -NoLive
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Highlight DATABASE
```

The live renderer shows timestamp and level columns once, followed by the message. It omits recognized leading timestamp and level fields from the display message, while filtering and plain output retain the complete original line. At compact widths the source time uses `HH:mm:ss`; wider views include the date, milliseconds and offset. Unknown source timestamps leave the timestamp column blank; no current-time timestamp is invented.

## Follow a Windows event log

On Windows, select a local log your account can read:

```powershell
Watch-ConsoleLog -LogName Application -Tail 20 -Highlight database
```

Each event shows its provider in a tag column, in a colour that stays the same for that provider. To follow only problems:

```powershell
Watch-ConsoleLog -LogName System -Level Warn
```

This sample uses the platform's `Get-WinEvent` command. It is documentation only: the generated evidence on this page reads the synthetic file, without querying Windows logs.

## Polling, retention and plain output

`-Tail` defaults to **10** initial completed lines or events. `-Tail 0` starts with subsequent entries. `-Count` defaults to **0**, following until quit or cancellation; it counts source polls, including empty polls. `-Count 1` is a snapshot, not a one-line limit. `-PollIntervalSeconds` defaults to **0.25 seconds**.

`-Capacity` defaults to **1000 retained history entries**. Updates use the existing live session's unbounded queue. Accepted updates are applied before history eviction; posts after shutdown follow the session's normal dropped-update rule. Capacity limits history after application, rather than the entire memory footprint. Scrolling pauses the viewport, not ingestion; eviction can remove the oldest retained history.

`-Plain`, piping, redirection and CI use raw line output and never acquire input. Timestamp and level prefixes and markup-like characters remain unchanged:

```powershell
Watch-ConsoleLog -Path ./synthetic.log -Tail 16 -Count 1 | Set-Content ./snapshot.txt
```

The core `ConsoleLogTail.Run` invokes its reader synchronously on the caller's thread. The caller owns the reader; each read must return promptly. `LogTailOptions` exposes `Profile`, `Theme`, borrowed `Output` and `Input`, and a `WriteLine` callback for raw text in plain mode. `LogTailView` owns its `LogRegion`, which handles `ShowTimestamp`, `HighlightText` and `ScrollOffset`.

File tails recognize completed CR/LF/CRLF lines and UTF-8/UTF-16/UTF-32 byte-order marks. Without a BOM, `-Encoding` chooses the file fallback, defaulting to `utf8`; a recognized BOM takes precedence. The accepted names match `Write-ConsoleText`: `unknown`, `string`, `unicode`, `bigendianunicode`, `utf8`, `utf8BOM`, `utf8NoBOM`, `utf32`, `ascii`, `default`, and `oem`. This parameter applies only to `-Path`.

```powershell
Watch-ConsoleLog -Path ./legacy.log -Encoding unicode -Count 1 -NoLive
```

Partial lines wait for their terminator and can use unbounded memory. Observed rotation or truncation restarts reading at the path; unread data in the old renamed file is not drained. An unobserved truncate/regrow with identical checkpoint bytes can look like an append. File identity supports Windows and x64/arm64 Linux/macOS.

Initialization scans the current file to find the requested completed history and retain any partial line, including with `-Tail 0`. Initial work grows with file size. Reads run synchronously, so cancellation is observed after an in-progress source read returns.

## Related

Put a tail beside other widgets in a [live session](/docs/live-sessions/), or use the [logging provider](/docs/logging/) to send application events straight to a dashboard.
