# Clickable links

ConsoleMonster makes URLs and file paths clickable with OSC 8 in supported interactive terminals. Tables and lists
recognize URL and path values automatically. A `Link` presenter declares the intent of a column, and rich text lets you
give a link a short label. Literal log messages also recognize visible URLs and paths.

## PowerShell

```powershell
Import-Module ConsoleMonster

Get-ChildItem -File | Show-ConsoleTable -Property Name, Length, @{
    Name = 'FullName'
    Header = 'Open file'
    Presenter = 'Link'
}

Get-Item ./report.txt | Show-Console -View List -Property Name, @{
    Name = 'FullName'
    Presenter = 'Link'
}

Write-ConsoleText 'Project documentation' -Link 'https://example.com/docs'
```

Inside a running ConsoleMonster dashboard, logs accept both literal messages and labelled rich text:

```powershell
Write-ConsoleLogEntry 'Saved /var/log/report.txt; see https://example.com/docs'
Write-ConsoleLogEntry (New-ConsoleText 'Open report' -Link 'file:///var/log/report.txt')
```

Use `New-ConsoleText -Link` in a calculated property when the label should differ from the destination:

```powershell
Get-ChildItem -File | Show-ConsoleTable -Property Name, @{
    Name = 'Report'
    Expression = { New-ConsoleText 'Open' -Link $_.FullName }
}
```

`Get-ConsoleCapability` returns the terminal profile, including `SupportsHyperlinks`. After changing an environment
override in the current session, refresh the cached capabilities:

```powershell
$env:CONSOLEMONSTER_HYPERLINKS = 'off'
Get-ConsoleCapability -Refresh | Select-Object Kind, SupportsHyperlinks, IsOutputRedirected, IsContinuousIntegration
```

Set the override to `on` (`true` or `1`) to enable links on another compatible terminal, `off` (`false` or `0`) to disable
them, or `auto` to use detection. Remove the variable to restore detection. Unknown values are ignored and recorded in
the profile's decision notes. Enabling links still requires interactive VT output.

## Terminal detection and text fallback

| Terminal | Detection |
| --- | --- |
| Windows Terminal | `WT_SESSION` is present |
| VS Code integrated terminal | `TERM_PROGRAM=vscode` |
| iTerm2 | `TERM_PROGRAM=iTerm.app` |
| WezTerm | `TERM_PROGRAM=WezTerm` |
| VTE terminals, including GNOME Terminal | `VTE_VERSION` is at least `5000` (0.50.0) |
| Konsole | `KONSOLE_VERSION` is at least `220400` (22.04.0, the conservative detection floor) |

Redirection, CI, stream mode, plain mode and unavailable VT always disable OSC 8, including with an environment or code
override. Unsupported terminals show ordinary text. A `Link` presenter whose label differs from its destination adds
the destination in parentheses, for example `Docs (https://example.com/docs)`. Rich text keeps its supplied label.
PowerShell host fallback routes, including transcripts, jobs and remoting, use a stream profile and emit no OSC 8.

Terminal settings can disable link activation even when the terminal advertises support. For example,
[VTE exposes an application setting for hyperlinks](https://gnome.pages.gitlab.gnome.org/vte/gtk4/property.Terminal.allow-hyperlink.html).
ConsoleMonster detects advertised capability; it does not open a destination or verify the terminal's mouse settings.
Use the terminal's normal modifier-click or link menu to follow a link.

Absolute Windows paths, UNC paths and Unix paths become escaped `file:` URIs without touching the filesystem. Explicit
`./` and `../` paths resolve against the process working directory; use full paths from `Get-Item` when the shell's
location may differ. Spaces, `#` and other filename characters are escaped in the target while the displayed path stays
readable. Existing `http:`, `https:`, `mailto:`, `ftp:` and `file:` URIs are supported. Other schemes and targets containing
terminal control characters stay text.

Automatic detection in literal log rows uses the smart tokenizer's visible tokens. For paths with spaces, long targets
that may be clipped or wrapped, and custom labels, supply an explicit link or `Link` presenter so the full destination
travels with the displayed cells. File links refer to the machine/path understood by the terminal; over SSH, a remote
path may need a terminal-specific handler. Detection does not establish remote-file accessibility.

## .NET

```csharp
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Presenters;
using ConsoleMonster.Widgets;

var table = new Table();
table.AddColumn(new TableColumn("Report") {
    Presenter = new LinkPresenter(text: _ => "Open report", address: value => (string)value!)
});
table.AddRow(TableCell.FromValue("/var/log/report.txt"));
Terminal.Write(table);

TerminalProfile profile = new CapabilityDetector().Detect();
bool clickable = profile.SupportsHyperlinks;
TerminalProfile disabled = profile.WithHyperlinks(false);
```

`TerminalProfile.SupportsHyperlinks` owns the final decision for presenters, frame encoding and direct styled output.
Custom capability providers can set the constructor's `supportsHyperlinks` argument or call `WithHyperlinks`. Copies
made for size, culture, language, glyphs, colour, theme and mode retain the configured support; switching back from stream
to interactive mode re-applies the output and CI guards. Font refresh also preserves the setting. The core adds no
runtime dependencies.
