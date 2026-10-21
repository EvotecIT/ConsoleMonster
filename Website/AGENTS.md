# Agent guide: the consolemonster.dev website

## Purpose

This folder builds **consolemonster.dev**, a PowerForge.Web site (`site.json`, `pipeline.json`, `themes/consolemonster`).
It is the home of ConsoleMonster 2.0: the widget gallery, tables, live dashboards, interactive screens, the PowerShell
module, guides, docs, benchmarks and the .NET and PowerShell references. GitHub Pages serves the site at consolemonster.dev,
with DNS managed in Cloudflare. `.github/workflows/website-ci.yml` validates PRs and `website-deploy.yml` publishes master
through PowerForge's shared deployment workflow. The CI pipeline runs the renderer tests, regenerates terminal assets,
and rejects output that differs from the committed files before building and auditing the site.

## Build

```powershell
pwsh Website/build.ps1 -Serve            # dev pipeline + local server on :8080
pwsh Website/build.ps1 -CI               # CI-mode pipeline (engine lock, verify, SEO doctor, audit, optimize)
```

`build.ps1` locates PowerForge.Web.Cli from `-PowerForgeRoot`, `POWERFORGE_ROOT`, a sibling `PSPublishModule` checkout or
the global `powerforge-web` tool. The engine is pinned in `.powerforge/engine-lock.json`; CI builds that ref.
A local CLI older than the lock can fail the API steps:
build the pinned ref from a `git archive` of PSPublishModule instead.

Caching: the CI pipeline's optimize step hashes every CSS and JS file into a content-addressed name (`hashAssets` in
`pipeline.json`) and rewrites the references, so a deploy never pairs new HTML with old styles or scripts. Prism's own
scripts keep fixed names because `site.js` builds their URLs. What scripts fetch by a fixed URL at run
time (frame bundles, replays, recordings, Prism) carries the page's content version instead. Automatic Prism injection is
off; `site.js` loads its core and components with that version on every page type. Token colours already live in the
shared, hashed `base.css`.
`render` writes a hash of those folders to `data/assets.json`, `partials/head.html` puts it in `<meta name="cm-runtime">`, and `frames.js` and
`site.js` add it as `?v=`. `build/Export-ApiChrome.ps1` carries the same meta tag into both reference families.

## Real output only

Every terminal frame on the site is ConsoleMonster output. Nothing is a screenshot and nothing is drawn by hand.
`tools/SiteFrames` (C#, referencing `../ConsoleMonster` and `../ConsoleMonster.Examples`) produces all of it:

```powershell
dotnet build ConsoleMonster.PowerShell -c Release                              # the module the PowerShell captures load
dotnet run --project Website/tools/SiteFrames -c Release -- capture Website    # Windows only; optional area: widgets, home, live, powershell, benchmarks, docs, demos
dotnet run --project Website/tools/SiteFrames -c Release -- render Website     # deterministic: frames, replays, data
pwsh Website/build/Render-SiteImages.ps1                                       # social card, touch icon, app icons, favicon.ico (needs Edge)
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
- C# examples are complete programs under `examples/**/*.cs` (a `Program.cs` with its `using` lines and top-level
  statements). `Capture/CSharpRunner.cs` compiles each exactly as a `dotnet new console` project would (C# 12, implicit
  usings, warnings and unused `using` lines fail) against this repository's ConsoleMonster only, and runs it to make the
  frame. Programs that need other packages (the hosting and logging demos) are projects of their own under
  `examples/demos/<name>/`, built with `dotnet build`. PowerShell examples live in `examples/**/*.ps1` (UTF-8 with a BOM
  for Windows PowerShell 5.1) and are shown verbatim.
- Demos (`Pages/DemosPage.cs`): one recording per feature, captured from `examples/demos/*.ps1` on PowerShell 7 or from
  programs (the example projects, and `tools/TemplateDemo`, which compiles the template's own `Program.cs` with scripted
  keys). Each recording is stamped with a fingerprint of everything that made it, and `render` reports a stale one.
  `render` writes `static/replays/demo-*.json` and `data/demos.json` with the sources read from those
  same files; `#region more` folds setup into the full program and `#region hidden` hides presentation lines (an echoed
  prompt) from the short form. Captures strip window titles and build with `ContinuousIntegrationBuild` so no local path
  is committed. Pages embed a demo with the `cm-demo` shortcode (`id`, `heading`, `autoplay`); its words and the
  install line come from `data/copy.json` (`demos`).
- Generated, never hand-edited: `data/widgets.json`, `tables.json`, `live.json`, `interactive.json`, `powershell.json`, `demos.json`,
  `figures.json`, `home.json`, `benchmarks.json`, `visuals.json`; `static/frames/**`, `static/replays/**`,
  `static/recordings/**`, `static/site/**`, `static/css/frames.css`, `static/assets/player/**`, `static/apple-touch-icon.png`,
  `static/favicon.ico`, and
  `tools/SiteFrames/glyphs.txt` (every glyph the frames use). `static/site.webmanifest` and `static/favicon.svg` are
  hand-written.
- `partials/start-cards.html` is the entry strip of cards that closes the home page and the 404 page; its CSS is in
  `assets/pages.css`, which both load.

## Design system

The site is set in the medium it documents: a terminal cell grid.

- **Colour.** Tokens live in `themes/consolemonster/assets/tokens.css` only, taken from ConsoleMonster's built-in themes
  (`ConsoleMonster/Styling/Theme.BuiltIn.cs`). Pass, warn and fail keep their status meaning; navigation and tab keys use
  the six chart series colours (`--cm-s1` to `--cm-s6`). Dark pages sit on slate (`--cm-page`); terminal screens use the
  exporter's Campbell defaults (`#0c0c0c` / `#cccccc`) and `#ffffff` / `#24292f` for light frames.
- **One accent.** `--cm-accent` fills actions (buttons), `--cm-link` colours links, the current item in a list (docs
  contents, menu destinations, the API type tree) has a 2 px accent bar on its left, and the chosen option in a switch or
  filter has the accent tint (`--cm-focus-bg`), as does the header chip naming the section you are in. Hover is neutral
  (`--cm-hover`, a wash of the ink colour, or `--cm-line-strong` on borders), so it never looks like the current item. Key
  colours (`--cm-s1`-`--cm-s6`) mark categories, never state; a card echoes its own key colour on its border when hovered.
- **Type.** Space Grotesk 700 (self-hosted Latin subset from @fontsource, OFL, `--cm-display`) for h1/h2 page and
  section titles, with a metric-matched Arial Bold fallback face so the swap does not reflow; JetBrains Mono (self-hosted,
  OFL) for small headings, navigation, controls, code and frames; the system sans for prose. Only 700 ships, so title
  rules ask for 700, not 800.
- **Stage and masthead.** Product pages open with `partials/stage.html`, a band that stays dark in both themes, with real
  output beside or under the title. Docs and guides open with `partials/masthead.html`. No eyebrow labels above either.
- **Screens.** Every frame uses `partials/screen.html`: a window bar with the real terminal profile as chips, then the
  frame. Static frames have a dark and a light rendering and follow the site theme; replays, stage art and captured
  PowerShell output stay dark. A frame scales to fit its column down to 9 px (5.5 px on phones, so a 100-column frame
  fits a 400 px screen) and then scrolls inside its window, which then shows a scroll hint in its bar, so the page never
  scrolls sideways. Replays can carry chapter marks (`markers` in the player data) under the position bar.
- **Players.** Replays play in ConsoleMonster's own player: `render` copies `ConsoleExporter.PlayerScript` and
  `PlayerStyles` to `static/assets/player/` (generated, never edit them) and writes `static/replays/*.json` in the engine's
  player-data format (`Recording.ExportPlayerData` for live-event recordings; `Replay/PlayerData.cs` writes the same
  format for pseudo-console runs and storyboards, which are not event recordings). `partials/player.html` shows a mid-run
  poster without JavaScript; `assets/frames.js` only mounts the engine player near the viewport, picks the phone
  rendering, opens it at the poster's moment (the player's `start` option) and moves the player's screen into the site
  window. A player is as wide as its terminal at its largest font (`--cols` and `--cm-max-font` on `.cm-replay`), so the
  window and its controls are always one width. Engine gaps go in the PR's
  engine notes, not into a fork of the player.
- **Spacing.** One scale in `tokens.css` (`--cm-space-1` to `--cm-space-8`) and one rhythm between sections
  (`--cm-section`, 96 px wide and 64 px on phones); a band keeps 64 px on each side of its edge. Use the tokens for every
  gap between blocks rather than new pixel values. The home hero is one centred column whose players share one stage width
  (`--cm-stage-w`), so switching between Everyday and Monster never moves or resizes the window.
- **Selection.** Tabs, tier switches and width controls use `.cm-seg`; the chosen option is drawn as ConsoleMonster's
  focus row. Tabs follow the ARIA tab pattern, switches the radio group pattern (arrow keys move and select).
- **Panels.** `.cm-panel` is ConsoleMonster's `Panel` widget in CSS (the title set into the top border); the gallery uses
  it for specimens. Do not nest panels.
- **Reference pages.** `/api/` and `/powershell/commands/` come from PowerForge's apidocs template; `api-base.css` is its
  fallback stylesheet and `api.css` sets that markup in this design system (calm header, light facts line, page-coloured
  sidebar tree, member cards, compact toolbar). Its rules are scoped to `html[data-theme] body.pf-api-docs` to out-rank the
  fallback's per-theme rules. Problems in the template's own markup go to PowerForge, not into more overrides here.
- **Header and footer.** One partial each, on every page including the API and command references (`layout: chrome`
  pages and `build/Export-ApiChrome.ps1`). The current page is a chip after the groups; no header slot changes size
  between pages. Do not style the header or footer per page.
- **Copy.** Plain, accurate sentences from the README, the pull request and `Docs/`. Hand-written page copy lives in
  `data/copy.json`. No all-caps labels, no numbered markers unless the content is a sequence, no arrows on links, and no
  numbers that are not in the repository or measured by the tool.

## The README follows the site

`README.md` at the repository root is the front page on GitHub and in the NuGet package. Its code blocks are marked
`<!-- example: <source> -->` (a file under `examples/`, shown whole, or `content/<page>.md#N`, that page's Nth code
block); `dotnet run --project tools/SiteFrames -c Release -- readme .` writes them, and the render fails when one has
drifted (`tools/SiteFrames/Readme.cs`). Its images are frames the site shows, written as pages by the render and
screenshotted by `build/Render-SiteImages.ps1` into `static/readme/`; the README links them with absolute
raw.githubusercontent.com URLs. Website CI runs on README changes for this check. The same command writes
`ConsoleMonster/README.nuget.md`, the README as nuget.org shows it (Markdown images instead of HTML), which the package
packs.

## The site is self-sufficient

Every page lets a visitor do the thing without leaving the site: paste this, do this, then this, and this is the code
to run.

- **The code is on the page.** Complete, runnable code for what the page shows, PowerShell first and C# second, with Copy
  and the setup it needs (`Install-Module`/`Import-Module`, `using` and `dotnet add package`, the profile line, the
  `dotnet new` command). Lead with the essential widget calls; the whole program is under "Full program".
- **The shown code is the code that made the frame.** Every frame and recording comes from running the exact script or
  program the page shows, complete: `Import-Module ConsoleMonster` (or the `using` lines), its data defined at the top,
  no `. .\shared.ps1`. Anything only the recording needs (scripted keys, a fixed screen size, a typed `$ ssh` line before a
  script, the repository build of the module) lives in the tool, never in the shown code: the capture runner puts a link
  to this repository's `Module` folder first on PSModulePath, so `Import-Module ConsoleMonster` loads the repository build
  (`Capture/PowerShellRunner.cs`; the module's example test does the same). A demo can run a published script with
  arguments (`Script`, `Arguments`, `Prompt` in `Pages/DemosPage.cs`); a published script is also served from the site
  under `/examples/` so steps can download it from here. `SourceSnippets` has no way to hide code from the full form.
- **No repository paths in shown code.** Never an `&` call into the repository, a `../` path, a repository folder name or
  `artifacts.ps1`. Code bars name the file only; `*Repo` keys (`powershellRepo`, `csharpRepo`) or `code_repo` in a
  template add the quiet "View on GitHub" link. `tools/SiteFrames/CodeGuard.cs` fails the render when a string in
  `data/*.json` (other than frames and links) or a fenced block in `content/**/*.md` reaches into the repository, and
  compiles every C# block that is a complete program. `tools/SiteFrames/FrameSources.cs` fails it when a frame has no
  code linked to it (in its data entry, through a `*Repo` link to a program the page tells the reader to run, or
  `sourceOf` a sibling entry), or when a docs figure hides its script (`script="false"`) that the page shows nowhere else.
- **Steps are steps.** Where a page describes doing something (install, enable in the profile, run at login, host in a
  generic host or ASP.NET, record and replay), it gives numbered, copyable steps. Demos do this through
  `copy.json` `demos.<id>.use.steps` (a sentence and the code for each step).
- **The result is shown.** The frame or recording beside the code is what that code produces.
- **No dead ends.** Every guide and docs page ends with what to try next, linked.
- **GitHub is secondary.** A link to the repository is a "View on GitHub" extra (`use.github`), never the only way in.

## Pages

- `content/pages/`: home, widgets, tables, live, interactive, powershell, benchmarks, 404 and the two chrome pages.
- `content/docs/` and `content/guides/`: reference topics and migration guides. Frames go in with the `cm-frame`
  shortcode, which reads `data/figures.json` (`id`, `ids` for several, `script="true"` to add the PowerShell that
  produced it).
- The docs catalogue (`layouts/docsindex.html`) is built from the docs pages: `content/docs/_index.md` is a section, so
  the template gets every page as `items`. Each page's front matter names its `group` (one of `docsCatalog.groups` in
  `data/copy.json`), a `summary`, `apis` (shown as code) and `chips` (topics), and a `card`: a figure or demo id whose
  miniature `tools/SiteFrames` (`Pages/DocCards.cs`) writes to `static/site/cards/` and lists in `data/visuals.json`. A
  page without a known group or card fails the render. The guides index still takes its cards from `data/copy.json`.
  Quote a front matter value that contains `: `.

## Validation

Run the CI-mode pipeline, then check every page type in both themes at 1440 px and 390 px: no horizontal page scroll,
readable frames, keyboard access to tabs, tier switches, width controls and players.
