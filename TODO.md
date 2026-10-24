# ConsoleMonster engineering tasks

Granular tasks: validation, review follow-ups and small fixes. Decisions, the next rounds and the backlog are in
[ROADMAP.md](ROADMAP.md).

## Before the first wide 2.0 release
- Validate on Windows Server 2016 (16 colours), 2019, 2022, 2025, Server Core, VS Code, SSH (ConPTY) and the PowerShell ISE fallback.
- Owner review of the prototype screenshots.
- Finish the package signing setup before public release. The module build has a configured signer; `Build/project.build.json` leaves package signing unconfigured for local validation.
- Heat strip widget from the v1 widget list.
- Per-widget narration options for stream mode.
- Light background detection in Windows Terminal: needs an OSC 11 query that never consumes user input (today `CONSOLEMONSTER_THEME=light` is required there).
- Table v2: column sorting and filtering in live tables and horizontal scrolling as an alternative to hiding columns.
- Inspector: PowerShell format data (`*.format.ps1xml`) views as an input to column selection.

## Follow-ups from the pre-PR review (review.md, 2026-10-17)
The review's high findings and M1-M4, M6 and M7 are fixed. Still open, with the review's labels:
- M5: print-once memory grows with rows x width (50,000 rows used about 570 MB). Negotiate widths over all rows or a
  bounded sample, then render and emit in bands; or cap rows with a warning.
- L1: the width re-probe after a resize or font change overwrites explicit `CONSOLEMONSTER_*_WIDTH` values and skips
  the ambiguous-width glyph downgrade; route it through the same method as the start-up probe.
- L2: detection enables VT processing on stdout and never restores the mode the module found.
- L3: live sessions without an explicit culture use the render thread's culture; capture the caller's culture and add
  `Invoke-ConsoleDashboard -Culture`.
- L5: with `-Parallel`, host writes from the main runspace draw over the live frame; without `-Parallel`, piped input
  is ignored silently.
- L6: unbounded buffers (the live queue, the capture writer's partial line, runspace stream collections, dashboard
  output, log entry length); cap them or document the limits.
- L7: `TreeNode.Add` accepts cycles and `Tree` recurses into them (stack overflow).
- L8: `Table.Distribute` shrinks one cell per pass; subtract the needed amount in one step.
- L9: `TextWrapper.Truncate` measures the ellipsis with the default width profile instead of the given one.
- L10: a custom glyph set with empty spinner frames divides by zero; `CellBuffer` width times height is unchecked;
  tabs in cell text are dropped.
- L11: calculated-property errors (`BoundColumn.LastError`) are never shown; warn once per failing column.
- L12: reconcile the module and package signing configuration before the public release.
- Rows measures auto-sized children without a height limit but lays them out with the remaining height, so a log region with fewer entries than `PreferredHeight` takes extra blank rows in print-once layouts (live dashboards rely on the stable height).
- L13: brace-less bodies in `Binding/PSObjectMemberSource.cs` and `Binding/PropertyBinder.cs`; three-part
  `COLORFGBG` values are rejected.

## Website follow-ups
- Monster hero on phones: the fleet health app is a 140 x 40 recording, so on a phone it is drawn small and scrolls
  sideways. Record a compact phone rendering (a `data-compact` replay with its own poster moment, as the assessment
  replay on /live/ has) and use it below 700 px.

## v1.x (only with a real consumer)
- ChartForgeX.Terminal: ChartForgeX scenes on the ConsoleMonster canvas.
- Declarative JSON layouts with data binding.
- Spectre.Console adapter (widgets as `IRenderable`).
- Remote sources (named pipe or HTTP event stream) so a console can watch another process or service.
