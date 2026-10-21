# ConsoleMonster.Hosting

A live console dashboard for .NET generic host applications. It depends only on
`Microsoft.Extensions.Hosting.Abstractions` and ConsoleMonster.

```csharp
using ConsoleMonster.Extensions.Logging;
using ConsoleMonster.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();            // the default console logger would print over the view
builder.Logging.AddConsoleMonster();
builder.Services.AddHostedService<QueueWorker>();
builder.Services.AddHostedService<CacheWarmer>();
builder.Services.AddConsoleDashboard(dashboard => dashboard.Title = "Orders");   // after AddHostedService
await builder.Build().RunAsync();
```

```text
 Orders · Production                                                  ctrl+c stops · 00:01:12
 ⠹ Queue        running      1m 10s  42 messages waiting
 ✓ CacheWarmer  completed       58s
 ✗ Reports      failed          12s  InvalidOperationException: the template folder is missing
 14:05:12        Queue  picked up order 1042
 14:05:13 ▴ WARN Queue  payment slow for order 1042
```

## What it shows

- A header with the title (default: the application name), the environment and the elapsed time.
- One line per hosted service registered before `AddConsoleDashboard`:
  - its state: waiting, starting, running, stopping, stopped, completed or failed;
  - elapsed time from the beginning of its `StartAsync` to its own stop, completion or failure;
  - the failure message when it fails.
  For a `BackgroundService`, the line also follows its `ExecuteAsync`: it shows completed when the method returns and
  failed when it throws.
- Your own entries through `IConsoleDashboard`, which you can inject anywhere:
  - `SetStatus("Import", ServiceState.Running, "file 3 of 10")`;
  - `SetDetail(...)`;
  - `Log(...)`;
  - `Snapshot()`.
- A log region. `ConsoleMonster.Extensions.Logging` writes into it while the dashboard shows, so log lines never print
  over the view. Remove the default console logger (`builder.Logging.ClearProviders()`) before adding it. Entries with an
  exception print above the dashboard instead, with their details, so they stay in the scrollback.

Hosted-service elapsed time uses a monotonic clock and includes startup and shutdown work. It freezes at that
service's first terminal outcome, so later host shutdown and wall-clock adjustments do not change the final duration.
Application-reported entries continue to show time in their current state; `ServiceStatus.Since` remains the timestamp
of the latest state change.

Only the hosted services the dashboard draws are wrapped, and the wrappers forward every call. A `BackgroundService`
stays a `BackgroundService` for the host, which logs a failure and applies `BackgroundServiceExceptionBehavior` as
usual. Lifecycle callbacks (`IHostedLifecycleService`) still reach the service. Where the dashboard won't draw, the
services are left as registered.

When the host stops, the dashboard stops last and leaves its final state in the scrollback (`KeepFinalState`).

## When it stays out of the way

With `Mode = Auto` (the default), the dashboard draws only on an interactive terminal. It draws nothing when:

- the output is redirected, or the app runs in CI or a container log;
- the process is not user-interactive (a Windows service);
- the app runs as a daemon without a terminal.

In those cases the host and its logging run exactly as they would without it. `Always` draws whenever the terminal can,
and `Never` only tracks the statuses, for `Snapshot()`. Ctrl+C stays with the host: it stops the application, and the
dashboard ends with it.

## Options

| Option | Default | Meaning |
| --- | --- | --- |
| `Title` | application name | The header title. |
| `Mode` | `Auto` | `Auto`, `Always` or `Never`. |
| `ShowServices` | `true` | Lists the hosted services and your entries. |
| `ShowLog` | `true` | Shows the log region. |
| `LogHeight` | `8` | Rows of the log region. |
| `KeepFinalState` | `true` | Leaves the last frame in the scrollback. |
| `ServiceName` | type name | Names a hosted service from its type. |

The package works with trimming and native AOT on .NET 8 and later.
