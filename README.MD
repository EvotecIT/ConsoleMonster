<p align="center">
  <a href="https://www.powershellgallery.com/packages/ConsoleMonster"><img src="https://img.shields.io/powershellgallery/v/ConsoleMonster.svg?style=flat-square"></a>
  <a href="https://www.nuget.org/packages/ConsoleMonster"><img src="https://img.shields.io/nuget/v/ConsoleMonster.svg?style=flat-square"></a>
  <a href="https://github.com/EvotecIT/ConsoleMonster"><img src="https://img.shields.io/github/license/EvotecIT/ConsoleMonster.svg?style=flat-square"></a>
</p>

<p align="center">
  <a href="https://twitter.com/PrzemyslawKlys"><img src="https://img.shields.io/twitter/follow/PrzemyslawKlys.svg?label=Twitter%20%40PrzemyslawKlys&style=flat-square&logo=twitter"></a>
  <a href="https://evotec.xyz/hub"><img src="https://img.shields.io/badge/Blog-evotec.xyz-2A6496.svg?style=flat-square"></a>
  <a href="https://www.linkedin.com/in/pklys"><img src="https://img.shields.io/badge/LinkedIn-pklys-0077B5.svg?logo=LinkedIn&style=flat-square"></a>
</p>

# ConsoleMonster

ConsoleMonster is a zero-dependency console rendering library for .NET (netstandard2.0, net472, net8.0) and a
PowerShell module for Windows PowerShell 5.1 and PowerShell 7. It prints tables, trees, panels, key/value lists and
charts once, and runs live dashboards that many threads can update at the same time.

Version 2 replaces the Spectre.Console / Terminal.Gui based 0.0.x module (still available on the PowerShell Gallery).

## Layers

Each layer can be used without the ones above it.

| Layer | What it does |
|---|---|
| L0 Terminal | Capability detection (VT, colour depth, glyph tier from the console font, size, redirection, CI) with `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_MODE` and `NO_COLOR` overrides; UTF-16 output through `WriteConsoleW` on Windows consoles, UTF-8 when redirected; Ctrl+C restore. |
| L1 Text and style | Display-width measurement (Unicode 17), Spectre-compatible markup (`[bold red]text[/]`, `[pass]`), semantic themes mapped to 24-bit, 256 and 16 colours, glyph sets (Full, Basic, ASCII). |
| L2 Canvas | Cell buffer, braille and half-block pixel canvases, frame diff. |
| L3 Layout and widgets | `IWidget` / `IContainer`; Rows, Columns, Grid, Panel, Padding; text, rule, table, tree, key/value, status list, progress, lanes, phases, bar and stacked bar, sparkline, histogram, line/scatter chart, log region, logo, dashboard. |
| L4 Live | `LiveSession`: lock-free update queue, ~15 fps coalesced diff rendering, typed handles and names, ambient `TerminalScope`, Console.Out/Error capture into the log region, JSON-lines record and replay. |
| L5 Surfaces | The C# object model and a fluent builder; 38 thin PowerShell cmdlets with a plain `Console` noun. |

Every widget renders in every mode: a live dashboard becomes append-only lines when output is redirected or in CI,
and plain ASCII lines without colour with `CONSOLEMONSTER_MODE=plain`. Status is never shown by colour alone.

## PowerShell

```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Write-ConsoleTable -Title 'Services' -StatusProperty Status

$dashboard = New-ConsoleDashboard -Title 'Patch' -Gap 1 {
    New-ConsolePanel -Title 'Servers' { New-ConsoleLanes }
    New-ConsolePanel -Title 'Log' -Fill { New-ConsoleLog }
}
'SRV1', 'SRV2', 'SRV3' | Start-ConsoleDashboard $dashboard -Parallel -ThrottleLimit 3 {
    param($Server)
    Update-ConsoleLane -Key $Server -Status Running -Total 5
    1..5 | ForEach-Object { Start-Sleep -Milliseconds 300; Update-ConsoleLane -Key $Server -Result Pass }
    Write-Host "$Server patched"   # lands in the log region, never on top of the dashboard
    Update-ConsoleLane -Key $Server -Complete
}
```

## C#

```csharp
using ConsoleMonster;
using ConsoleMonster.Building;
using ConsoleMonster.Live;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Dashboard dashboard = new DashboardBuilder("Deploy")
    .Panel("Servers", new Lanes(), out Lanes lanes, caption: "4 workers")
    .Panel("Log", new LogRegion(), out LogRegion log, fill: true)
    .BuildDashboard();

using (LiveSession session = new LiveSession(dashboard)) {
    session.Run(s => {
        LanesHandle servers = lanes.Handle(s);           // thread-safe from any thread
        Parallel.ForEach(new[] { "SRV1", "SRV2", "SRV3" }, server => {
            servers.Start(server);
            servers.Advance(server, StatusKind.Pass, "copying files");
            Console.WriteLine(server + " done");          // captured into the log region
            servers.Complete(server);
        });
    });
}
```

## Repository

- `ConsoleMonster.psd1` / `ConsoleMonster.psm1` / `ConsoleMonster.Tests.ps1`: the module (built by `Build/Build-Module.ps1`).
- `Sources/ConsoleMonster`: the library; `Sources/ConsoleMonster.PowerShell`: the cmdlets; `Sources/ConsoleMonster.Tests`: xUnit;
  `Sources/ConsoleMonster.Examples`: showcases (`query`, `gallery`, `pipeline`, `testimox`, `record`, `stress`); `Sources/ConsoleMonster.Benchmarks`.
- `Examples/*.ps1`: PowerShell showcases; `Tests/*.Tests.ps1`: Pester (Windows PowerShell 5.1 and PowerShell 7).
- `Build/Build-Module.ps1` (PSPublishModule), `Build/Build-Project.ps1` and `Build/project.build.json` (PowerForge, NuGet package).
