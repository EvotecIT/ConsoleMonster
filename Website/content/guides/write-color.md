---
title: Coming from Write-Color
description: Move Write-Color scripts to Write-ConsoleText using consistent parameter names, rich text and structured logs.
slug: write-color
card: shaping
order: 2
---

`Write-ConsoleText` covers the same message-writing jobs as `Write-Color` from [PSWriteColor](https://github.com/EvotecIT/PSWriteColor), using ConsoleMonster’s parameter vocabulary. Use the mappings below when moving a script. Both modules can be loaded in the same session.

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Write-ConsoleText -Text 'Deploying ', 'FS01', ' to ', 'Production' -Color Gray, Cyan, Gray, Yellow
```

{{< cm-frame id="write-consoletext" script="true" caption="`Write-ConsoleText` on Windows PowerShell 5.1: pieces with colours, markup, tabs, spaces, padding with a character and a gradient." >}}

## Parameters

| Write-Color | Write-ConsoleText | Notes |
|---|---|---|
| `-Text` (`-T`) | `-Text` | Pieces are literal. Rich text from `New-ConsoleText` is accepted too. |
| `-Color` (`-C`, `-ForegroundColor`, `-FGC`) | `-Color` | Also theme roles, `#RRGGBB`, `rgb()` and `r,g,b`. Extra pieces take the first colour, as in Write-Color. Without `-Color` the terminal's own colour is used; Write-Color uses White. |
| `-BackGroundColor` (`-B`, `-BGC`) | `-BackgroundColor` | Counts don't have to match: extra pieces take the first background. |
| `-StartTab` (`-Indent`) | `-StartTab` | Eight cells per tab. |
| `-StartSpaces`, `-LinesBefore`, `-LinesAfter` | same | |
| `-ShowTime`, `-DateTimeFormat` (`-DateFormat`, `-TimeFormat`) | `-ShowTimestamp`, `-TimestampFormat` | Wall-clock time. |
| `-NoNewLine` | same | Works in transcripts, jobs and the host route too. |
| `-HorizontalCenter` | same | Measured in terminal cells, so emoji and CJK centre correctly. |
| `-LogFile` (`-L`), `-LogTime` (`-LogTimeStamp`), `-LogRetry` | `-LogFile`, `-LogTimestamp`, `-LogRetry` | Appends under a file lock, with retries and warnings as in Write-Color. |
| `-Encoding` | same | A new log defaults to UTF-8 without a byte order mark; Write-Color defaults to UTF-16. Appending to an existing log keeps its encoding, so last month's Write-Color log stays UTF-16, and a contradicting `-Encoding` warns. |
| `-NoConsoleOutput` (`-HideConsole`) | `-NoConsoleOutput` | |

## Parameters that Write-ConsoleText adds

| Parameter | What it does |
|---|---|
| `-Markup`, `-Decoration`, `-Link`, `-Role`, `-Gradient` | Markup after the pieces, decorations, hyperlinks, theme roles and colour gradients. |
| `-PadLeft`, `-PadRight`, `-PadCenter`, `-PadCharacter` | Padding in terminal cells. |
| `-Wrap`, `-Width` | Wraps with a hanging indent under the time and prefix. |
| `-SplitLines` | Splits at a separator and aligns the following lines under the first. |
| `-LogFormat Text`, `Csv` or `JsonLines`, `-LogSeparator`, `-Level`, `-Category` | Structured logs. |

## Where the two behave differently

- In transcripts, each call is one line.
- In jobs and remoting, the colours travel with the output as information records.
- Redirected output is plain text.
- ConsoleColor names render as PowerShell shows them: `DarkRed` is ANSI red and `Red` is bright red, so a script looks the same as with `Write-Host -ForegroundColor`.

For a line that updates in place, use `Show-ConsoleStatusLine 'Copying ', $file -Color Gray, Cyan -Spinner -ShowElapsed` and end it with `-Complete`. Outside a real console it writes plain lines.

## Related

More rich text, from status lines to gradients, is on the [PowerShell page](/powershell/). The roles `-Role` accepts are listed under [themes](/docs/themes/).
