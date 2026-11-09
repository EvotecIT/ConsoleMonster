---
title: Themes
description: The dark, light and high-contrast themes, the roles they define and how their colours are reduced for 256 and 16-colour consoles.
slug: themes
order: 4
group: start
card: theme-dark
chips: [Dark, Light, High contrast]
summary: "Dark, light and high contrast, and how colours are reduced per tier."
---

Widgets never name colours directly. They use theme roles: `Text`, `Muted`, `Accent`, `Pass`, `Warn`, `Fail`, `Info`, `Skip`, `Running`, `Pending`, `Border`, `Title`, `Header`, `Surface`, `Focus` and six chart series. A theme maps each role to a 24-bit colour with its 256 and 16-colour fallbacks, so a role stays distinguishable on every tier.

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
show as symbols (✓ ✗ ▴) or as words (`[OK]` `[FAIL]` `[WARN]`). Words help screen readers and anyone who finds small
symbols hard to tell apart, and they look the same in every terminal font. Keep your own style whatever the theme:

```powershell
Set-ConsoleSetting Border Single -Save
Set-ConsoleSetting StatusMarkers Words -Save
```

`Show-ConsoleSettings` previews both. A widget that asks for its own frame (`New-ConsolePanel -Border Double`) keeps
it, and the ASCII glyph tier still draws ASCII. `CONSOLEMONSTER_BORDER` and `CONSOLEMONSTER_STATUS_MARKERS` set them
for one run; in C#, `Theme.WithAppearance` gives a theme its style and `TerminalProfile.WithBorder` and
`WithStatusMarkers` override it.

Exports follow your choice too: once you pick a theme, `Export-ConsoleFrame` draws in it on the background it is made
for. `-Theme Auto` still gives both the dark and the light rendering, and `-Theme Current` asks for the theme in use.

## Markup and roles

Theme roles are markup tags (`[pass]`, `[fail]`, `[warn]`, `[muted]`, `[accent]` and the others), also as backgrounds (`[white on fail]`). In PowerShell, `-Role` on `New-ConsoleText` and `Write-ConsoleText` does the same. Without colour, a status role adds its symbol, so status is never shown by colour alone.

## Reduction per tier

Colours given as hex, `rgb()` or palette names are reduced for each tier the way the SGR encoder sends them: to the nearest of the 256 xterm colours on a 256-colour console, and to the nearest of the 16 console colours on a 16-colour one. Theme roles use the 256 and 16-colour values the theme defines instead of the nearest match. The [widget gallery](/widgets/) shows every widget at each tier.

## Share a team theme file

Export a starting palette, edit its semantic tokens and load it for the session:

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
`team-theme.json` to change the accent while preserving the other Dark tokens:

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
[style](#frames-and-status-words): `"Style": { "Border": "Single", "StatusMarkers": "Words", "Background": "Dark" }`. Missing tokens
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

### Load JSON in .NET

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

## Related

[PowerShell rich text](/powershell/) shows role-based text, and the [widget gallery](/widgets/) shows the roles at each
colour tier.

## Check a theme

`Test-ConsoleTheme` checks the active theme without changing it. Use `-Theme` for a preset or `-Path` for a JSON or PSD1 team theme:

```powershell
Test-ConsoleTheme -Theme ColorBlindSafeLight -Background White
Test-ConsoleTheme -Path ./team-theme.json -Strict
(Test-ConsoleTheme -Theme Dark).Results | Where-Object Outcome -EQ Fail
```

The report shows text contrast (at least 4.5:1 against each token's fill or the page), pass/warning/fail separation (CIELAB distance 25 in normal vision and simulated protanopia, deuteranopia and tritanopia), markers without colour, and Critical's distinct fill. The page defaults to `#0c0c0c` for dark themes and `#ffffff` for light themes. `-Background` overrides it; `Surface` is chrome, not the page.

Dark and Light correctly fail colour-vision checks; both colour-blind presets pass. Palette indexes and terminal-default foregrounds have unknown RGB and report **Unknown**, while marker checks still run. `Passed` requires every check to pass. `-Strict` makes any **Fail** terminating, with the report in the error's `TargetObject`; Unknown alone does not throw. The settings picker shows a short verdict below its preview.

In C#, call `ThemeCheck.Run(theme, new ThemeCheckOptions { Background = Color.FromHex("#ffffff") })` and inspect `Passed` and `Results`. The checker measures the theme's true-colour styles; it does not infer the live terminal palette or certify a terminal's rendering.
