# Agent guide: the consolemonster.dev website

## Purpose

This folder builds **consolemonster.dev**, a PowerForge.Web site (`site.json`, `pipeline.json`, `themes/consolemonster`).
It is the home of ConsoleMonster 2.0: the widget gallery, tables, live dashboards, interactive screens, the PowerShell
module, guides, docs, benchmarks and the .NET and PowerShell references. Hosting (GitHub Pages) is set up by the owner;
this folder only builds and validates the site, and `.github/workflows/website-ci.yml` runs the CI-mode pipeline.

## Build

```powershell
pwsh Website/build.ps1 -Serve            # dev pipeline + local server on :8080
pwsh Website/build.ps1 -CI               # CI-mode pipeline (engine lock, verify, SEO doctor, audit, optimize)
```

`build.ps1` locates PowerForge.Web.Cli from `-PowerForgeRoot`, `POWERFORGE_ROOT`, a sibling `PSPublishModule` checkout or
the global `powerforge-web` tool. The engine is pinned in `.powerforge/engine-lock.json` (PSPublishModule `77a10b3d`, which
has the PowerShell command reference mode); CI builds that ref. A local CLI older than the lock can fail the API steps:
build the pinned ref from a `git archive` of PSPublishModule instead.

## Real output only

Every terminal frame on the site is ConsoleMonster output. Nothing is a screenshot and nothing is drawn by hand.
`tools/SiteFrames` (C#, referencing `../ConsoleMonster` and `../ConsoleMonster.Examples`) produces all of it:

```powershell
dotnet build ConsoleMonster.PowerShell -c Release                              # the module the PowerShell captures load
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website    # Windows only; optional area: widgets, home, live, powershell, benchmarks, docs
dotnet run --project Website/tools/SiteFrames -c Release -- render Website     # deterministic: frames, replays, data
pwsh Website/build/Render-SiteImages.ps1                                       # social card and touch icon (needs Edge)
```

- `render` is deterministic: it renders in-process with fixed widths, tiers, themes and the en-US culture, replays the
  committed captures and recordings, and writes the generated files listed below. Rerun it after any renderer change and
  commit what changed. It exits with an error when a PowerShell example no longer draws the same cells as its C# twin.
- `capture` runs scripts and showcases inside a Windows pseudo console (ConPTY) of a fixed size, with the terminal profile
  pinned through `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_THEME` and `CONSOLEMONSTER_CULTURE`, and the
  thread culture pinned for built-in cmdlets. It saves the VT streams under `captures/` (`.ansi` for printed output,
  asciicast `.cast` for live runs, plus the host versions and the benchmark machine). Live runs, the stress test and the
  benchmarks carry real timing and measurements, so a new capture differs from the last one; commit captures deliberately.
- The converters are the tool's own: `Frames/FrameHtml.cs` (cells to HTML), `Frames/GlyphPaint.cs` (box drawing, block
  elements and braille painted on the cell grid, so borders join in every font), `Frames/FrameMinimap.cs` (navigation
  thumbnails) and `Vt/VtScreen.cs` (a VT parser that turns captured streams back into cells). No terminal library is used.
- Cell geometry: a cell is 0.6em by 1.2em (JetBrains Mono). Every run except a row's last is a box exactly as wide as its
  cells (`.cN` classes in `static/css/frames.css`), and the frame font size is snapped so cells are whole layout units, so
  bars and borders line up from row to row.
- C# examples live in `tools/SiteFrames/Catalog/*.cs` and `tools/SiteFrames/Pages/InteractivePage.cs` between
  `// <example>` markers; the site shows exactly those lines. Table showcases show their method from
  `ConsoleMonster.Examples`. PowerShell examples live in `examples/**/*.ps1` (UTF-8 with a BOM for Windows PowerShell 5.1)
  and are shown verbatim.
- Generated, never hand-edited: `data/widgets.json`, `tables.json`, `live.json`, `interactive.json`, `powershell.json`,
  `figures.json`, `home.json`, `benchmarks.json`, `visuals.json`; `static/frames/**`, `static/replays/**`,
  `static/recordings/**`, `static/site/**`, `static/css/frames.css`, `static/apple-touch-icon.png`, and
  `tools/SiteFrames/glyphs.txt` (every glyph the frames use).

## Design system

The site is set in the medium it documents: a terminal cell grid.

- **Colour.** Tokens live in `themes/consolemonster/assets/tokens.css` only, taken from ConsoleMonster's built-in themes
  (`ConsoleMonster/Styling/Theme.BuiltIn.cs`). Pass, warn and fail keep their status meaning; navigation and tab keys use
  the six chart series colours (`--cm-s1` to `--cm-s6`). Dark pages sit on slate (`--cm-page`); terminal screens use the
  exporter's Campbell defaults (`#0c0c0c` / `#cccccc`) and `#ffffff` / `#24292f` for light frames.
- **Type.** JetBrains Mono (self-hosted, OFL) for headings, navigation, controls, code and frames; the system sans for
  prose.
- **Stage and masthead.** Product pages open with `partials/stage.html`, a band that stays dark in both themes, with real
  output beside or under the title. Docs and guides open with `partials/masthead.html`. No eyebrow labels above either.
- **Screens.** Every frame uses `partials/screen.html`: a window bar with the real terminal profile as chips, then the
  frame. Static frames have a dark and a light rendering and follow the site theme; replays, stage art and captured
  PowerShell output stay dark. A frame scales to fit its column down to 9 px and then scrolls inside its window, so the
  page never scrolls sideways.
- **Players.** `partials/player.html` shows the last frame without JavaScript; `assets/frames.js` loads the timeline when
  asked or when an autoplaying player scrolls into view, and nothing plays by itself with reduced motion.
- **Selection.** Tabs, tier switches and width controls use `.cm-seg`; the chosen option is drawn as ConsoleMonster's
  focus row. Tabs follow the ARIA tab pattern, switches the radio group pattern (arrow keys move and select).
- **Panels.** `.cm-panel` is ConsoleMonster's `Panel` widget in CSS (the title set into the top border); the gallery uses
  it for specimens. Do not nest panels.
- **Header and footer.** One partial each, on every page including the API and command references (`layout: chrome`
  pages and `build/Export-ApiChrome.ps1`). The current page is a chip after the groups; no header slot changes size
  between pages. Do not style the header or footer per page.
- **Copy.** Plain, accurate sentences from the README, the pull request and `Docs/`. Hand-written page copy lives in
  `data/copy.json`. No all-caps labels, no numbered markers unless the content is a sequence, no arrows on links, and no
  numbers that are not in the repository or measured by the tool.

## Pages

- `content/pages/`: home, widgets, tables, live, interactive, powershell, benchmarks, 404 and the two chrome pages.
- `content/docs/` and `content/guides/`: reference topics and migration guides (index pages use the `docsindex` and
  `guidesindex` layouts, whose cards come from `data/copy.json`). Frames go in with the `cm-frame` shortcode, which reads
  `data/figures.json` (`id`, `ids` for several, `script="true"` to add the PowerShell that produced it).

## Validation

Run the CI-mode pipeline, then check every page type in both themes at 1440 px and 390 px: no horizontal page scroll,
readable frames, keyboard access to tabs, tier switches, width controls and players.
