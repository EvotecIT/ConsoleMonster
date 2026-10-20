# ConsoleMonster.Extensions.Logging

A `Microsoft.Extensions.Logging` provider that writes log entries the ConsoleMonster way. It depends only on
`Microsoft.Extensions.Logging.Abstractions` and ConsoleMonster.

```csharp
using ConsoleMonster.Extensions.Logging;
using Microsoft.Extensions.Logging;

using ILoggerFactory factory = LoggerFactory.Create(logging => logging.AddConsoleMonster());
ILogger log = factory.CreateLogger("Shop.Orders");

using (log.BeginScope("order {Id}", 1042)) {
    log.LogInformation("Payment accepted");
    log.LogWarning("Stock is low for {Sku}", "A-17");
}
```

```text
14:05:12        Orders (order 1042)  Payment accepted
14:05:12 ▴ WARN Orders (order 1042)  Stock is low for A-17
```

## What it does

- Each entry gets the level tag and colour that `Write-ConsoleLog` and the dashboard log region use:
  - `✗ ERR` for Error and Critical;
  - `▴ WARN` for Warning;
  - `DBG` and `VERB` for Debug and Trace, with the text muted;
  - no tag for Information.
- A timestamp comes first (`TimestampFormat`, default `HH:mm:ss`; null leaves it out; `UseUtcTimestamp`).
- Then the category: the last part by default, `Full`, or `None`. Scopes follow in brackets (`IncludeScopes`).
- The event id is shown only when asked for (`IncludeEventId`).
- An exception's type and message print under the entry, with up to `MaxExceptionLines` lines of details. While a
  dashboard shows, such entries print above it rather than into its log, so the details stay in the scrollback.
- While a live session with a log region runs, such as a ConsoleMonster dashboard or `ConsoleMonster.Hosting`'s
  `AddConsoleDashboard`, entries go into that region (`LogRegion`, default `$log`), so they never print over the view.
- With output redirected (CI, a file, a pipe), entries are plain lines without escape codes. Colours and glyphs follow
  the terminal, as everywhere in ConsoleMonster.

## Options

| Option | Default | Meaning |
| --- | --- | --- |
| `MinimumLevel` | `Trace` | The lowest level written; the logging builder's filters apply first. |
| `TimestampFormat` | `HH:mm:ss` | A .NET date format, or null for no timestamp. |
| `UseUtcTimestamp` | `false` | UTC instead of local time. |
| `Category` | `Short` | `Short`, `Full` or `None`. |
| `IncludeScopes` | `true` | Shows the scopes after the category. |
| `IncludeEventId` | `false` | Shows the event id. |
| `MaxExceptionLines` | `12` | Lines of exception details; 0 for the type and message only. |
| `LogRegion` | `$log` | The live log region to write into while one runs; null always prints. |
| `Profile`, `Writer` | current terminal | Where and for which terminal lines are written (tests, files). |

The provider is added once: a second `AddConsoleMonster` keeps the first call's options. Without dependency
injection, use `loggerFactory.AddConsoleMonster()`. The provider is thread-safe and works with
trimming and native AOT on .NET 8 and later.
