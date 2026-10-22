# Live log tails

`Watch-ConsoleLog` follows a file or a local Windows event log. It detects log levels, shows source timestamps in a fixed column, and keeps a scrollable history. Scrolling pauses following; End returns to the newest entries. Redirected output and `-Plain` return the original lines as strings without opening a live screen or reading keyboard input.

## PowerShell

Install and import the module, then follow an existing log file:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Watch-ConsoleLog -Path ./service.log -Highlight database
```

For a finite snapshot, use `-Count 1`. This counts one **source poll**, not one line. Every matching completed line available in that poll is returned.

```powershell
Watch-ConsoleLog -Path ./service.log -Tail 50 -Filter ERROR -Count 1 -Plain
```

`-Path` is a single literal FileSystem path; wildcards are not expanded. `-Filter` and `-Highlight` are literal, ordinal case-insensitive substrings, not regular expressions. A filter selects lines; a highlight changes only live presentation. Pipes and redirects preserve raw text, including timestamp and level prefixes and markup-like characters.

On Windows, `-LogName` selects a local event log through `Get-WinEvent`:

```powershell
# Windows only; use an existing log your account can read.
Watch-ConsoleLog -LogName Application -Tail 20 -Highlight database
```

The Windows event-log command is a usage example. The website's captured evidence uses only the supplied synthetic file and does not query Windows logs.

| Option | Default | Meaning |
| --- | --- | --- |
| `-Tail` | `10` | Completed history lines or events loaded initially; `0` starts with subsequent entries. |
| `-Encoding` | `utf8` | File fallback encoding; a recognized BOM takes precedence. |
| `-Count` | `0` | Poll iterations; `0` follows until quit or cancellation, `1` processes one snapshot. |
| `-Interval` | `0.25` | Seconds between source polls. |
| `-Capacity` | `1000` | Maximum retained history entries in the live view. |
| `-Filter` | Empty | Required literal substring in each original line. |
| `-Highlight` | Empty | Literal substring emphasized in the live view. |
| `-Plain` | Off | Force original-line output and never acquire input. |

Capacity limits applied history. Updates use the existing live session's unbounded queue; capacity is not a bound on all memory used by a busy producer. Accepted updates are applied before history eviction, and posts after shutdown follow the session's normal dropped-update rule. The footer reports discarded history entries.

## Navigation

| Key | Result |
| --- | --- |
| Up / PgUp / Home | Scroll toward older entries and pause following. |
| Down / PgDn | Scroll toward newer entries; reaching the newest resumes following. |
| End | Return to the newest entries and resume following. |
| q / Escape / Ctrl+C | End the tail and restore the terminal. |

New entries still arrive while paused. The viewport stays on its retained history until navigation resumes following; eviction can remove the oldest history. Long messages are clipped to the available row width rather than wrapped.

## C#

Create a console application and add the package:

```text
dotnet new console -n LogTail
cd LogTail
dotnet add package ConsoleMonster
```

Replace `Program.cs` with this complete program. Place an existing `service.log` in the working directory before running it.

```csharp
using ConsoleMonster.Live.Tails;

using var source = new FileLogTail("service.log", initialLineCount: 10);
ConsoleLogTail.Run(source.ReadAvailable, new LogTailOptions {
    Highlight = "database"
});
```

Set `Count = 1` for a finite snapshot. `ConsoleLogTail.Run` calls its reader synchronously on the calling thread, so readers must return promptly. The caller owns the reader and its resources. `LogTailOptions` also accepts `Profile`, `Theme`, borrowed `Output`, and borrowed `Input`; `Plain = true` never acquires input. `WriteLine` receives the raw original text in plain mode, without a line terminator.

To embed the view in an application, construct `LogTailView`, include it in a `LiveSession`, send entries with `view.Post(dispatcher, entry)`, and forward input with `view.Navigate(dispatcher, input)`. Both methods queue work through the dispatcher. The view owns its `LogRegion`; that renderer supplies `ShowTimestamp`, `HighlightText`, and `ScrollOffset`. Keep source polling and input ownership in the application.

## File behavior and limits

`FileLogTail` reads completed lines incrementally, using UTF-8 by default and recognizing UTF-8/UTF-16/UTF-32 byte-order marks. Decoder state survives split writes. CR, LF, and CRLF terminate lines; unfinished text is retained until a terminator arrives. An unfinished line can use unbounded memory.

The initial read scans the current file to find the requested completed history and retain any unfinished line. Its cost grows with file size, even with `-Tail 0`. Source reads run synchronously; cancellation is observed after an in-progress read returns.

For a file without a BOM, PowerShell's `-Encoding` accepts the same names as `Write-ConsoleText`: `unknown`, `string`, `unicode`, `bigendianunicode`, `utf8`, `utf8BOM`, `utf8NoBOM`, `utf32`, `ascii`, `default`, and `oem`. The option applies only to `-Path`. A recognized BOM overrides the fallback.

The reader follows the path across observed replacement, shrinkage, or changed checkpoint bytes. It does not drain unread bytes from a renamed old file. A truncate/regrow between polls with identical checkpoint bytes cannot be distinguished from an append, and concurrent rewrites are not atomic snapshots. A missing path after opening is retried on subsequent polls; other I/O failures propagate. File identity supports Windows and x64/arm64 Linux/macOS.

Source timestamps are metadata, not capture-time clocks; unknown timestamps leave the timestamp column blank. The live message omits recognized leading timestamp and level fields because they already have their own columns. At narrower widths the timestamp column shows the source time as `HH:mm:ss`; wider views include the date, milliseconds and offset. Original lines remain intact for filtering and plain output.

See the [website guide](https://consolemonster.dev/docs/live-tails/) for downloadable synthetic examples and generated following, paused, and resumed frames. For larger compositions, see the [live sessions guide](https://consolemonster.dev/docs/live-sessions/).
