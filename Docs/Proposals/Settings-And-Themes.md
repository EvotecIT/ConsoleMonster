# Proposal: one settings model and themes that cover style

Status: the settings core (section 1, without `Get-ConsoleCapability -Explain`) and the settings picker (section 2)
are built. `Get-ConsoleSetting` reports each source, and the picker previews themes, colour depths, glyph sets,
cultures and languages. Style, the checker, the terminal palette and theme-aware exports are still proposals. The
document describes how a user would set up ConsoleMonster once, see the result while choosing, and keep it in every
session and every tool built on ConsoleMonster.

## Why

A user who wants ConsoleMonster to look a certain way has to find out about:

- about thirty `CONSOLEMONSTER_*` environment variables (theme, colour depth, glyphs, mode, language, culture, mouse, motion,
  widths and more);
- profile lines (`Set-ConsoleTheme`, `Enable-ConsoleFormatting` with its features);
- per-widget switches such as `-Border`, `-LineStyle` and `-TimestampFormat`, which have no session-wide default.

None of this is discoverable from inside the terminal, and none of it can be previewed. The colour-blind request that
started this showed it clearly: the right preset existed, but the user did not know it was there, and keeping it meant
editing a profile.

Themes are also only colour. Border style, density and the status marker set are fixed by the code or set per call,
so a theme cannot express "compact, square corners, words instead of symbols".

## Goals

1. One settings model, read from one file, with the same precedence everywhere.
2. Choose settings in the terminal with a live preview, and save them in one step.
3. Themes describe style as well as colour, and can follow the terminal's own colour scheme.
4. Custom themes can be checked for contrast and colour-blind legibility before they are shared.
5. Exports and recordings follow the active theme.

Out of scope: per-built-in-view column customisation (`Set-ConsoleView`), which builds on this file but is a separate
proposal, and new everyday views.

## What already exists

| Piece | Where | State |
| --- | --- | --- |
| Capability detection with recorded decisions | `CapabilityDetector`, `TerminalProfile.Decisions`, `Get-ConsoleCapability` | Shipped |
| Environment overrides | `CONSOLEMONSTER_THEME`, `_COLOR`, `_GLYPHS`, `_MODE`, `_LANGUAGE`, `_CULTURE`, `_NOMOUSE`, `_MOTION` and others | Shipped |
| Semantic theme tokens with 16/256/true-colour values | `ThemeToken` (28 tokens), `ThemeStyle`, `ThemeColor` | Shipped |
| Theme files (JSON and restricted PSD1), validated, with a base preset | `ThemeFile`, `Set-ConsoleTheme -Path`, `Export-ConsoleTheme` | Shipped, schema version 1 |
| Presets, including high contrast and colour-blind-safe | `Theme.Dark`, `Light`, `HighContrast`, `ColorBlindSafe` (light variant on the colour-blind branch) | Shipped |
| Box styles and glyph tiers | `BoxStyle` (Rounded, Single, Double, Heavy, Ascii), `GlyphTier`, `IGlyphSet` | Shipped, chosen per widget or by tier |
| Apps with inputs, selects, toggles, splitters and state | `Show-ConsoleApp`, `ConsoleApp`, `AppState` | Shipped; the picker is built from these |
| Profile snippet | `Enable-ConsoleFormatting -ProfileSnippet` | Shipped; the theme line is on the colour-blind branch |

What is missing is the settings file and the commands around it, style in the theme, a palette-following theme, a
theme checker and theme-aware exports.

## 1. Settings

### The file

One file per user:

- Windows: `%APPDATA%\ConsoleMonster\settings.json`
- macOS and Linux: `$XDG_CONFIG_HOME/consolemonster/settings.json`, default `~/.config/consolemonster/settings.json`

```json
{
  "SchemaVersion": 1,
  "Theme": "ColorBlindSafe",
  "Style": { "Border": "Rounded", "Density": "Comfortable", "StatusMarkers": "Symbols" },
  "Glyphs": "Auto",
  "Mouse": true,
  "Motion": "Full",
  "Language": "en",
  "Culture": "current",
  "Dates": { "Format": "Relative" },
  "Sizes": { "Units": "Binary" },
  "Formatting": { "Include": [ "Output", "Errors", "Progress" ] }
}
```

`Theme` names a preset or a theme file path. Every key is optional; a missing key keeps today's behaviour. An invalid
key or value is ignored with a recorded decision, the same way an invalid environment value is today, so a bad file
never stops a script.

### Precedence

From strongest to weakest:

1. an explicit parameter or C# option on the call;
2. the session (`Set-ConsoleSetting` without `-Save`, `Terminal.Theme = ...`);
3. an environment variable;
4. the settings file;
5. detection and built-in defaults.

Environment variables stay above the file so CI and one-off runs (`CONSOLEMONSTER_COLOR=none`) keep working without
touching a user's file. `Get-ConsoleCapability -Explain` lists each effective setting and which of the five it came
from.

### Commands

```powershell
Get-ConsoleSetting                       # every effective setting and its source
Get-ConsoleSetting Theme
Set-ConsoleSetting Theme ColorBlindSafe   # this session
Set-ConsoleSetting Theme ColorBlindSafe -Save   # this session and the file
Reset-ConsoleSetting Theme -Save
Show-ConsoleSettings                      # the interactive picker
```

`Set-ConsoleTheme` stays as the familiar way to set one setting; `Set-ConsoleTheme ... -Save` writes the file.
`Enable-ConsoleFormatting -Save` stores the chosen features, so the profile needs only `Import-Module ConsoleMonster`
(a module import can apply the stored features). `-ProfileSnippet` stays for people who prefer explicit profile lines.

### C#

```csharp
ConsoleSettings settings = ConsoleSettings.Current;      // effective values with sources
ConsoleSettings.Load(path);                               // tests and tools with their own file
var options = new LiveSessionOptions { Settings = ConsoleSettings.None };   // ignore the user's file
```

**Recommendation:** applications built on ConsoleMonster read the user file by default. The file holds accessibility
preferences (a colour-blind theme, reduced motion, no mouse) that should follow the user into every tool, as `NO_COLOR`
does. An application that needs fixed output, such as a snapshot test or a recording, opts out with
`ConsoleSettings.None` or explicit options, and `TestTerminal` and `HeadlessTerminal` never read it.

## 2. The settings picker

`Show-ConsoleSettings` is an ordinary `ConsoleApp`: a list of sections on the left (Theme, Style, Glyphs, Formatting,
Input and motion, Language and formats) and a live preview on the right. The preview is fixed sample content that
shows every token: a table with pass, warning and fail rows, a status list, a progress bar, a small chart, a panel and
an error.

- Moving through themes or border styles redraws the preview immediately.
- Each choice shows its source (file, environment or default); an environment override is shown as locked with the
  variable's name, so nobody wonders why a choice does not stick.
- Save writes the file; Escape leaves the session unchanged.
- With a colour-blind theme selected, the preview also shows the theme check result from section 4.
- In a non-interactive host it prints `Get-ConsoleSetting` and the commands to change each value, rather than failing.

This is also the main discovery path: the help for `Set-ConsoleTheme` and `Enable-ConsoleFormatting` points to it.

## 3. Theme version 2: style as well as colour

Schema version 2 adds an optional `Style` section to theme files. Version 1 files load unchanged.

```json
{
  "SchemaVersion": 2,
  "Name": "Team Compact",
  "BaseTheme": "Dark",
  "Style": {
    "Border": "Single",
    "Density": "Compact",
    "StatusMarkers": "Words",
    "HeaderDecoration": "Underline",
    "Background": "Dark"
  },
  "Tokens": { }
}
```

- `Border`: the default `BoxStyle` for panels, tables and rules. A widget's own `-Border` still wins. `Ascii` is still
  forced by the ASCII glyph tier.
- `Density`: `Comfortable` (today) or `Compact` (no blank lines between sections, tighter table padding, single-line
  panel headers).
- `StatusMarkers`: `Symbols` (✓ ✗ ▲, today), `Words` (`[PASS]`, `[FAIL]`, the ASCII forms at any glyph tier) or `Both`.
  Words help screen-reader users and people who find small symbols hard to tell apart.
- `HeaderDecoration`: how table and section headers are emphasised when the token's decoration is not enough.
- `Background`: `Dark` or `Light`, the background the theme is made for. Presets set it; a custom theme inherits it
  from `BaseTheme` or states it, and the checker and exports use it to pick a default page background.

Settings-file `Style` values override the theme's `Style`, so a user can keep a team theme's colours with their own
density.

### A theme that follows the terminal

A new `Terminal` preset maps every token to one of the terminal's 16 palette colours (green for pass, red for fail,
yellow for warning, blue for info and so on) and emits palette indexes at every colour-enabled depth, never RGB. With no colour (`NO_COLOR`,
`CONSOLEMONSTER_COLOR=none`, plain mode) it emits no colour at all, like every other theme. The terminal's
own scheme then decides the actual colours, so ConsoleMonster always matches it, and a user who has already set up a
colour-blind or low-vision scheme in their terminal gets it in ConsoleMonster without choosing anything.

This needs a `ThemeColor` that stays a palette index at 256-colour and true-colour depths (today a `ThemeColor` always
has an RGB value and uses its 16-colour index only on 16-colour terminals), and theme files need a way to write it,
for example `"Foreground": { "Palette": 2 }`.

### Importing colour schemes

`Import-ConsoleTheme` converts a terminal colour scheme into a ConsoleMonster theme file:

- a Windows Terminal scheme (`settings.json` `schemes` entry);
- an iTerm2 `.itermcolors` file;
- a base16 YAML scheme.

The importer assigns tokens from the scheme's palette (pass from green, fail from red and so on), then runs the theme
check from section 4 and reports any pair that fails, so an imported scheme is never silently unreadable.

## 4. Checking a theme

```powershell
Test-ConsoleTheme ./team-theme.json
Test-ConsoleTheme -Theme ColorBlindSafeLight -Background White
```

For each status token it reports:

- WCAG contrast against the colour the text is actually drawn on: the token's own background when it has one (for
  example `Critical` in `ColorBlindSafe` is dark text on rose), otherwise the intended page background: the
  background given with `-Background`, or the default dark or light terminal background for the theme's variant
  (`Surface` is for header and footer bars, not the page). A custom theme states its variant with the
  `Style.Background` field of section 3 (`Dark` or `Light`); without it and without `-Background`, the contrast
  checks report that the background is unknown instead of guessing;
- the CIELAB distance between pass, warning and fail text colours in normal vision and under simulated protanopia,
  deuteranopia and tritanopia (full severity, Machado et al. 2009);
- whether the statuses can be told apart without colour: each status's rendered marker (its symbol or word at the
  current glyph tier, and its decorations) must differ from every other status's. A decoration they all share, such
  as bold for pass, warning and fail in `HighContrast`, does not count, and when the markers cannot be rendered the
  result is unknown rather than a pass;
- for a token drawn as a filled block, such as `Critical` in the colour-blind presets (dark text on the fail colour),
  that it differs from the text tokens by its fill, not by hue. Sharing the fail hue is intended (critical is a
  stronger fail), so it is not compared by distance with fail.

`AccessibleThemeTests` already checks the pass, warning and fail distances; the fill rule is added there with the
checker.

A theme that uses palette indexes, such as the `Terminal` preset, has no RGB values of its own. The checker takes the
palette with `-Palette` (a list of 16 colours, or an imported terminal scheme) and otherwise reports its contrast and
distance checks as indeterminate, while still checking the non-colour markers.

The output is an ordinary ConsoleMonster view with pass and fail rows, and objects for scripts. Built-in and custom
themes are held to the same bar.
`-Strict` makes failures terminating, for CI in repositories that ship a team theme.

## 5. Exports follow the active theme

Exports have two kinds of input, and the theme reaches them differently:

- **Widgets and objects** (`Export-ConsoleFrame` with a widget or ordinary objects, and recordings that re-render
  dashboard widgets from recorded events) are rendered at export time with the built-in theme that matches
  `ExportTheme` (`Auto`, `Dark`, `Light` or `HighContrast`). A session using `ColorBlindSafe` or a team theme therefore
  exports in different colours from what the user saw. `ExportOptions.DarkTheme` and `LightTheme` already allow a
  custom theme in C#.
- **Captured frames** (a `CellBuffer` from a snapshot, `ScreenCapture` or a session recording) already carry their cell
  colours. `ExportTheme` only chooses the page palette for them: background, default foreground and chrome.

Change:

- Widgets and objects render with the active theme by default.
- Captured frames keep their cell colours, as today. The default page palette becomes the one that matches the theme
  the frame was captured with: the terminal background recorded with the capture when there is one, otherwise the
  default dark or light background for that theme's variant. Captured colours then sit on the background they were
  chosen for. Frames that do not record their theme keep today's `Auto` behaviour.
- `-Theme Dark|Light|HighContrast` keeps working as an explicit choice for both inputs, and a new `-Theme Current` names
  the default. Documentation and website captures keep fixed themes through their existing explicit options.

## Delivery

One PR per phase, each usable on its own:

1. **Settings core.** The file, precedence, `Get-`/`Set-`/`Reset-ConsoleSetting`, `-Save` on `Set-ConsoleTheme` and
   `Enable-ConsoleFormatting`, `Get-ConsoleCapability -Explain`, the C# `ConsoleSettings` API and the opt-out.
2. **Theme v2 and exports.** The `Style` section, `Density` and `StatusMarkers` in the widgets that draw borders,
   spacing and status, and exports that follow the active theme.
3. **Picker and checker.** `Show-ConsoleSettings` and `Test-ConsoleTheme`.
4. **Terminal palette and import.** The `Terminal` preset, palette-index colours and `Import-ConsoleTheme`.

Each phase adds its tests at the boundary that owns the behaviour: precedence in the settings resolver, style in the
widgets that read it, and the checker against the built-in presets. Visual changes get golden frames for the preview
content in Dark, Light and ColorBlindSafe.

## Open questions

- Should module import apply stored `Enable-ConsoleFormatting` features automatically, or only when the profile calls
  `Enable-ConsoleFormatting` without parameters? Automatic is simpler for users; explicit is more predictable for
  people who share profiles across machines. My preference is explicit, with the picker writing that one line.
- Should a machine-wide settings file exist for managed environments (below the user file)? Not needed for the first
  phase.
- `Density = Compact` touches many widgets. The first version can cover tables, panels, lists and dashboards and leave
  the rest at today's spacing.
