# ConsoleMonster

Tables, trees, charts and live dashboards for .NET and PowerShell. ConsoleMonster lays out the screen, fits content to
the terminal, and keeps output readable when it goes to a log. The PowerShell cmdlets use the same rendering engine as
the C# library.

[![ConsoleMonster: real terminal output from the assessment dashboard](https://raw.githubusercontent.com/EvotecIT/ConsoleMonster/master/Website/static/site/social-card.png)](https://consolemonster.dev/)

[Website and gallery](https://consolemonster.dev/) · [Documentation](https://consolemonster.dev/docs/) ·
[.NET API](https://consolemonster.dev/api/) · [PowerShell commands](https://consolemonster.dev/powershell/commands/) ·
[Examples and usage guide](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Usage.md)

## Make PowerShell output nicer in one line

Install the module once, then turn the formatting on for the session:

```powershell
Install-Module ConsoleMonster -Scope CurrentUser
Import-Module ConsoleMonster
Enable-ConsoleFormatting
```

Files, processes, services, commands, modules, event log records, IP addresses, volumes and dictionaries that reach the
console are then shown as ConsoleMonster tables and records. The objects don't change: pipelines into other commands,
`Format-*`, `Out-String`, redirected output, jobs and remoting keep PowerShell's behaviour. `Disable-ConsoleFormatting`
turns it off again.

To keep it on in every session, print the lines for your profile. The command only prints them and never edits the profile:

```powershell
Enable-ConsoleFormatting -ProfileSnippet
```

```text
# ConsoleMonster: nicer default output in every session. Add these lines to C:\Users\you\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
Import-Module ConsoleMonster
Enable-ConsoleFormatting
```

## Format-Console commands

The `Format-Console*` commands work like `Format-Table` and `Format-List`, and take the same `-Property` lists and
calculated properties. `fct` and `fcl` are short aliases for `Format-ConsoleTable` and `Format-ConsoleList`.

```powershell
$servers = @(
    [pscustomobject] @{ Name = 'web-01'; Status = 'Running'; Cpu = 12.5; Region = 'Warsaw'; Tags = @('prod', 'dmz') }
    [pscustomobject] @{ Name = 'web-02'; Status = 'Warning'; Cpu = 71; Region = 'Dublin'; Tags = @('prod') }
    [pscustomobject] @{ Name = 'sql-01'; Status = 'Stopped'; Cpu = 0; Region = 'Dublin'; Tags = @() }
)
$servers | Format-Table
```

```text
Name   Status     Cpu Region Tags
----   ------     --- ------ ----
web-01 Running 12.500 Warsaw {prod, dmz}
web-02 Warning     71 Dublin {prod}
sql-01 Stopped      0 Dublin {}
```

```powershell
$servers | Format-ConsoleTable
```

```text
╭────────┬───────────┬──────┬────────┬───────────╮
│ Name   │ Status    │  Cpu │ Region │ Tags      │
├────────┼───────────┼──────┼────────┼───────────┤
│ web-01 │ ✓ Running │ 12.5 │ Warsaw │ prod, dmz │
│ web-02 │ ▴ Warning │ 71.0 │ Dublin │ prod      │
│ sql-01 │ – Stopped │  0.0 │ Dublin │ —         │
╰────────┴───────────┴──────┴────────┴───────────╯
```

- Status words get a marker and a colour, including PowerShell's own words (Success, Warning, Error, Information).
- The numbers in a column share the decimals the column needs, so the decimal separators line up.
- Short values (numbers, sizes, booleans, dates, statuses) never wrap. Long text wraps onto at most three lines, and
  `-Wrap` shows all of it.
- Identifiers such as `Id`, `Port` and `ProcessId` are never grouped with thousands separators.
- Dates use one format everywhere: `yyyy-MM-dd HH:mm` in local time, whatever the culture, so they sort and compare
  the same on every machine. Numbers follow the culture, or `-Culture` and `CONSOLEMONSTER_CULTURE`.
- Types with a known view keep their usual columns, and files keep their directory heading:

```text
 Directory: C:\Windows
╭───────┬──────────────────┬─────────┬──────────────╮
│ Mode  │ LastWriteTime    │  Length │ Name         │
├───────┼──────────────────┼─────────┼──────────────┤
│ la--- │ 2026-09-09 07:09 │  352 KB │ notepad.exe  │
│ la--- │ 2026-10-14 02:06 │ 3.32 MB │ explorer.exe │
╰───────┴──────────────────┴─────────┴──────────────╯
```

One object stays a one-row table, as with `Format-Table` (a single dictionary is shown as a record, see below).
`Format-ConsoleList`, or `Format-ConsoleTable -View Record`, shows labelled fields instead:

```powershell
$servers[0] | Format-ConsoleList
```

```text
web-01
  Status  ✓ Running
  Cpu     12.5
  Region  Warsaw
  Tags    prod, dmz
```

`Format-ConsoleTree` shows an object's structure, `Format-ConsoleJson` writes JSON through the same rules,
`Format-ConsoleError` shows an error with its location once, and `Show-Console` picks a view for you.

## Arrays, dictionaries and grouping

Arrays in a cell are joined (`prod, dmz`), and an empty one shows a dash. A dictionary is shown as a record, and a list of
dictionaries as a table of their keys:

```powershell
@{ Name = 'web-01'; Port = 8080; Enabled = $true } | Format-ConsoleTable
@{ Name = 'web-01'; Port = 8080 }, @{ Name = 'web-02'; Port = 8443 } | Format-ConsoleTable
```

```text
web-01
  Enabled  ✓ yes
  Port     8080
╭────────┬──────╮
│ Name   │ Port │
├────────┼──────┤
│ web-01 │ 8080 │
│ web-02 │ 8443 │
╰────────┴──────╯
```

`-GroupBy` puts the rows under labelled rules with their counts. The grouped column is left out, because the heading
already names it (`-ShowGroupColumn` keeps it):

```powershell
$servers | Format-ConsoleTable -GroupBy Region -Property Name, Status, Cpu
```

```text
╭──────────────┬───────────┬──────╮
│ Name         │ Status    │  Cpu │
├─ Dublin · 2 ─┼───────────┼──────┤
│ web-02       │ ▴ Warning │ 71.0 │
│ sql-01       │ – Stopped │  0.0 │
├─ Warsaw · 1 ─┼───────────┼──────┤
│ web-01       │ ✓ Running │ 12.5 │
╰──────────────┴───────────┴──────╯
```

## Keep the output

The output reaches every PowerShell consumer: `| Out-String`, `| Out-File`, `| Select-String` and `| more` get plain
lines. On PowerShell 7.2 and later, a command typed at the prompt can also be captured with `$x = ...`, `> file` or
`-OutVariable`. Inside scripts and functions, and on Windows PowerShell 5.1, add `-AsString` to keep the text in a
variable. Use `-AsWidget` to compose a table into a panel or a dashboard, or to export it:

```powershell
$report = $servers | Format-ConsoleTable -AsString
$servers | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path ./servers.html
```

The [command reference](https://consolemonster.dev/powershell/commands/) has parameters and examples for every cmdlet.

## Compose widgets in PowerShell

Every widget is a `New-Console*` command. Containers take a script block of children, and widgets with items take item
commands with the same `-Status`, `-Detail` and `-Tone` parameters everywhere, so no .NET code is needed:

```powershell
New-ConsoleTree '[bold]evotec.xyz[/]' {
    New-ConsoleTreeNode 'MX' -Status Pass -Detail '2 records' {
        New-ConsoleTreeNode 'evotec-xyz.mail.protection.outlook.com' -Detail 'priority 0'
    }
    New-ConsoleTreeNode 'DMARC' -Status Fail -Detail 'p=none'
} | Write-ConsoleWidget

New-ConsoleLog -Height 4 {
    New-ConsoleLogEntry 'Discovered 4 domain controllers in 2 domains' -Level Info -At 2s
    New-ConsoleLogEntry 'DC02: USN rollback check failed (event 2095)' -Level Error -At 44s
} | Write-ConsoleWidget
```

[PowerShell conventions](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/PowerShell-Conventions.md) lists
every widget with its cmdlet form, its live update command and its .NET method form.

## Choose your starting point

| What you need | Start here |
| --- | --- |
| Print objects as readable tables | [Tables](https://consolemonster.dev/tables/) |
| Compose panels, trees, charts and progress indicators | [Widget gallery, with C# and PowerShell examples](https://consolemonster.dev/widgets/) |
| Show work from several threads or scripts | [Live dashboards and replays](https://consolemonster.dev/live/) |
| Ask for input or build a terminal screen | [Prompts, menus, forms and grids](https://consolemonster.dev/interactive/) |
| Replace existing PowerShell formatting | [PowerShell guide](https://consolemonster.dev/powershell/) |
| Save output or play a run again | [Recording and export](https://consolemonster.dev/docs/recording/) |

The .NET package has no runtime package dependencies. It targets **.NET Standard 2.0, .NET Framework 4.7.2, .NET 8 and .NET 10**.
The module supports **Windows PowerShell 5.1 and PowerShell 7**. Both are MIT licensed.

## Start with C#

Add the package to your application:

```shell
dotnet add package ConsoleMonster
```

Create a table and write it to the terminal:

```csharp
using ConsoleMonster;
using ConsoleMonster.Widgets;

Table table = new Table { Title = "Servers" };
table.AddColumn(new TableColumn("Name") { IsKey = true });
table.AddColumn(new TableColumn("Region"));
table.AddColumn(new TableColumn("Status"));
table.AddValues("web-01", "Warsaw", "Pass");
table.AddValues("web-02", "Dublin", "Warn");
table.AddValues("web-03", "Dublin", "Fail");

Terminal.Write(table);
```

Widgets support object initializers, fluent `With...` / `Add...` methods, and the factories in `ConsoleMonster.Ui`.
These styles compose the same objects; choose the one that reads best for your application. The fluent methods are
compiled into the library, so consuming projects do not need a source generator.

For typed data, start with `Table.For(items)` and select columns with expressions. For formatted sizes, durations,
numbers and status cells, use the built-in cell presenters. The [table guide](https://consolemonster.dev/tables/)
shows both languages and the output at different window widths.

### Logging, hosting and a template

Three optional packages build on the zero-dependency core:

- [`ConsoleMonster.Extensions.Logging`](ConsoleMonster.Extensions.Logging/README.md): `logging.AddConsoleMonster()`
  writes `ILogger` entries with ConsoleMonster's level tags, timestamps, categories and scopes. Entries go into a
  running dashboard's log region, and print as plain lines when the output is redirected.
- [`ConsoleMonster.Hosting`](ConsoleMonster.Hosting/README.md): `services.AddConsoleDashboard()` shows a generic host's
  hosted services and log live. It stays out of the way when the output is redirected or the app runs as a service.
- [`ConsoleMonster.Templates`](Templates/ConsoleMonster.Templates/README.md): `dotnet new consolemonster` creates an
  app with a menu, logging, the host dashboard and native AOT publishing.

## Keep long-running work visible

A dashboard combines named widgets with a session that accepts updates while work runs. In PowerShell, the dashboard
captures the script's host output, warnings and errors in its log region:

```powershell
$dashboard = New-ConsoleDashboard -Title 'Deploy' -Gap 1 {
    New-ConsolePanel -Title 'Servers' { New-ConsoleLanes }
    New-ConsolePanel -Title 'Output' -Fill { New-ConsoleLog }
}

'web-01', 'web-02', 'web-03' | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 3 {
    param($Server)

    Update-ConsoleLane -Key $Server -Status Running -Total 3
    1..3 | ForEach-Object {
        Start-Sleep -Milliseconds 300
        Update-ConsoleLane -Key $Server -Result Pass
    }
    Write-Host "$Server deployed"
    Update-ConsoleLane -Key $Server -Complete
}
```

In C#, build the screen with `DashboardBuilder`, run it with `LiveSession`, and post updates through typed widget
handles. Handles accept updates from worker threads; the session draws the screen and restores the terminal when it
ends. See the [live-session guide](https://consolemonster.dev/docs/live-sessions/) for a complete C# example,
inline progress, replaceable regions and live tables.

## Output that fits its destination

ConsoleMonster detects terminal capabilities and adapts the same widgets to the available space and output host.

- **Narrow windows:** tables hide lower-priority columns and can turn rows into cards. Paths and other long values wrap
  at useful separators.
- **Colour and fonts:** 24-bit, 256-colour, 16-colour and uncoloured output; Full, Basic and ASCII glyph sets.
  Status includes a symbol or word so colour is not the only signal.
- **Logs and CI:** redirected output uses plain lines. Live sessions narrate progress instead of emitting a moving screen.
- **PowerShell hosts:** transcripts and the ISE use host output; jobs and remoting use information records. How those
  records display colour depends on the receiving host.
- **Themes and language:** built-in dark and light themes, culture-aware formatting and localized built-in labels.

`NO_COLOR` and the `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_MODE`,
`CONSOLEMONSTER_THEME` and `CONSOLEMONSTER_CULTURE` environment variables let you choose an explicit profile.
See [capability detection](https://consolemonster.dev/docs/capabilities/),
[environment settings](https://consolemonster.dev/docs/environment/) and
[hosts and CI](https://consolemonster.dev/guides/hosts-and-ci/) for supported values and host behavior.

## Save, replay and test your output

Export widgets and frames as self-contained HTML, SVG or plain text. Record live-session events, then replay them
through the dashboard or export an HTML player, animated SVG or asciicast recording. The
[recording guide](https://consolemonster.dev/docs/recording/) explains the formats and their limits.

For automated checks, use an in-memory target instead of a real console. `Assert-ConsoleOutput` checks PowerShell
output, and the .NET testing APIs let you inspect frames and events. The
[usage guide](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Usage.md#testing-your-output)
has complete assertions, export examples and hosting patterns.

## Compatibility

Version 2 replaces the 0.0.x module's Spectre.Console and Terminal.Gui wrappers with ConsoleMonster's own rendering
engine. The old `New-Spectre*` and `New-Terminal*` commands are removed. Use the
[installation guide](https://consolemonster.dev/docs/install/) and
[migration guides](https://consolemonster.dev/guides/) when updating existing scripts.

Windows PowerShell 5.1 loads one version of an assembly per session. Modules that use ConsoleMonster there must use
the same library version; restart the session after an upgrade. PowerShell 7 loads the module's binaries in their own
assembly load context.

The .NET 8 and .NET 10 targets are marked AOT compatible. When trimming, disable the object inspector's reflection member source
with `InspectorOptions.UseReflection = false`, or explicitly preserve the members your application inspects.
The general-purpose inspection API still emits a trimming warning because its options are chosen at runtime.
Suppress that call-site warning only when you have verified your inputs use dictionary, JSON or custom member sources
with reflection disabled. Typed tables preserve their public properties; inferred columns use declaration order on
managed runtimes and stable alphabetical order when Native AOT does not expose metadata tokens.

CI publishes and runs a native executable on .NET 8 and .NET 10, checking typed and inferred tables, reflection-free
dictionary and JSON inspection, and text, HTML and SVG exports. This applies to the .NET library; the PowerShell
module runs inside its PowerShell host and is not a standalone Native AOT application.

## Build and release

The repository uses PSPublishModule / PowerForge for C# packaging, module generation and coordinated publication.
Run these commands from the repository root with PowerShell 7 and PSPublishModule installed:

```powershell
# Build the NuGet package and PowerShell module together, without signing or installing locally.
./Module/Build/Build-Module.ps1 -RunMode Build -SignModule $false -SkipInstall $true

# Preview the C# package version without building, updating files or publishing.
./Build/Build-Project.ps1 -ConfigurationGateMode Manifest

# Build only the C# package.
./Build/Build-Project.ps1 -ConfigurationGateMode Build

# Sign the assemblies, NuGet package and module, then publish the coordinated release.
./Module/Build/Build-Module.ps1 -RunMode Publish
```

The coordinated command builds packages before the module, uses the C# package as the shared version source, and
publishes in this order: **NuGet, PowerShell Gallery, GitHub**. Both publishers use the `ConsoleMonster-v{version}`
release identity. GitHub receives the C# release archive and the PowerShell module ZIP, with generated release notes.
The release signs the C# assemblies, NuGet package and PowerShell module with the configured certificate.

Publishing needs the configured signing certificate and destination credentials. Credential file paths can be
configured in `Build/project.build.json`; the module script also accepts `PowerShellGalleryApiKeyPath` and
`GitHubApiKeyPath`. Build mode does not upload packages. Generated packages and coordinated staging are under
`Artefacts/`; module build reports are under `Module/Artefacts/`.

For development, build the solution and run the .NET and PowerShell suites:

```powershell
dotnet build ConsoleMonster.sln -c Release
dotnet test ConsoleMonster.Tests/ConsoleMonster.Tests.csproj -c Release --no-build
Invoke-Pester -Path ./Module/Tests
```

Run Pester 5 in both Windows PowerShell 5.1 and PowerShell 7 after building the module binaries. CI exercises both
PowerShell hosts and the .NET Framework 4.7.2 / .NET 8 / .NET 10 test targets. The source module can load development binaries
from `ConsoleMonster.PowerShell/bin`.

## Explore the project

- [Interactive website](https://consolemonster.dev/): real rendered output, width and colour comparisons, live replays,
  and examples in both languages.
- [Detailed usage guide](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Usage.md): rich text, tables,
  prompts, forms, Pester output, exports and testing APIs.
- [C# showcases](https://github.com/EvotecIT/ConsoleMonster/tree/master/ConsoleMonster.Examples) and
  [PowerShell examples](https://github.com/EvotecIT/ConsoleMonster/tree/master/Module/Examples): runnable examples.
- [Benchmarks](https://consolemonster.dev/benchmarks/): measured scenarios and their environment.
- [Design system](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Design-System.md): terminal layout,
  focus, status and interaction conventions.
- [Report an issue](https://github.com/EvotecIT/ConsoleMonster/issues): include the host, terminal, font, module or
  package version, and a small reproduction.

ConsoleMonster is maintained by [Evotec](https://evotec.xyz/) and distributed under the
[MIT license](https://github.com/EvotecIT/ConsoleMonster/blob/master/LICENSE).
