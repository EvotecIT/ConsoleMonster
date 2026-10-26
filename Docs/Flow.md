# Flow strips

A flow explains a short sequence of hops, including the connection that failed.
`Flow` draws directly into terminal cells and composes with panels, dashboards,
table cells (use `TableCell.FromValue(flow)` in C#), recordings and frame exports. It has no chart or graph dependency.

```powershell
$nodes = @(
    [pscustomobject] @{ Kind = 'Node'; Key = 'client'; Label = 'client'; Status = 'Pass' }
    [pscustomobject] @{ Kind = 'Node'; Key = 'issuer'; Label = 'issuer'; Status = 'Fail' }
)
$flow = $nodes | New-ConsoleFlow -WidgetId chain
$flow | Add-ConsoleItem {
    [pscustomobject] @{ Kind = 'Edge'; Key = 'trust'; From = 'client'; To = 'issuer'; Label = 'issuer'; Status = 'Fail'; Cut = $true }
    [pscustomobject] @{ Kind = 'Branch'; From = 'issuer'; Label = 'expired 2026-09-30'; Status = 'Fail' }
}
New-ConsolePanel -Title 'Certificate path' { $flow } | Show-Console
```

Typed C# items and pipeline records use the same properties. Records need `Kind` set to
`Node`, `Edge` or `Branch`. Add nodes in strip order before their edges and branch.
Keys identify items; adding the same key replaces it in place. An edge connects
adjacent nodes. Omitted edges are plain arrows. `Branch` replaces the single
branch, and a null branch removes it. A branch can end below its source or return
to a later node through the `To` field. Branches do not contain other branches.

The equivalent C# builder uses immutable records:

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

var flow = new Flow("chain")
    .Add(new FlowNode("client", "client", StatusKind.Pass))
    .Add(new FlowNode("issuer", "issuer", StatusKind.Fail))
    .Add(new FlowEdge("trust", "client", "issuer", "issuer", StatusKind.Fail, Cut: true))
    .Branch(new FlowBranch("issuer", "expired 2026-09-30", Status: StatusKind.Fail));
Terminal.Write(flow);
```

Labels are literal text. Statuses use the shared Pass, Warn, Fail, Pending and
Skip vocabulary, including its normal synonyms. Every status has a glyph as well
as a colour. A cut arrow uses `✗`, or `x` in the ASCII tier. Unicode labels use
terminal cell widths, including CJK characters and emoji.

The strip becomes vertical when it cannot fit horizontally. Labels wrap at word
boundaries. An indivisible word wider than the available node becomes `...`;
a word and a grapheme are never split across a border.

In a dashboard, `Update-Console chain -NodeKey issuer -Status Fail`
changes a node. `-EdgeKey trust -Status Fail -Cut` severs an edge; `-Cut:$false`
reconnects it. Omitting `-Cut` preserves connectivity. The C# equivalent is
`flow.Handle(session).Node("issuer", StatusKind.Fail)` or
`flow.Handle(session).Edge("trust", StatusKind.Fail, cut: true)`.

The [TLS verdict showcase](../Website/examples/gallery/tls-verdict/tls-verdict.ps1)
adds a numbered `New-ConsolePhases -StepCounter` ledger, paired context with
`New-ConsoleKeyValue -Columns 2`, and a double-bordered `New-ConsolePanel -Verdict`. A verdict with `-Status`
includes the theme's status glyph in its centred title, including in ASCII and
without colour.
The numbered ledger gives labels the full available width, with wrapped lines
indented under the label. A result shares the row only when the whole label and
result fit; otherwise it appears below, right-aligned. In a vertical flow, a
connector-only row separates a side branch from the next edge label.
Run its two fictional outcomes with `-Scenario Healthy` or
`-Scenario ExpiredIntermediate`. Dates, duration and exit code are simulated;
the program does not contact a host or change the process exit code.

A table measures embedded widgets at the allocated column width and keeps them
when it falls back to record cards. Live events and recording replay find the
flow through the table, just as through a panel. Widget cells use their full
measured height; the table's default three-line limit applies to text cells.
