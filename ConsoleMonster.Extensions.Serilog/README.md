# ConsoleMonster.Extensions.Serilog

A native Serilog sink for ConsoleMonster's level tags, structured values and exception view. It depends on Serilog
and ConsoleMonster; `Microsoft.Extensions.Logging` is not required.

```bash
dotnet add package ConsoleMonster.Extensions.Serilog
```

```csharp
using Serilog;

using var logger = new LoggerConfiguration()
    .WriteTo.ConsoleMonster(options => options.TimestampFormat = "HH:mm:ss")
    .CreateLogger();
var log = logger.ForContext("SourceContext", "Shop.Orders").ForContext("Region", "eu-west");

log.Information("Order {OrderId} for {Customer} totals {Total}", 1042, "Ada", 42.50m);
```

`SourceContext` appears as the shortened category `Orders`. String values in the template print without quotes;
numbers and dates are highlighted. Properties not used in the template follow as muted `key=value` fields
(`Region=eu-west` here). Destructured values use compact JSON. Events remain native Serilog events, so other sinks
still receive their structured properties.

## Options

Pass an options callback to `WriteTo.ConsoleMonster`. Serilog's minimum level and filters apply first;
the sink's `restrictedToMinimumLevel` argument defaults to `Verbose`.

| Option | Default | Meaning |
| --- | --- | --- |
| `TimestampFormat` | `HH:mm:ss` | A .NET date format for the event timestamp; null omits it. |
| `UseUtcTimestamp` | `false` | Show the event timestamp in UTC instead of local time. |
| `IncludeCategory` | `true` | Show the last part of `SourceContext`. |
| `MaxExceptionLines` | `12` | Bound the exception details; 0 shows only the type and message. |
| `LogRegion` | `$log` | Post entries to this region when it exists; null always prints. |
| `Profile` | `null` | Use the current terminal profile, or supply a `TerminalProfile`. |
| `Writer` | `null` | Use the current terminal writer, or supply an `ITerminalWriter`. |

Exceptions render through ConsoleMonster's `ExceptionView`. During a live session, entries print above the view
unless the configured log region exists, in which case they post to that region. Redirected output is plain,
append-only text without escape codes.

For direct integration, construct `ConsoleMonsterSink` with an optional `ConsoleMonsterSinkOptions` and pass native
`LogEvent` instances to `Emit`; `Options` exposes the sink's configuration. Both types are in
`ConsoleMonster.Extensions.Serilog`; the `WriteTo.ConsoleMonster` extension is in the usual `Serilog` namespace.

The package targets .NET Standard 2.0, .NET Framework 4.7.2, .NET 8 and .NET 10, and requires Serilog 4.4.0 or later.

[Logging](https://consolemonster.dev/docs/logging/) · [ConsoleMonster](https://consolemonster.dev/)
