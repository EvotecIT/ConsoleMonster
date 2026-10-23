---
title: Logging
description: A Microsoft.Extensions.Logging provider with ConsoleMonster's level tags, scopes and exceptions, and plain lines when output is redirected.
slug: logging
order: 12
group: apps
card: logging
apis: [ILogger]
summary: "A Microsoft.Extensions.Logging provider with ConsoleMonster's level tags."
---

Add the provider to an existing project:

```terminal
dotnet add package ConsoleMonster.Extensions.Logging
```

The provider depends only on `Microsoft.Extensions.Logging.Abstractions` and ConsoleMonster, and works with trimming and native AOT on .NET 8 and later.

{{< cm-demo id="logging" heading="h2" >}}

## What it writes

- The level tag and colour that `Write-ConsoleLog` and the dashboard log region use: `✗ ERR` for Error and Critical, `▴ WARN` for Warning, `DBG` and `VERB` muted for Debug and Trace, and no tag for Information.
- A timestamp (`TimestampFormat`, default `HH:mm:ss`; null leaves it out), then the category (the last part by default, `Full` or `None`) and the scopes in brackets.
- An exception's type and message under the entry, with up to `MaxExceptionLines` lines of details.
- While a ConsoleMonster dashboard or [`AddConsoleDashboard`](/docs/hosting/) shows, entries go into its log region, so they never print over the view.
- With output redirected, plain lines without escape codes.

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

Without dependency injection, use `loggerFactory.AddConsoleMonster()`. The provider is added once: a second `AddConsoleMonster` keeps the first call's options. It is thread-safe.

## Related

Show the same entries inside a [hosting dashboard](/docs/hosting/), or follow a log file with [live log tails](/docs/live-tails/).
