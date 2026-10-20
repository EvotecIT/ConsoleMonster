---
title: Hosting dashboard
description: ConsoleMonster.Hosting adds a live dashboard to a .NET generic host: each hosted service's state, failures and the log.
slug: hosting
order: 11
---

```bash
dotnet add package ConsoleMonster.Hosting
dotnet add package ConsoleMonster.Extensions.Logging
```

The dashboard depends only on `Microsoft.Extensions.Hosting.Abstractions` and ConsoleMonster.

{{< cm-demo id="hosting" heading="h2" >}}

## What it shows

- A header with the title (default: the application name), the environment and the elapsed time.
- One line per hosted service registered before `AddConsoleDashboard`, with its state (waiting, starting, running, stopping, stopped, completed or failed), how long it has been in it and the failure message when it fails. A `BackgroundService` shows completed when `ExecuteAsync` returns and failed when it throws.
- Your own entries through `IConsoleDashboard`, which you can inject anywhere: `SetStatus("Import", ServiceState.Running, "file 3 of 10")`, `SetDetail`, `Log` and `Snapshot`.
- A log region that `ConsoleMonster.Extensions.Logging` writes into while the dashboard shows. Remove the default console logger (`builder.Logging.ClearProviders()`) first, so it does not print over the view.

When the host stops, the dashboard stops last and leaves its final state in the scrollback. Ctrl+C stays with the host.

## When it stays out of the way

With `Mode = Auto` (the default) the dashboard draws only on an interactive terminal: not when output is redirected, in CI or a container log, in a Windows service or a daemon without a terminal. There the host and its logging run exactly as they would without it. `Always` draws whenever the terminal can, and `Never` only tracks the statuses for `Snapshot()`.

| Option | Default | Meaning |
| --- | --- | --- |
| `Title` | application name | The header title. |
| `Mode` | `Auto` | `Auto`, `Always` or `Never`. |
| `ShowServices` | `true` | Lists the hosted services and your entries. |
| `ShowLog` | `true` | Shows the log region. |
| `LogHeight` | `8` | Rows of the log region. |
| `KeepFinalState` | `true` | Leaves the last frame in the scrollback. |
| `ServiceName` | type name | Names a hosted service from its type. |