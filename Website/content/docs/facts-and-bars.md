---
title: Facts, bars and hierarchy
description: Give counts, quotas, comparisons and composition consistent meaning in terminal reports.
slug: facts-and-bars
order: 16
group: apps
summary: "Shared report facts, meaningful bar shapes and labelled trends."
apis: [New-ConsoleFacts, Ui.Facts, New-ConsoleChart, New-ConsoleProgress]
chips: [Reports, Charts, PowerShell, C#]
card: dashboard-frame-110
---

Report facts use bold counts and the same status colour for the count and its word. Neutral facts step back in Muted.
Separators are middle dots, or ` | ` in ASCII. Default-view summaries use the same widget.

```powershell
New-ConsoleFacts -Content {
    @{ Value = 3; Label = 'passed'; Role = 'Pass' }
    @{ Value = 1; Label = 'warning'; Role = 'Warn' }
    @{ Value = 5; Label = 'checks' }
} | Show-Console
```

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;

Ui.Facts().Add(3, "passed", ThemeToken.Pass).Add(1, "warning", ThemeToken.Warn)
    .Add(5, "checks").Show();
```

A Bar chart with `-Maximum 100` shows a quota as a Line with a Subtle track and a percentage on the right. With a
different whole it shows value/whole. Without Maximum, bars compare values using Block without a track. Progress
and score gauges use the same quota language. Composition bars keep the healthy share neutral and colour problems.

Use `New-ConsoleChart -Type Sparkline -Values 4,3,8 -Labels '00:00','12:00','23:00'` for an axis and peak value.
Without Labels, the compact trend keeps its existing appearance. A chart in a titled panel drops a repeated caption.

Printed reports keep their natural height. Live Dashboard rows use the remaining screen space, with an explicit
Fill row taking precedence over the last section. App layouts keep their existing sizing. Numeric-only table
columns align right even when values arrive as strings; an explicit alignment wins.

Try the [readiness report](/demos/readiness-report/), [event log summary](/demos/event-log-summary/) and
[dashboard frame](/docs/dashboard-frames/) for complete programs in all three styles.
