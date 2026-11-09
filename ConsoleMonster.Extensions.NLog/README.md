# ConsoleMonster.Extensions.NLog

A native NLog target for ConsoleMonster's level tags, structured values and exception view. It depends on NLog
and ConsoleMonster; `Microsoft.Extensions.Logging` is not required.

```bash
dotnet add package ConsoleMonster.Extensions.NLog
```

```csharp
using ConsoleMonster.Extensions.NLog;
using NLog;
using NLog.Config;

var configuration = new LoggingConfiguration();
var target = new ConsoleMonsterTarget { Name = "consolemonster", TimestampFormat = "HH:mm:ss" };
configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, target);

using var factory = new LogFactory { Configuration = configuration };
var log = factory.GetLogger("Shop.Orders");
log.Info("Order {OrderId} for {Customer} totals {Total}", 1042, "Ada", 42.50m);
```

The logger name appears as the shortened category `Orders`. String values in the template print without quotes;
numbers and dates are highlighted. Event properties not used in the template follow as muted `key=value` fields.
NLog captures such as `{@Payload}` and `{$Payload}` retain their native serialization and string rendering.
Events remain native NLog events, so other targets still receive their structured properties.

## XML configuration

Register the assembly explicitly in `NLog.config`. The target's `name` is NLog's inherited `Name` property,
used by the logging rules in both XML and programmatic configuration.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <extensions>
    <add assembly="ConsoleMonster.Extensions.NLog" />
  </extensions>
  <targets>
    <target xsi:type="ConsoleMonster" name="consolemonster"
            timestampFormat="HH:mm:ss" includeCategory="true"
            maxExceptionLines="12" logRegion="$log" />
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" writeTo="consolemonster" />
  </rules>
</nlog>
```

## Options

NLog's rules and filters select the events written to the target.

| Property | Default | Meaning |
| --- | --- | --- |
| `TimestampFormat` | `HH:mm:ss` | A .NET date format for the event timestamp; null or `""` omits it. |
| `UseUtcTimestamp` | `false` | Show the event timestamp in UTC instead of local time. |
| `IncludeCategory` | `true` | Show the last part of the logger name. |
| `MaxExceptionLines` | `12` | Bound the exception details; 0 shows only the type and message. |
| `LogRegion` | `$log` | Post entries to this region when it exists; null or `""` always prints. |
| `Profile` | `null` | Use the current terminal profile, or supply a `TerminalProfile` in code. |
| `Writer` | `null` | Use the current terminal writer, or supply an `ITerminalWriter` in code. |

In XML, use `timestampFormat=""` to omit timestamps and `logRegion=""` to always print.
Exceptions render through ConsoleMonster's `ExceptionView`. During a live session, entries print above the view
unless the configured log region exists, in which case they post to that region. Redirected output is plain,
append-only text without escape codes.

The package targets .NET Standard 2.0, .NET Framework 4.7.2, .NET 8 and .NET 10, using NLog 5.0.0 as its minimum API baseline.

[Logging](https://consolemonster.dev/docs/logging/) · [ConsoleMonster](https://consolemonster.dev/)
