# Copyable demos

Each folder contains one complete `<slug>.ps1` script, one complete `Program.cs`,
and `metadata.json`. The samples use fictional inline data and never contact
hosts, install software, patch machines, or publish artifacts. The welcome
screens use fixed host and git summaries so they start quickly and work offline.

Install ConsoleMonster, then browse and run the PowerShell demos:

```powershell
Import-Module ConsoleMonster
Get-ConsoleDemo | Select-Object Slug, Title, Category
Show-ConsoleDemo fleet
Get-ConsoleDemo fleet -Path ./fleet.ps1
```

Edit `fleet.ps1`, then run it from your shell. Use `-StaticSnapshot -Step 0`
to print a state without reading input. `Steps` in the metadata gives the number
of available states, starting at zero. Redirected runs also select snapshot mode.
Live dashboards finish after a short simulated run; apps exit with Escape.

For a welcome screen, export the script and call it from `$PROFILE`, or copy its
body after the `param` block into your profile. Replace the fictional values
with cached local information as needed; avoid network requests at shell startup.

Install `ConsoleMonster.Templates` with `dotnet new install`, then create a demo:

```terminal
dotnet new consolemonster --demo fleet -n FleetStarter
cd FleetStarter
dotnet run
```

The generated project contains the gallery's `Program.cs` and needs only the
ConsoleMonster package. Run it with `dotnet run -- --snapshot --step 1` to print
a deterministic state. Omitting `--demo` creates the full menu/logging/hosting
starter as before. The PowerShell assembly embeds the canonical scripts and
metadata; the template package includes the canonical C# files directly at pack
time. There is no second maintained copy of either source.

## Metadata and generated data

`metadata.json` defines the title, pitch, category, tags, terminal size, supported
runtimes, add-ons, three customization suggestions, mode, and state count.
Keep each source at 250 lines or less. All launch demos require no add-ons.

SiteFrames generates `Website/data/gallery.json` as:

```json
{
  "schemaVersion": 1,
  "demos": []
}
```

Each demo keeps its metadata and adds `id`, a `source` object with `powershell`
and `csharp` entries, download URLs, run commands, a `frame`, a
`thumbnail` SVG URL, and per-state cell parity results. Live entries also carry
`url`, `duration` in milliseconds, `frames`, `fps`, and `start` for the existing
player. Their `capture` field identifies them as deterministic snapshot
storyboards: they show the sample's successive states at fixed intervals,
rather than recording keyboard timing or claiming measured performance.

Each language entry in `source` contains `file`, `text`, and `repo`. `text` is
the complete runnable file, read directly from its canonical UTF-8 bytes, including
the final newline. `repo` is its repository-relative path, for example
`Website/examples/gallery/fleet/fleet.ps1` or
`Website/examples/gallery/fleet/Program.cs`. Append it to
`https://github.com/EvotecIT/ConsoleMonster/blob/master/` for the source link.
The existing flat source and repository-path fields remain available.

Readers can copy `fleet.ps1` after `Install-Module ConsoleMonster`, or copy
`Program.cs` into `dotnet new console` after `dotnet add package ConsoleMonster`.
Neither file needs other gallery files. Both explain their data, layout, and live
updates in comments; static screens print once and return.

Frames use the site's existing `frames.css`; replays use its existing player.
No gallery route or layout is generated here.

## Refresh captures

Build the module, capture its PowerShell output on any platform with PowerShell
7, then regenerate site assets:

```terminal
dotnet build ConsoleMonster.PowerShell -c Release -f net8.0
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website gallery
dotnet run --project Website/tools/SiteFrames -c Release -- render Website
```

The renderer compiles and runs every C# source, compares all snapshot cells and
styles with the PowerShell output, and rejects missing or stale captures.
`gallery-review Website <output-directory>` exports standalone HTML and text
for inspection. The normal website CI build includes the same parity check.

For focused validation, run the gallery Pester tests and
`python3 Tests/Build/test_template_gallery.py`; template tests use an isolated template
hive and local package feed.
