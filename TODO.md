# ConsoleMonster engineering tasks

Granular tasks: validation, review follow-ups and small fixes. Decisions, the next rounds and the backlog are in
[ROADMAP.md](ROADMAP.md).

## Validation and follow-ups
- Validate on Windows Server 2016 (16 colours), 2019, 2022, 2025, Server Core, VS Code, SSH (ConPTY) and the PowerShell ISE fallback.
- Heat strip widget from the v1 widget list.
- Per-widget narration options for stream mode.
- Live-table filtering and horizontal scrolling as an alternative to hiding columns; column sorting already ships
  through the live table APIs.

## Follow-ups from the pre-PR review (review.md, 2026-10-17)
The review's high findings and M1-M4, M6 and M7 were fixed. Recheck these historical follow-ups against current code before implementing them:
- M5: print-once memory grows with rows x width (50,000 rows used about 570 MB). Negotiate widths over all rows or a
  bounded sample, then render and emit in bands; or cap rows with a warning.
- L1: the width re-probe after a resize or font change overwrites explicit `CONSOLEMONSTER_*_WIDTH` values and skips
  the ambiguous-width glyph downgrade; route it through the same method as the start-up probe.
- L3: live sessions without an explicit culture use the render thread's culture; capture the caller's culture and add
  `Invoke-ConsoleDashboard -Culture`.
- L5: with `-Parallel`, host writes made by upstream pipeline commands in the main runspace still reach the host
  directly and draw over the live frame (writes that go through `HostOutput` or `LiveSession.WriteOutside` no longer
  do); without `-Parallel`, piped input is ignored silently.
- `HostQueueWriter` (stream-mode dashboards outside a console) queues lines for the pipeline thread without a limit.
- L7: `TreeNode.Add` accepts cycles and `Tree` recurses into them (stack overflow).
- L8: `Table.Distribute` shrinks one cell per pass; subtract the needed amount in one step.
- L9: `TextWrapper.Truncate` measures the ellipsis with the default width profile instead of the given one.
- L10: a custom glyph set with empty spinner frames divides by zero; `CellBuffer` width times height is unchecked;
  tabs in cell text are dropped.
- L11: calculated-property errors (`BoundColumn.LastError`) are never shown; warn once per failing column.
- Rows measures auto-sized children without a height limit but lays them out with the remaining height, so a log region with fewer entries than `PreferredHeight` takes extra blank rows in print-once layouts (live dashboards rely on the stable height).
- L13: brace-less bodies in `Binding/PSObjectMemberSource.cs` and `Binding/PropertyBinder.cs`; three-part
  `COLORFGBG` values are rejected.

## Website follow-ups
- Monster hero on phones: the fleet health app is a 140 x 40 recording, so on a phone it is drawn small and scrolls
  sideways. Record a compact phone rendering (a `data-compact` replay with its own poster moment, as the assessment
  replay on /live/ has) and use it below 700 px. The fleet demo's scripted keys run out at 64 columns (the narrow
  layout needs a different key path), so the example needs that first.

## After the Diagram API
- Nerd Font glyph tier, opt-in only (`CONSOLEMONSTER_GLYPHS=nerd`, since the font cannot be detected): real icons for
  diagram node kinds (server, database, cloud, user, the kinds ChartForgeX already has icons for), status lists and
  trees. Without it nothing changes. Many PowerShell users already have Nerd Fonts for Oh My Posh.

## Future work (only with a real consumer)
- Declarative JSON layouts with data binding.
- Spectre.Console adapter (widgets as `IRenderable`).
- Remote sources (named pipe or HTTP event stream) so a console can watch another process or service.
