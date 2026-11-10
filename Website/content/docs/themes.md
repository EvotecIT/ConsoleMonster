---
title: Themes
description: Built-in themes, following your terminal palette, importing schemes and checking accessible variants.
slug: themes
order: 6
group: start
card: theme-dark
chips: [Themes, Terminal, Schemes]
summary: "Choose a theme, follow your terminal or import a colour scheme."
---

A theme restyles every widget at once. Widgets never name colours directly; they use theme roles such as `Pass`, `Muted` and `Accent`, and a theme maps each role to a 24-bit colour with its 256 and 16-colour fallbacks, so a role stays distinguishable on every tier. The [full list of roles](#roles-and-markup) is below.

{{< cm-frame ids="theme-dark,theme-light,theme-contrast" caption="One summary in `Theme.Dark`, `Theme.Light` and `Theme.HighContrast`." >}}

## Choosing the theme

- `Show-ConsoleSettings`, or `Set-ConsoleSetting Theme ... -Save`, for every session (below).
- `CONSOLEMONSTER_THEME=light|dark|auto` (default `auto`), or `TerminalProfile.WithThemePreference` in code.
- `auto` reads the console background colour in conhost. In Windows Terminal, which does not report its scheme to console programs, `auto` picks the dark theme; set `CONSOLEMONSTER_THEME=light` for a light scheme.
- `Set-ConsoleTheme` in PowerShell, `LiveSessionOptions.Theme` for one session.

To switch for the rest of the session in PowerShell, or for every write that follows in C#:

<div class="cm-lang" data-lang="powershell">

```powershell
Import-Module ConsoleMonster

Set-ConsoleTheme Light
Write-ConsoleText -Markup '[pass]√ 412 passed[/]  [warn]▲ 51 warnings[/]  [fail]✗ 23 failed[/]'
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Theme = Theme.Light;                                   // every write from here on
Terminal.Write(new TextBlock("[pass]√ 412 passed[/]  [warn]▲ 51 warnings[/]  [fail]✗ 23 failed[/]"));
Terminal.Theme = Theme.HighContrast;
Terminal.Write(new Rule("[bold]High contrast[/]"));
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;

Terminal.Theme = Theme.Light;                                   // every write from here on
Ui.Markup("[pass]√ 412 passed[/]  [warn]▲ 51 warnings[/]  [fail]✗ 23 failed[/]").Show();
Terminal.Theme = Theme.HighContrast;
Ui.Rule("[bold]High contrast[/]").Show();
```

</div>

## Follow your terminal

Choose **Terminal (follow my terminal)** in the Theme list in `Show-ConsoleSettings`, or use:

```powershell
Set-ConsoleTheme Terminal -Save
Test-ConsoleTheme -Theme Terminal
Export-ConsoleTheme -Path ./terminal-theme.json
```

`Terminal` keeps ANSI palette indexes even at true-colour depth, so your terminal application chooses the colours.
Before ConsoleMonster opens the keyboard, it asks for the sixteen colours with OSC 4 and the default foreground and
background with OSC 10/11. Each guarded exchange has a 100 ms timeout and runs only in an interactive Unix terminal,
never with redirected input/output or in CI. `Terminal.Palette` contains the result, or null when it is unavailable.
The probe cannot run after another ConsoleMonster input reader has started.

Exports and recordings use the measured RGB palette when available. Otherwise they use the background variant's
default colours; the capability decisions record that fallback. An unavailable palette leaves colour checks Unknown.
`Auto` continues choosing the built-in light or dark theme from the detected background.

## Import a colour scheme

The [colour scheme gallery](/docs/colour-schemes/) shows Dracula, Nord, Solarized, Gruvbox, Catppuccin and Tokyo Night,
each faithful and accessible, with what the checker says about it. Any other scheme can be imported from its file:

```powershell
Import-ConsoleTheme -Path ./my-scheme.json -OutFile ./my-theme.json
Set-ConsoleTheme -Path ./my-theme.json

Import-ConsoleTheme -Path ./my-scheme.itermcolors -Accessible -OutFile ./my-accessible-theme.json
Set-ConsoleTheme -Path ./my-accessible-theme.json
```

Import accepts a single Windows Terminal scheme object (or `settings.json` containing one `schemes` entry), an XML
iTerm2 `.itermcolors` plist in sRGB, or a flat base16 `.yaml`/`.yml` scheme. Without `-OutFile`, it writes
`<source-name>.theme.json` beside the source. It reports the same verdict as `Test-ConsoleTheme` and imports even when
checks fail. Import does not change the active theme.

A faithful theme chooses colours from the palette unchanged, preferring a normal or bright candidate with 3:1
contrast while keeping the role's hue; grey bright slots do not replace coloured statuses. `Gap` uses the muted
text colour. The accessible variant first chooses status colours from that palette, then adjusts OKLCH lightness
while retaining hue and at least half the source chroma. If that hue cannot meet the bars, it tries another palette
colour in the same family: blue-to-cyan, orange-to-yellow or red-to-magenta. Fail is also
underlined. The report lists the roles **adjusted for contrast**. Arbitrary imported palettes may still fall short;
the report gives the measurements. A version 3 file keeps the embedded scheme, so it can also be selected with
`Set-ConsoleTheme -Path ./my-theme.json -Accessible -Save` without rewriting the source file.

In C#, use `TerminalScheme.Load(path)` and `Theme.FromScheme(scheme, accessible: true)`. See
[theme files](#share-a-team-theme-file) for role overrides and [checking themes](#check-a-theme) for measurements.

## Keep a theme for every session

`Show-ConsoleSettings` opens a picker with a preview that redraws as you move through the themes, glyph sets, colour depths and the other settings, so you can compare them before choosing. Save keeps the choices for every session; Escape leaves everything as it was. Without the picker, `-Save` does the same:

```powershell
Set-ConsoleTheme ColorBlindSafe -Save       # or: Set-ConsoleSetting Theme ColorBlindSafe -Save
Set-ConsoleSetting Glyphs Basic -Save
Get-ConsoleSetting                           # each setting, its value and where it came from
```

Saved settings go to `settings.json` in `%APPDATA%\ConsoleMonster` on Windows and `~/.config/ConsoleMonster` on macOS and Linux. Every PowerShell session and every program built on ConsoleMonster reads it, so nothing needs to go in your profile. A theme can also be the path of a [team theme file](#share-a-team-theme-file).

The strongest choice wins: a theme passed to one call, then `Set-ConsoleSetting` or `Set-ConsoleTheme` in this session, then a `CONSOLEMONSTER_*` [environment variable](/docs/environment/), then the settings file, then detection. Variables beat the file, so CI and one-off runs keep working without touching it, and `CONSOLEMONSTER_SETTINGS=none` ignores the file. In C#, `ConsoleSettings.Current` lists the effective values with their sources, and `ConsoleSettings.FilePath = null` keeps a program's output fixed whatever the user saved.

The light theme meets WCAG contrast on white, and the dark theme keeps titles readable on a light background that could not be detected, because titles, headers and highlights use the terminal's own foreground in bold.

## Frames and status words

A theme is more than colours. Its style picks the frame drawn around panels, tables and inputs, and whether statuses
show as symbols (✓ ✗ ▲) or as words (`[OK]` `[FAIL]` `[WARN]`). Words help screen readers and anyone who finds small
symbols hard to tell apart, and they look the same in every terminal font. Keep your own style whatever the theme:

```powershell
Set-ConsoleSetting Border Single -Save
Set-ConsoleSetting Density Compact -Save
Set-ConsoleSetting StatusMarkers Words -Save
```

`Show-ConsoleSettings` previews these choices. A widget that asks for its own frame (`New-ConsolePanel -Border Double`) keeps
it, and the ASCII glyph tier still draws ASCII. `CONSOLEMONSTER_BORDER`, `CONSOLEMONSTER_DENSITY` and `CONSOLEMONSTER_STATUS_MARKERS` set them
for one run; in C#, `Theme.WithAppearance` gives a theme its style and `TerminalProfile.WithBorder` and
`WithDensity` and `WithStatusMarkers` override it.

`Comfortable` is the default spacing. `Compact` removes boxed table cell padding, reduces
unboxed table and list gaps from two cells to one, removes default horizontal panel padding, and closes the
default gap between framed layout tiles. Unframed children keep at least one cell of default separation;
larger default gaps shrink by one. Explicit padding, gaps and
column gaps keep the values the author set. In C#, use `new ThemeAppearance(ThemeDensity.Compact)` with
`Theme.WithAppearance`, or `profile.WithDensity(ThemeDensity.Compact)` for an override.

Exports follow your choice too: once you pick a theme, `Export-ConsoleFrame` draws in it on the background it is made
for. `-Theme Auto` still gives both the dark and the light rendering, and `-Theme Current` asks for the theme in use.

## Roles and markup

Every role is also a markup tag, written in lower case (`[pass]OK[/]`, `[muted]12:00[/]`), and works as a background too (`[white on fail]`). In PowerShell, `-Role` on `New-ConsoleText` and `Write-ConsoleText` takes the same names; in C#, the roles are the `ThemeToken` values. Theme files call them tokens. Without colour, a status role adds its symbol, so status is never shown by colour alone.

| Role | Used for | Markup tag |
|---|---|---|
| `Text` | Body text | `[text]` |
| `Muted` | Secondary text: timestamps, units, hints | `[muted]` |
| `Subtle` | Very low emphasis: empty bar tracks, guides, separators | `[subtle]` |
| `Accent` | The primary accent: titles, active items, the main series | `[accent]` |
| `Accent2` | A secondary accent hue | `[accent2]` |
| `Pass` | Success, passed | `[pass]` |
| `Fail` | Failure, error | `[fail]` |
| `Warn` | Warning | `[warn]` |
| `Info` | Informational | `[info]` |
| `Skip` | Skipped, not applicable | `[skip]` |
| `Running` | In progress | `[running]` |
| `Pending` | Queued, not started | `[pending]` |
| `Border` | Panel and table borders | `[border]` |
| `Title` | Panel titles | `[title]` |
| `Header` | Table and section headers | `[header]` |
| `Highlight` | Emphasised values, key figures | `[highlight]` |
| `Surface` | Header and footer bars; carries a background | `[surface]` |
| `SurfaceAccent` | Accent text on `Surface` | `[surfaceaccent]` |
| `Series1` to `Series6` | Chart series one to six | `[series1]` to `[series6]` |
| `Focus` | The focused row or control in prompts, menus and forms; carries a background | `[focus]` |
| `Strong` | Emphasised text without a background fill | `[strong]` |
| `Low` | A low-severity finding | `[low]` |
| `Critical` | A critical finding, always emphasised | `[critical]` |
| `Gap` | An evidence gap that prevented evaluation | `[gap]` |

## Reduction per tier

Colours given as hex, `rgb()` or palette names are reduced for each tier the way the SGR encoder sends them: to the nearest of the 256 xterm colours on a 256-colour console, and to the nearest of the 16 console colours on a 16-colour one. Theme roles use the 256 and 16-colour values the theme defines instead of the nearest match. The [widget gallery](/widgets/) shows every widget at each tier.

## Share a team theme file

Export a starting palette, edit its roles (the `Tokens` in the file) and load it for the session:

```powershell
Import-Module ConsoleMonster

Set-ConsoleTheme -Theme Dark
Export-ConsoleTheme -Path ./team-theme.json -PassThru
# Edit the file, then apply it.
Set-ConsoleTheme -Path ./team-theme.json
Write-ConsoleText 'Deployment ready' -Role Pass
```

Choose `Dark`, `Light`, `HighContrast` or `ColorBlindSafe` with `-Theme`.
`ColorBlindSafe` keeps pass, warning and fail distinct with red-green colour
blindness (blue, yellow and underlined rose) and switches to its light variant when it
detects a light background (otherwise it stays dark); `ColorBlindSafeLight` and `ColorBlindSafeDark` pick one
explicitly. Status widgets retain their symbols and words. To keep the theme, or
the team file, in every session, add `-Save` (`Set-ConsoleTheme -Path ./team-theme.json -Save`).
Export any preset
as a starting file. Changing the widget theme does not change the terminal
application's background.

Use `.psd1` instead of `.json` for a restricted PowerShell data file. The extension
selects the format. Export replaces an existing destination, whose parent folder
must exist; `-WhatIf` previews the write and `-PassThru` returns its `FileInfo`.
Export does not change the active theme. Invalid files raise an error and leave
the previous theme active.

A partial JSON file can inherit from a built-in palette. Save this as
`team-theme.json` to change the accent while keeping the other Dark roles:

```json
{
  "SchemaVersion": 1,
  "Name": "Team Dark",
  "BaseTheme": "Dark",
  "Tokens": {
    "Accent": {
      "Foreground": { "TrueColor": "#7dd3fc", "Ansi16": 14, "Ansi256": 117 },
      "Decoration": "Bold"
    }
  }
}
```

`Name` and `Tokens` are required; `SchemaVersion` defaults to `1`. With `"SchemaVersion": 2` a file can also set its
[style](#frames-and-status-words): `"Style": { "Border": "Single", "Density": "Compact", "StatusMarkers": "Words", "Background": "Dark" }`. Missing tokens
and style fields inherit from `BaseTheme`. Styles accept `Foreground`,
`Background` and `Decoration`. Explicit `null` colours use terminal defaults;
`"Decoration": "None"` clears inherited decoration. Fallback indices use ANSI
palette order: `Ansi16` is `0`–`15` (`-1` uses the terminal default), while
`Ansi256` is `0`–`255` (`-1` requests automatic reduction). Complete exports
resolve inheritance and include every token. Unknown keys, tokens and invalid
values are rejected before application.

### Try it: the same report before and after

Save [team-dark.json](/examples/themes/team-dark.json) and [team-light.json](/examples/themes/team-light.json) next to the program. The script builds a status list, a table and a log with native PowerShell cmdlets, then draws the same widgets with the built-in palette and with the team file.

{{< cm-try key="theme-files" ids="theme-files-before-120,theme-files-after-120" project="ThemeReport" file="theme-files.ps1" run="pwsh ./theme-files.ps1 -BaseTheme Dark -Width 120" csrun="dotnet run -- ./team-dark.json" caption="The same report at 120 columns, before and after loading the team file. Switch the site theme to see the matching light palette." >}}

On a light terminal use `-BaseTheme Light` and `team-light.json`. The script leaves the team theme active; `Set-ConsoleTheme -Theme Dark` or `-Theme Light` restores a built-in, and `-ExportDirectory ./presets` writes starting files for all five presets. Without an argument the C# program uses the current terminal theme; with a file it scopes the loaded palette to the report's output target with `Terminal.Use` and restores the target when the report finishes.

{{< cm-frame ids="theme-files-before-60,theme-files-after-60" script="false" caption="The same programs at 60 columns. Status symbols, words and log levels stay visible in both palettes." >}}

### In C#

Add the `ConsoleMonster` NuGet package to a console project, then use the same
file. The core reads JSON; PSD1 belongs to the PowerShell adapter.

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Theme team = ThemeFile.Load("team-dark.json").ToTheme();
Terminal.Theme = team;
Terminal.Write(new TextBlock("[pass]Deployment ready[/]"));
ThemeFile.FromTheme(team).Save("team-snapshot.json");
```

`ThemeFile.Parse` accepts JSON text and `ThemeFile.Import` accepts a data
dictionary. `ToJson` returns complete JSON. Loading and saving do not select a
theme; assigning `Terminal.Theme` does. The same palette can instead be supplied
to `LiveSessionOptions.Theme` for one session.

## Check a theme

`Test-ConsoleTheme` checks the active theme without changing it. Use `-Theme` for a preset or `-Path` for a JSON or PSD1 team theme:

```powershell
Test-ConsoleTheme -Theme ColorBlindSafeLight -Background White
Test-ConsoleTheme -Theme Dark -Detailed
Test-ConsoleTheme -Path ./team-theme.json -Strict
(Test-ConsoleTheme -Theme Dark).Results | Where-Object Outcome -EQ Fail
```

The default view names the theme and gives a calm verdict, lists only problems and unknowns, and ends with the number of other checks that pass. A pair that fails under several vision conditions appears once, with its lowest distance. `-Detailed` shows every measurement; the `Results` property always retains all 28 checks, also for `Format-List` or `Show-ConsoleTable`.

The checks cover text contrast (at least 4.5:1 against each role's fill or the page), pass/warning/fail separation (CIELAB distance 25 in normal vision and simulated protanopia, deuteranopia and tritanopia), markers without colour, and Critical's distinct fill. The page defaults to `#0c0c0c` for dark themes and `#ffffff` for light themes. `-Background` overrides it; `Surface` is chrome, not the page.

Dark and Light correctly fail colour-vision checks; both colour-blind presets pass. Palette indexes, terminal-default foregrounds and dim text have unknown RGB (terminals choose their own dimming) and report **Unknown**, while marker checks still run. `Passed` requires every check to pass. `-Strict` makes any **Fail** terminating, with the report in the error's `TargetObject`; Unknown alone does not throw. The settings picker shows a short verdict below its preview.

In C#, call `ThemeCheck.Run(theme, new ThemeCheckOptions { Background = Color.FromHex("#ffffff") })` and inspect `Passed` and `Results`. The checker measures the theme's true-colour styles; it does not infer the live terminal palette or certify a terminal's rendering.

## Next

- [Colour schemes](/docs/colour-schemes/): Dracula, Nord, Solarized and others, faithful and accessible.
- [Capability detection](/docs/capabilities/): how the colour depth and background are detected.
- [Environment overrides](/docs/environment/): pin the theme and colour depth for CI and screenshots.
