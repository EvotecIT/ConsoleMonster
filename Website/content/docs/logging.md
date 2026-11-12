---
title: Logging
description: Microsoft.Extensions.Logging, Serilog and NLog adapters with ConsoleMonster's level tags, structured values and exceptions, and plain redirected output.
slug: logging
order: 44
group: apps
card: logging
apis: [ILogger]
summary: "ConsoleMonster output for Microsoft.Extensions.Logging, Serilog and NLog."
---

Add the provider to an existing project:

```terminal
dotnet add package ConsoleMonster.Extensions.Logging
```

The provider depends only on `Microsoft.Extensions.Logging.Abstractions` and ConsoleMonster, and works with trimming and native AOT on .NET 8 and later.

{{< cm-demo id="logging" heading="h2" >}}

## What it writes

- The level tag and colour that `Write-ConsoleLogEntry` and the dashboard log region use: `◆ CRIT` for Critical in the Critical style, `✗ ERR` for Error, `▲ WARN` for Warning, `DBG` and `VERB` muted for Debug and Trace, and no tag for Information.
- With Words markers, the level appears once as `[FAIL]`, `[WARN]` or `[OK]`. ASCII logs retain their short level words.
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

## Native Serilog and NLog

These adapters use the logging framework's own events and event timestamps. Neither requires `Microsoft.Extensions.Logging`.
Both target .NET Standard 2.0, .NET Framework 4.7.2, .NET 8 and .NET 10.
All three logging adapters share ConsoleMonster's internal formatter; the core library remains dependency-free.

### Serilog

```terminal
dotnet add package ConsoleMonster.Extensions.Serilog
```

```csharp
using Serilog;

using var logger = new LoggerConfiguration()
    .WriteTo.ConsoleMonster(options => options.TimestampFormat = "HH:mm:ss")
    .CreateLogger();
var log = logger.ForContext("SourceContext", "Shop.Orders");
log.Information("Order {OrderId} for {Customer}", 1042, "Ada");
```

The extension is in the `Serilog` namespace. The sink's `restrictedToMinimumLevel` defaults to `Verbose`;
Serilog's own minimum level and filters apply first.

### NLog

```terminal
dotnet add package ConsoleMonster.Extensions.NLog
```

```csharp
using ConsoleMonster.Extensions.NLog;
using NLog;
using NLog.Config;

var configuration = new LoggingConfiguration();
var target = new ConsoleMonsterTarget { Name = "consolemonster" };
configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

using var factory = new LogFactory { Configuration = configuration };
var log = factory.GetLogger("Shop.Orders");
log.Info("Order {OrderId} for {Customer}", 1042, "Ada");
```

For XML configuration, register `<add assembly="ConsoleMonster.Extensions.NLog" />` inside `<extensions>`,
then use a target with `xsi:type="ConsoleMonster"`. NLog's usual `name` and rules select the target.

### What the native adapters write

The category is the last part of Serilog's `SourceContext` or NLog's logger name (`Orders` here).
String values in the template print without quotes; numbers and dates are highlighted. Unused properties follow as
muted `key=value` fields, and Serilog structures use compact JSON. Other sinks and targets retain the native structured events.

Both adapters accept `TimestampFormat` (`HH:mm:ss`), `UseUtcTimestamp` (`false`), `IncludeCategory` (`true`),
`MaxExceptionLines` (`12`), `LogRegion` (`$log`), and optional `Profile` and `Writer` overrides.
Set `TimestampFormat` to null to omit timestamps, or `LogRegion` to null to always print.
NLog also accepts empty strings for these two settings, including `timestampFormat=""` and `logRegion=""` in XML.
Exceptions use `ExceptionView`. During a live session, entries print above the view unless the configured log region
exists, in which case they post there. Redirected output stays plain and append-only.

## Next

- [dotnet new template](/docs/template/): an app with logging and the hosting dashboard wired up.
- [Hosting dashboard](/docs/hosting/): show the same entries inside a live dashboard.
- [Live log tails](/docs/live-tails/): follow a log file instead.
