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

Reports give the same kind of number the same look everywhere: a count, a quota, a comparison and a share of a whole each have one form, so a reader learns them once and reads every report the same way.

{{< cm-frame id="server-estate-frame" script="true" caption="The server estate summary: facts after the title, a composition bar and a ratio as meters, and the needs-attention list." >}}

## Facts

Facts are bold counts with a label. A status fact uses the same colour for the count and its word; a neutral fact steps back in `Muted`. Facts are separated by middle dots, or ` | ` in ASCII. The summaries under the module's default views use the same widget.

<div class="cm-lang" data-lang="powershell">

```powershell
Import-Module ConsoleMonster

$facts = @(
    [pscustomobject]@{ Value = 3; Label = 'passed'; Role = 'Pass' }
    [pscustomobject]@{ Value = 1; Label = 'warning'; Role = 'Warn' }
    [pscustomobject]@{ Value = 5; Label = 'checks' }
)
$facts | New-ConsoleFacts | Show-Console
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Write(new ViewFacts {
    Facts = {
        new ViewFact(3, "passed", role: ThemeToken.Pass),
        new ViewFact(1, "warning", role: ThemeToken.Warn),
        new ViewFact(5, "checks")
    }
});
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;

Ui.Facts().Add(3, "passed", ThemeToken.Pass).Add(1, "warning", ThemeToken.Warn)
    .Add(5, "checks").Show();
```

</div>

## Bars

- **Quota.** A Bar chart with `-Maximum 100` draws a Line bar on a `Subtle` track with a percentage on the right. With any other maximum it shows value/whole.
- **Comparison.** Without `-Maximum`, bars compare their values as solid blocks without a track.
- **Composition.** A composition bar keeps the healthy share neutral and colours only the problems. Adjacent identical fills are separated by one cell when there is room, and the legend fits its columns to their labels before wrapping.

Progress bars and score gauges use the same quota form.

## Trends

`New-ConsoleChart -Type Sparkline -Values 4,3,8 -Labels '00:00','12:00','23:00'` adds an axis and the peak value under the trend. Without `-Labels` it stays a compact one-line trend. A chart inside a titled panel drops a caption that would repeat the title.

## Height and alignment

Printed reports keep their natural height. In a live Dashboard, rows share the remaining screen space, and an explicit Fill row takes precedence over the last section. Table columns that hold only numbers align right, even when the values arrive as strings; an explicit alignment wins.

## Next

- [Charts from the pipeline](/docs/pipeline-charts/): bar, line and sparkline charts from object properties.
- [Dashboard frames](/docs/dashboard-frames/): facts, meters and attention items in one summary.
- [Markdown display and export](/docs/markdown/): documents and exports built from the same widgets.
