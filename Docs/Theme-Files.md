# Share a theme file

A team theme gives the same semantic roles to status markers, tables, logs and
markup. Start with a built-in preset, export it, edit the tokens you need, then
load the file in a session or your PowerShell profile. Widgets keep their layout;
their colours and decorations follow the selected theme.

## PowerShell

```powershell
Import-Module ConsoleMonster

Set-ConsoleTheme -Theme Dark
Export-ConsoleTheme -Path ./team-theme.json -PassThru
# Edit team-theme.json, then apply it.
Set-ConsoleTheme -Path ./team-theme.json
Write-ConsoleText 'Deployment ready' -Role Pass
```

`-PassThru` returns the written `FileInfo`. Without it, export writes no pipeline
output. The destination folder must exist. An existing file is replaced;
`Export-ConsoleTheme -Path ./team-theme.json -WhatIf` previews the write without
creating or replacing the file. Export leaves the active theme unchanged.

The extension chooses JSON or a PowerShell data file:

```powershell
Export-ConsoleTheme -Path ./team-theme.psd1
Set-ConsoleTheme -Path ./team-theme.psd1
```

PSD1 is imported as restricted data, not executed as a script. JSON exports are
UTF-8 without a BOM; PSD1 exports include a UTF-8 BOM for Windows PowerShell 5.1.
Paths are literal file paths, relative to the current PowerShell location when
not absolute. Invalid input raises an error and leaves the current theme active.

For every new session, put the import and load in `$PROFILE`, choosing a file that
matches the terminal background:

```powershell
Import-Module ConsoleMonster
Set-ConsoleTheme -Path (Join-Path $HOME 'team-dark.json')
```

This selects ConsoleMonster's widget palette; it does not change the terminal
application's own colour scheme or background.

## Four starting presets

`Dark` suits dark backgrounds; `Light` suits white backgrounds. `HighContrast`
uses saturated primaries on a dark background. `ColorBlindSafe` uses blue and
orange for pass/fail, with an underline on failures, on a dark background.
Status widgets also retain symbols and words, so colour is not the only signal.
Check a customised palette in the terminal and colour tiers your team uses.

```powershell
Import-Module ConsoleMonster

$null = New-Item -ItemType Directory -Path ./presets -Force
foreach ($preset in 'Dark', 'Light', 'HighContrast', 'ColorBlindSafe') {
    Set-ConsoleTheme -Theme $preset
    Export-ConsoleTheme -Path (Join-Path ./presets ($preset + '.json'))
}
Set-ConsoleTheme -Theme Dark
```

Use `.psd1` instead of `.json` to export the same presets as PowerShell data.
Exported files contain the complete resolved token table and no `BaseTheme`
dependency. Changing or removing the original partial file does not change an
exported snapshot.

## A small team file

Save this as `team-dark.json`. Unspecified tokens and style fields inherit from
`Dark`; this file changes only the accent and the pass/fail treatment.

```json
{
  "SchemaVersion": 1,
  "Name": "Team Dark",
  "BaseTheme": "Dark",
  "Tokens": {
    "Accent": {
      "Foreground": { "TrueColor": "#7dd3fc", "Ansi16": 14, "Ansi256": 117 },
      "Decoration": "Bold"
    },
    "Pass": {
      "Foreground": { "TrueColor": "#56b4e9", "Ansi16": 14, "Ansi256": 74 },
      "Decoration": "Bold"
    },
    "Fail": {
      "Foreground": { "TrueColor": "#f29d49", "Ansi16": 13, "Ansi256": 215 },
      "Decoration": "Bold, Underline"
    }
  }
}
```

The same partial theme in `team-dark.psd1`:

```powershell
@{
    SchemaVersion = 1
    Name = 'Team Dark'
    BaseTheme = 'Dark'
    Tokens = @{
        Accent = @{
            Foreground = @{ TrueColor = '#7dd3fc'; Ansi16 = 14; Ansi256 = 117 }
            Decoration = 'Bold'
        }
        Pass = @{
            Foreground = @{ TrueColor = '#56b4e9'; Ansi16 = 14; Ansi256 = 74 }
            Decoration = 'Bold'
        }
        Fail = @{
            Foreground = @{ TrueColor = '#f29d49'; Ansi16 = 13; Ansi256 = 215 }
            Decoration = 'Bold, Underline'
        }
    }
}
```

`Name` and `Tokens` are required. `SchemaVersion` is optional and defaults to `1`;
version `1` is the supported version. `BaseTheme` can be `Dark`, `Light`,
`HighContrast` or `ColorBlindSafe`. Without it, unspecified styles use terminal
defaults. A base names a built-in preset, not another file.

Token names and keys are case-insensitive. Styles accept `Foreground`,
`Background` and `Decoration`. An omitted field inherits; an explicit `null`
(`$null` in PSD1) foreground or background restores the terminal default for that
field. `Decoration = 'None'` clears inherited decorations.

Colours accept the object form above, or a colour string such as `'#7dd3fc'`.
Use the object form to choose explicit fallbacks: `Ansi16` is `0`–`15` in ANSI
palette order, and `Ansi256` is `0`–`255`. These indices are not PowerShell's
`ConsoleColor` numeric order. `Ansi16 = -1` uses the terminal default;
`Ansi256 = -1` requests automatic reduction. If `Ansi16` is omitted it is
derived from `TrueColor`. A colour object requires `TrueColor`.
Decorations use names such as `Bold`, `Underline`, `Italic`, `Dim`,
`Strikethrough` and `Reverse`, combined with commas.

Unknown keys, tokens, base themes, unsupported versions, invalid colours,
fallback indices and decorations are rejected. JSON duplicate keys and trailing
content are rejected too. No part of an invalid file is applied.

## Compare the same report

The [PowerShell example](../Website/examples/themes/theme-files.ps1) builds status,
table and log widgets with native cmdlets, then draws the same widgets before and
after loading a team file. Save it beside [team-dark.json](../Website/examples/themes/team-dark.json)
and [team-light.json](../Website/examples/themes/team-light.json), then run:

```powershell
./theme-files.ps1 -BaseTheme Dark -Width 120
./theme-files.ps1 -BaseTheme Light -Width 60
```

The sample data includes pass, warning, failure and pending states. Use the first
command on a dark terminal and the second on a light terminal. Add
`-ExportDirectory ./presets` to write all four starting preset files. The script
leaves the team theme active; `Set-ConsoleTheme -Theme Dark` or `Light` restores a
built-in palette.

## .NET

Install the `ConsoleMonster` NuGet package in a console project. The core loads
JSON; the PowerShell adapter owns PSD1 parsing.

```csharp
using ConsoleMonster;
using ConsoleMonster.Styling;
using ConsoleMonster.Widgets;

ThemeFile document = ThemeFile.Load("team-dark.json");
Theme team = document.ToTheme();
Terminal.Theme = team;
Terminal.Write(new TextBlock("[pass]Deployment ready[/]"));

// Capture the resolved palette, including every fallback and decoration.
ThemeFile.FromTheme(team).Save("team-snapshot.json");
```

`ThemeFile.Parse(json)` reads a JSON string. `ThemeFile.Import(dictionary)`
validates an adapter's data dictionary. `FromTheme(theme)` snapshots an `ITheme`;
`ToJson()` returns the complete JSON document and `Save(path)` writes it. Loading,
parsing, importing and saving do not change `Terminal.Theme`; assign the result
of `ToTheme()` when you want to apply it.

The [complete .NET example](../Website/examples/themes/report.cs) uses the same
illustrative report. Run it without arguments for the current theme, or pass a
team JSON path. It scopes the selected palette to the report's existing output
target and restores that target when it finishes.

For more on role-based styling and the supported tiers, see
[Themes on the site](https://consolemonster.dev/docs/themes/) and
[the output export guide](Output-Exports.md).
