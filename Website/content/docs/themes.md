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

- `CONSOLEMONSTER_THEME=light|dark|auto` (default `auto`), or `TerminalProfile.WithThemePreference` in code.
- `auto` reads the console background colour in conhost. In Windows Terminal, which does not report its scheme to console programs, `auto` picks the dark theme; set `CONSOLEMONSTER_THEME=light` for a light scheme.
- `Set-ConsoleTheme` in PowerShell, `LiveSessionOptions.Theme` for one session.

In PowerShell, for this session:

```powershell
Import-Module ConsoleMonster

Set-ConsoleTheme Light
Write-ConsoleMarkup '[pass]√ 412 passed[/]  [warn]▲ 51 warnings[/]  [fail]✗ 23 failed[/]'
```

For every new session, set the variable before ConsoleMonster first draws in that session, since detection reads it once then. Your profile (`$PROFILE`) is the easy place:

```powershell
$env:CONSOLEMONSTER_THEME = 'light'
Import-Module ConsoleMonster
```

In C#, for every write that follows:

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

Terminal.Theme = Theme.Light;                                   // every write from here on
Terminal.Write(new TextBlock("[pass]√ 412 passed[/]  [warn]▲ 51 warnings[/]  [fail]✗ 23 failed[/]"));
Terminal.Theme = Theme.HighContrast;
Terminal.Write(new Rule("[bold]High contrast[/]"));
```
The light theme meets WCAG contrast on white, and the dark theme keeps titles readable on a light background that could not be detected, because titles, headers and highlights use the terminal's own foreground in bold.

## Markup and roles

Theme roles are markup tags (`[pass]`, `[fail]`, `[warn]`, `[muted]`, `[accent]` and the others), also as backgrounds (`[white on fail]`). In PowerShell, `-Role` on `New-ConsoleText` and `Write-ConsoleText` does the same. Without colour, a status role adds its symbol, so status is never shown by colour alone.

## Reduction per tier

Colours given as hex, `rgb()` or palette names are reduced for each tier the way the SGR encoder sends them: to the nearest of the 256 xterm colours on a 256-colour console, and to the nearest of the 16 console colours on a 16-colour one. Theme roles use the 256 and 16-colour values the theme defines instead of the nearest match. The [widget gallery](/widgets/) shows every widget at each tier.

## Share a team theme file

Export a starting palette, edit its semantic tokens and load it for the session:

```powershell
Import-Module ConsoleMonster

Set-ConsoleTheme -Name Dark
Export-ConsoleTheme -Path ./team-theme.json -PassThru
# Edit the file, then apply it.
Set-ConsoleTheme -Path ./team-theme.json
Write-ConsoleText 'Deployment ready' -Role Pass
```

Choose `Dark`, `Light`, `HighContrast` or `ColorBlindSafe` with `-Name`.
`ColorBlindSafe` uses blue/orange pass/fail colours and underlines failures on a
dark background; status widgets retain their symbols and words. Export any preset
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

`Name` and `Tokens` are required; `SchemaVersion` defaults to `1`. Missing tokens
and style fields inherit from `BaseTheme`. Styles accept `Foreground`,
`Background` and `Decoration`. Explicit `null` colours use terminal defaults;
`"Decoration": "None"` clears inherited decoration. Fallback indices use ANSI
palette order: `Ansi16` is `0`–`15` (`-1` uses the terminal default), while
`Ansi256` is `0`–`255` (`-1` requests automatic reduction). Complete exports
resolve inheritance and include every token. Unknown keys, tokens and invalid
values are rejected before application.

### Try the same report before and after

1. Save [theme-files.ps1](/examples/themes/theme-files.ps1),
   [team-dark.json](/examples/themes/team-dark.json) and
   [team-light.json](/examples/themes/team-light.json) together in your own folder.
2. Use the background that matches your terminal and run the sample:

```powershell
./theme-files.ps1 -BaseTheme Dark -Width 120
# On a light terminal, compare the compact layout:
./theme-files.ps1 -BaseTheme Light -Width 60
```

The script builds a status list, a table and a log with native PowerShell
cmdlets, then draws those same widgets with the built-in and team palettes. It
leaves the team theme active. Add `-ExportDirectory ./presets` to write starting
JSON files for all four presets. Restore a built-in with
`Set-ConsoleTheme -Name Dark` or `Set-ConsoleTheme -Name Light`.

{{< cm-frame ids="theme-files-before-120,theme-files-after-120" caption="The same report at 120 columns, before and after loading the team files. Switch the site theme to see the matching dark or light palette. The complete PowerShell script and C# program are below the frames." >}}

{{< cm-frame ids="theme-files-before-60,theme-files-after-60" script="false" caption="The same programs at 60 columns. Status symbols, words and log levels remain visible in both palettes." >}}

To run the C# report, save [report.cs](/examples/themes/report.cs) as `Program.cs`
in a new console project, with the same two JSON files beside it:

```powershell
dotnet new console -n ThemeReport
cd ThemeReport
dotnet add package ConsoleMonster
# Save report.cs as Program.cs and the two team JSON files in this folder.
dotnet run
dotnet run -- ./team-dark.json
# For a light terminal:
dotnet run -- ./team-light.json
```

Without an argument the program uses the current terminal theme. A file argument
scopes the loaded palette to the report's existing output target with `Terminal.Use`,
then restores that target when the report finishes.

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

Try [PowerShell rich text](/powershell/) for role-based text and the
[widget gallery](/widgets/) to see the roles at each colour tier.
