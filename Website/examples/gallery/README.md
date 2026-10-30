# Copyable demos

Each folder contains a complete `<slug>.ps1`, `Program.cs` with initializers,
`Program.Fluent.cs` with fluent calls, and `metadata.json`. Starters keep the first useful version small. Showcases
carry the full workflow: richer dashboards, menus, forms, live progress, and
results. Both tiers use fictional inline data and never contact hosts, install
software, patch machines, or publish artifacts. These limits live in the source
comments so the terminal output reads like the tool you would build from it.

Install ConsoleMonster, then browse and run the PowerShell demos:

```powershell
Import-Module ConsoleMonster
Get-ConsoleDemo | Select-Object Slug, Title, Category
Show-ConsoleDemo fleet
(Get-ConsoleDemo fleet).Source | Set-Content -Path ./fleet.ps1 -Encoding UTF8
```

Use `Get-ConsoleDemo | Where-Object Tier -eq showcase` to browse the larger
programs. For example, `Show-ConsoleDemo fleet` opens the shell workflow
with presets, scope selection, a review, live check progress, and results.
`Show-ConsoleDemo fleet-filter` opens the smaller searchable host list.

Edit `fleet.ps1`, then run it from your shell. The copied program has no capture
parameters. `Show-ConsoleDemo fleet -Mode Snapshot -Step 0` selects a state through
the runner. `steps` in the metadata gives the number of available states, starting
at zero. Live dashboards finish after a short simulated run; apps exit with Escape.

For a welcome screen, export the script and call it from `$PROFILE`, or copy its
body into your profile. Replace the fictional values
with cached local information as needed; avoid network requests at shell startup.

Install `ConsoleMonster.Templates` with `dotnet new install`, then create a demo:

```terminal
dotnet new consolemonster --demo fleet -n FleetConsole
cd FleetConsole
dotnet run
```

The generated project contains both C# programs and needs only the
ConsoleMonster package. `dotnet run` selects initializers; `dotnet run --property:DemoStyle=Fluent`
selects the fluent file. The project compiles one style at a time. Set `CONSOLEMONSTER_SNAPSHOT=1` to initialize an app without
reading input and skip dashboard pauses. `CONSOLEMONSTER_INPUT` supplies scripted
keys for native menus, forms and prompts. Omitting `--demo` creates the full
menu/logging/hosting starter as before. The PowerShell assembly embeds the canonical scripts and
metadata; the template package includes the canonical C# files directly at pack
time. There is no second maintained copy of either source.

## Metadata and generated data

`metadata.json` defines the title, pitch, category, tags, terminal size, supported
runtimes, add-ons, three customization suggestions, mode, state count, `tier`,
and `useCase`. Its `capture` recipes select real screens with scripted keys,
live delay checkpoints or printed result markers. `capture.runKeys` is used only
when an unattended full run needs input; ordinary apps use the snapshot environment
switch. Capture controls never enter the copyable programs. The use-case sentence starts with "Use it when". `featured` marks
the strongest showcase programs. Keep starters at 250 lines or less and
showcases at 450 lines or less. No entry requires an add-on.

SiteFrames generates `Website/data/gallery.json` as:

```json
{
  "schemaVersion": 1,
  "demos": []
}
```

Each demo keeps its metadata and adds `id`, a `source` object with `powershell`
`csharp` and `csharpFluent` entries, `lines` with each file's line count, download URLs, run
commands, a `frame`, a `thumbnail` SVG URL, and per-state cell parity results.
Featured entries come first, followed by slug order. Live entries also carry
`url`, `duration` in milliseconds, `frames`, `fps`, and `start` for the existing
player. Their `capture` field identifies a scripted native program. These deterministic
storyboards show its actual successive states at fixed intervals, rather than
recording keyboard timing or claiming measured performance.

`replay` is null for a static entry. For a live entry it has the existing player
shape: `url`, `w`, `h`, `poster` (`w`, `h`, `dark`), `start`, and `hero`. Its
dimensions match the replay file, and `start` is the poster's time. The existing
flat replay and source fields remain available. Recordings of a different
program are not reused as evidence for a new port.

Each language entry in `source` contains `file`, `text`, and `repo`. `text` is
the complete runnable file, read directly from its canonical UTF-8 source, including
the final newline and omitting its BOM. PowerShell source files and downloads retain
exactly one UTF-8 BOM for Windows PowerShell 5.1. `repo` is its repository-relative path, for example
`Website/examples/gallery/fleet/fleet.ps1` or
`Website/examples/gallery/fleet/Program.cs`. Append it to
`https://github.com/EvotecIT/ConsoleMonster/blob/master/` for the source link.
The existing flat source and repository-path fields remain available.

Readers can copy `fleet.ps1` after `Install-Module ConsoleMonster`, or copy
either C# program into a new console project's `Program.cs` after `dotnet add package ConsoleMonster`.
Each program stands alone. All three explain their data, layout, and live
updates in comments; static screens print once and return.

Frames use the site's existing `frames.css`; replays use its existing player.
SiteFrames also generates `Website/content/pages/demos/<slug>.md` from the same
metadata. Each page uses the Demos detail layout. Validation rejects missing,
orphaned or stale pages, and website CI rejects uncommitted generated changes.

## Refresh captures

After editing an initializer program, regenerate the fluent source with
`dotnet run --project Website/tools/SiteFrames -c Release -- write-gallery-fluent Website`.
The translator resolves actual properties and preserves setter order. Callback
properties use Configure; mixed collection initializers retain their original order.

Build the module, capture its PowerShell output on any platform with PowerShell
7, then regenerate site assets:

```terminal
dotnet build ConsoleMonster.PowerShell -c Release -f net8.0
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website gallery
dotnet run --project Website/tools/SiteFrames -c Release -- render Website
```

The renderer compiles and runs both C# styles, compares all three programs' snapshot cells and
styles byte for byte, and rejects missing or stale captures. Each demo page has three
tabs in its initial HTML and a How it maps note generated from the parity contract.
`gallery-review Website <output-directory>` exports standalone HTML and text
for inspection. The normal website CI build includes the same parity check.

For focused validation, run the gallery Pester tests and
`python3 Tests/Build/test_template_gallery.py`; template tests use an isolated template
hive and local package feed.
