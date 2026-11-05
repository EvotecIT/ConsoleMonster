# ConsoleMonster 2.2.0 release notes

ConsoleMonster 2.2.0 makes apps and documents easy to move around in. The mouse works wherever the keyboard does, long content folds, panes resize, logs and documents have find, Markdown and command help open in a document viewer, and charts can be explored and linked to tables. Everything is built from parts you can reuse in your own apps, in PowerShell and C#.

## Highlights

- **The mouse wherever the keyboard works.** Footer hints, breadcrumbs, scroll bars, toggles, text selection, double-click to open, header sorting and a right-click context menu (also on the Menu key and Shift+F10), in apps and in standalone prompts, menus, grids, the explorer and pagers. Every pointer action has a key, and `CONSOLEMONSTER_NOMOUSE=1` turns it off everywhere. (#106)
- **Folding, panes and find.** `New-ConsoleCollapsible` and `New-ConsoleAccordion` fold long content, `New-ConsoleSplitter` resizes two panes with the mouse or Ctrl+Shift+arrows, and a find bar searches logs, text areas and paged documents. (#107)
- **Documents and help you can navigate.** Markdown with headings and `Show-ConsoleHelp` open in a viewer with an outline, folding headings, links with back and forward history and Ctrl+K to jump to a section or parameter. Help gets a header card and tabs for Overview, Parameters, Examples and Related. (#108)
- **Charts you can explore.** `New-ConsoleChartView` adds a crosshair, legend toggles, zoom and pan to any chart, and `-LinkKey` links its selection to a `New-ConsoleDataGrid` or `New-ConsoleSelect` through app state. (#109)

## New

- Mouse support in apps, prompts, menus, the command palette, forms, `Show-ConsoleGrid`, `Show-ConsoleObject`, the pager and diagrams. `InteractiveOptions.Mouse` and `CONSOLEMONSTER_NOMOUSE=1` turn it off everywhere; `Show-ConsoleApp -NoMouse` turns it off for one app. (#106)
- `Collapsible` and `Accordion` (`New-ConsoleCollapsible`, `New-ConsoleAccordion -Content`): focusable headers, collapsed summaries, one open section at a time in an accordion, `+` and `-` to expand or collapse all. (#107)
- `Splitter` (`New-ConsoleSplitter`): side-by-side or stacked panes with a draggable divider, `-Fill`, content measurement for vertical splitters and a ratio stored in app state under its `Key`. (#107)
- A find bar in `LogViewport`, `TextArea` and the document pager, with highlighting, match counts, Enter and Shift+Enter, and `n`/`N` after the bar closes. (#107)
- The document viewer for Markdown and command help, used by `Show-ConsoleHelp` and by `help` with `Enable-ConsoleFormatting -Include Help` on interactive terminals. `Show-ConsoleHelp -NoPager` prints instead, and redirected output is unchanged. (#108)
- Reusable navigation parts: `Outline` (`New-ConsoleOutline -Items`), `Breadcrumb` (`New-ConsoleBreadcrumb -Segments -Ancestors`) and `NavigationHistory<T>`. (#108)
- Keyed tabs: `New-ConsoleTabs -Key -OnChange` keeps the active tab's title in app state; C# uses `new Tabs("key")`, `Tabs.Add(TabOptions)` and the fluent `.Tab(title, c => …)`. Tab strips take focus, list their keys in the footer and scroll on narrow terminals. (#108)
- `ChartView` (`New-ConsoleChartView -PointLabels`) over any `ChartPlot`, and `ToChartPlot()` for ChartForgeX line charts in ConsoleMonster.Graphics. (#109)
- Linked selection through `ILinkedSelection.LinkKey`: `DataGrid`, `ChartView`, `Select` and `MultiSelect` share a selection in app state, with `-LinkKey` on `New-ConsoleDataGrid`, `New-ConsoleChartView` and `New-ConsoleSelect`. `New-ConsoleDataGrid` also lists ordinary objects with `-InputObject` and `-Property`. (#109)
- Three gallery demos in PowerShell, C# and C# fluent: `chart-table`, `help-sections` and `settings-tabs`, also available through `dotnet new consolemonster --demo`.

## Changed

- `DataGrid.SelectedIndex` is the focused row's position, the name `Tabs`, `Outline` and `ChartView` use too. `DataGrid.Cursor` still works and is marked obsolete.

## Fixed

- Double-clicking a row in `Show-ConsoleGrid` opens its details.
- A splitter inside a binding keeps dragging after the binding redraws.
- The find bar's match count is no longer clipped.
- `Show-ConsoleObject` prints a static view when output is redirected or runs under CI.
- Fluent-style projects from `dotnet new consolemonster` build.
- Examples that read input print a clear message when input is redirected, instead of failing.

## Dependencies

- ConsoleMonster.Graphics uses ChartForgeX and ChartForgeX.Markup 3.4.0.
- ConsoleMonster.Markdown uses OfficeIMO.Markdown 3.5.2.
- ConsoleMonster.Hosting uses Microsoft.Extensions.Hosting.Abstractions 10.0.13, and ConsoleMonster.Extensions.Logging uses Microsoft.Extensions.Logging.Abstractions 10.0.13.
- The core ConsoleMonster package has no dependencies and targets netstandard2.0, net472, net8.0 and net10.0. The PowerShell module supports Windows PowerShell 5.1 and PowerShell 7.

The full list of changes, including the 2.1.1 to 2.1.3 releases, is in [the changelog](../CHANGELOG.MD).
