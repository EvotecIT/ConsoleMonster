# ConsoleMonster.Markdown

The optional Markdown add-on uses OfficeIMO.Markdown to read CommonMark with pipe tables and renders through ConsoleMonster's native widgets. The ConsoleMonster core keeps its zero-dependency package boundary.

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
