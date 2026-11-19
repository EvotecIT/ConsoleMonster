# Migrating to ConsoleMonster 3.0

ConsoleMonster 3.0 adopts the authoring convention shared with ChartForgeX, CanopyX and HtmlForgeX: fluent methods
are generated on the models, children are named by their noun, callbacks declare a verb, output uses one vocabulary,
and `ThemeMode`, `Tone` and `Icon` mean the same thing in every engine. It is one breaking release with no
compatibility shims. Object initializers and plain property access are unchanged; most changes are method names in
fluent chains.

PowerShell scripts are mostly unaffected: cmdlet and parameter names stay, and the few renamed parameters keep their old
name as an alias until 4.0.

## Children: `AddX` becomes `X`

Every `AddX(item)` becomes `X(item)`, and `AddXs(items)` becomes `Xs(items)`, which takes an `IEnumerable<T>`.
Handwritten model methods follow the same rule, and `SetX` methods that return the model become `X`.

| 2.x | 3.0 |
| --- | --- |
| `table.AddColumn("Name").AddRow(...)` | `table.Column("Name").Row(...)` |
| `table.AddValues(...)`, `AddGroup`, `AddPlainRow`, `SetFooter` | `Values(...)`, `Group`, `PlainRow`, `Footer` |
| `header.AddCrumb("Tasks").AddCrumbs("Review", "Run")` | `header.Crumb("Tasks").Crumbs(new[] { "Review", "Run" })` |
| `column.AddRoleRules(rule)` | `column.RoleRule(rule)` |
| `recordView.AddField(...)`, `AddSection`, `AddWidget`, `AddFieldAt` | `Field(...)`, `Section`, `Widget`, `FieldAt` |
| `markdown.AddParagraph(...)`, `AddHeading`, `AddCode`, `AddTable` | `Paragraph(...)`, `Heading`, `Code`, `Table` |
| `chart.AddItem(...)`, `barChart.AddMissing(...)`, `lineChart.AddSeries(...)` | `Item(...)`, `Missing(...)`, `Series(...)` |
| `summary.SetCounter(...)`, `SetProgress(...)` | `Counter(...)`, `Progress(...)` |
| `matrix.SetCell(...)`, `phases.SetItem(...)`, `SetSubItem(...)` | `Cell(...)`, `Item(...)`, `SubItem(...)` |
| `sparkline.AddRange(values)` | `sparkline.Values(values)` |
| `grid.AddColumn(...)`, `AddRow(...)` | `grid.Column(...)`, `Row(...)` |

A plural method no longer takes `params`: pass an array or call the singular method once per item. `Add` on containers
(collection initializers) and `void` runtime operations on live widgets (`LiveTable.AddRow`, `Grid.SetRows`,
`RegionWidget.Set`) keep their names.

## Callbacks: `Configure(...)` becomes a declared verb

`Configure(x => x.Callback = ...)` is gone. Each delegate property has its own method:

| Property | Method |
| --- | --- |
| `Validator` | `Validate(...)` |
| `VisibleWhen` | `ShowWhen(...)` |
| `Preview`, `PreviewText` | `PreviewBy(...)`, `PreviewTextBy(...)` |
| `LoadChoices` | `ChoicesBy(...)` |
| `OnChange`, `OnClick`, `OnSubmit`, `AfterSubmit` | `WhenChanged(...)`, `WhenClicked(...)`, `WhenSubmitted(...)` |
| `Action`, `ActionAsync` | `Run(...)`, `RunAsync(...)` |
| `TitleSource`, `HintsSource`, `Content`, `Page`, `Review`, `Badge`, `Detail` | `TitleBy`, `HintsBy`, `ContentBy`, `PageBy`, `ReviewBy`, `BadgeBy`, `DetailBy` |
| `IsHealthy`, `IsIncomplete`, `Available`, `Unavailable` | `HealthyWhen`, `IncompleteWhen`, `AvailableWhen`, `UnavailableWhen` |
| `Output`, `WriteLine`, `Print` | `WriteTo(...)` |

The pattern: a computed value is `XBy`, a predicate is `XWhen`, an event is `WhenXed`, and an action is a verb.
Two properties were renamed so the verb reads naturally: `ReviewModel.Run` is now `ReviewModel.Action` (set with
`Run(...)`), and `FormDialogOptions.Validate` / `FormDialog<T>.Validate` are now `Validator` (set with `Validate(...)`).
The clock and delay test hooks (`Clock`, `ActionClock`, `ReplayDelay`, `Delay`) have no fluent method; assign them.

## Output: `Export` becomes `Save`, `Render` and `ToX`

| 2.x | 3.0 |
| --- | --- |
| `widget.Export("report.html")` | `widget.Save("report.html")` |
| `widget.Export(path, "Svg")` | `widget.Save(path, OutputFormat.Svg)` |
| `ConsoleExporter.Export(widget, format, options)` | `widget.Render(format, options)` (a `RenderedOutput`) or `widget.ToHtml(options)` |
| `FrameExporter.Export(widget, "Png", ...)` | `FrameExporter.Render(widget, OutputFormat.Png, ...).ToBytes()` |
| `recording.Export(format, options)` | `recording.Render(format, options)`, `recording.Save(path)`, `ToHtml()`, `ToSvg()` |
| `recording.ExportPlayerData()`, `ExportFinalSvg()` | `ToPlayerData()`, `ToFinalSvg()` |
| `Recording.ExportHtml/ExportSvg/ExportAsciicast(...)` | `Recording.ToHtml/ToSvg/ToAsciicast(...)` |
| `MarkdownExporter.Export(widget)`, `ExportRecording(recording)` | `widget.ToMarkdown()`, `recording.ToMarkdown()` |
| `MarkdownExporter.ExportRecording(root, events)` | `MarkdownExporter.ReplayToMarkdown(root, events)` |
| `widget.SavePng(path, options)`, `recording.SaveGraphics(path, format)` | `widget.Save(path, options)`, `recording.Save(path, format)` |
| `recording.SaveChapterPngs(folder)` | `recording.SaveChapters(folder)` |
| `ExportFormat` | `OutputFormat` |

`ConsoleExporter.ToHtml`, `ToSvg` and `ToText` are now extension methods, so `widget.ToSvg()` works; the static calls
still compile. `OutputFormat` also names the add-on formats (`Markdown`, `FencedText`, `Png`), and an
`IFrameExportProvider` reports its `OutputFormat` and implements `Render` instead of `Export`.

## Themes: `ThemePreference` becomes `ThemeMode` and `ThemePalette`

`TerminalProfile.ThemePreference` is split in two:

| 2.x | 3.0 |
| --- | --- |
| `ThemePreference.Auto`, `Light`, `Dark` | `ThemeMode.Auto`, `Light`, `Dark` |
| `ThemePreference.HighContrast` | `ThemePalette.HighContrast` |
| `ThemePreference.ColorBlind` | `ThemePalette.ColorBlind` |
| `ThemePreference.ColorBlindLight` / `ColorBlindDark` | `ThemePalette.ColorBlind` with `ThemeMode.Light` / `Dark` |
| `ThemePreference.Terminal` | `ThemePalette.Terminal` |
| `profile.WithThemePreference(x)` | `profile.WithThemeMode(mode)`, `profile.WithPalette(palette)` |

`CONSOLEMONSTER_THEME` values are unchanged. `Get-ConsoleCapability` shows `ThemeMode` and `Palette` instead of
`ThemePreference`.

## Tone and Icon

- `MatrixCell.Tone`, `TableRowRole.Tone`, `TableRowRole.MarkerTone`, `TreeNode.MarkerTone` and `TableCell.WithTone` take
  the shared `Tone` (`Tone.Danger`, `Tone.Muted`, or `Tone.Named("accent")` for any theme token by name) instead of
  `ThemeToken?`. `ThemeToken.ToTone()` and `Tone.ToToken()` convert. Unset is `Tone.None` rather than `null`.
- `StatusKind` and `Severity` stay as facts; `ToTone()` gives the tone they paint with.
- `GlyphIcon` is replaced by icon catalog keys: `Icons.Run` (`Icon.Named("run")`). `MenuItem.Icon` is now the key, and the
  free-text mark moved to `MenuItem.Mark`. In PowerShell, `New-ConsoleMenuItem -Icon run` takes the key (`-IconName`
  remains an alias until 4.0) and `-Mark` takes free text. A script that passed text to `-Icon` uses `-Mark`.

## Smaller changes

- Handwritten fluent helpers moved into the per-type classes (`TableFluent`, `SplitterFluent`, `MenuItemFluent`,
  `DashboardFrameFluent`); code that named `WidgetsFluent`, `FramesFluent` or `InteractiveFluent` directly uses the
  type's class.
- `Ui.Panel("Build", p => ...)`, `Ui.Table("Hosts")`, `Ui.Tree`, `Ui.HeaderBar`, `Ui.Menu`, `Ui.Form`, `Ui.Chart` and
  `Ui.Diagram` take a title and an optional configuration; the parameterless factories stay.
- `LineChart.Series(key)` and `Sparkline.Values(values)` are extension methods, because the classes have properties of
  the same name.
