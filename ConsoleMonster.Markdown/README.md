# ConsoleMonster.Markdown

The Markdown add-on owns Markdown display and export. It uses OfficeIMO.Markdown to read CommonMark with pipe tables and renders through ConsoleMonster's native widgets. The ConsoleMonster core keeps its zero-dependency package boundary and emits no Markdown.

In PowerShell, import ConsoleMonster and read a file or supply content:

```powershell
Import-Module ConsoleMonster
Show-ConsoleMarkdown -Path .\README.md
Show-ConsoleMarkdown -Content '# Deployment notes' -NoPager
```

For .NET, install the `ConsoleMonster.Markdown` package:

```csharp
using ConsoleMonster.Markdown;

MarkdownConsole.Show("# Deployment notes\n\nRead the [guide](https://example.org/guide).");
```

`MarkdownConsole.Parse` returns a reusable `MarkdownView` for composition, export or the shared `WidgetPager`. Long documents page only in an interactive console; redirected output prints the complete document. Headings, lists, quotes, code and tables reflow to the target width. Links use OSC 8 where supported and include a muted destination otherwise. HTML remains literal, images become labelled links, and no document content is executed or downloaded.

## Export

Add the package in C# and use `MarkdownExporter.Export(widget, options)`. The overloads accept rendered `CellBuffer` frames, live sessions and terminal `SessionRecording` transcripts. For dashboard event recordings, pass the dashboard definition, events and replay options; each distinct frame gets a timestamp heading. `fenced: true` on a widget keeps its exact terminal layout.

```csharp
using ConsoleMonster.Export;
using ConsoleMonster.Markdown;
using ConsoleMonster.Widgets;

var table = new Table().AddColumn("Host").AddRow("web-01");
string markdown = MarkdownExporter.Export(table, new ExportOptions { Width = 80 });
```

PowerShell keeps `$rows | Export-ConsoleFrame -Format Markdown`, `-Format FencedText`, `Copy-ConsoleOutput -Format Markdown` and `Export-ConsoleRecording -Format Markdown`. The module bundles the add-on and loads it only when Markdown display or export is requested. The core never emits Markdown; a core `.md` export request names this package.

OfficeIMO's document model owns fenced blocks and recording documents, including safe fence selection. Semantic tables, records and trees use direct text in this bridge: they already have presented cell values, group/subtotal boundaries and literal escaping that must stay intact without reparsing or a document-builder row limit. Their output is unchanged. Fenced content containing backticks may use a shorter tilde fence; the content remains literal and the goldens cover that change.
