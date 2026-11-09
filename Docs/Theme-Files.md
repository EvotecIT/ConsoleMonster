# Share a theme file

A team theme gives the same semantic roles to status markers, tables, logs and
markup. Start with a built-in preset, export it, edit the tokens you need, then
load the file in a session or your PowerShell profile. Widgets keep their layout;
their colours and decorations follow the selected theme.

Set `CONSOLEMONSTER_THEME=highcontrast` or `colorblind` to select `Theme.HighContrast` or `Theme.ColorBlindSafe`
without a theme file. `light`, `dark` and `auto` remain available. With `auto` or no theme preference, Windows
High Contrast selects the high contrast palette before background detection. Other platforms keep background
detection. An explicit theme object, loaded file or named light/dark palette wins over system detection.

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

## Starting presets

`Dark` suits dark backgrounds; `Light` suits white backgrounds. `HighContrast`
uses saturated primaries on a dark background. `ColorBlindSafe` keeps pass,
warning and fail apart with red-green colour blindness: blue, yellow and
underlined rose on dark backgrounds, and blue, dark gold and underlined magenta
in `ColorBlindSafeLight`. `-Theme ColorBlindSafe` picks the variant from the
detected background. Status widgets also retain symbols and words, so colour is
not the only signal.
Check a customised palette in the terminal and colour tiers your team uses.

```powershell
Import-Module ConsoleMonster

$null = New-Item -ItemType Directory -Path ./presets -Force
foreach ($preset in 'Dark', 'Light', 'HighContrast', 'ColorBlindSafeDark', 'ColorBlindSafeLight') {
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

`Name` and `Tokens` are required. `SchemaVersion` is optional and defaults to `1`.
Version `2` adds an optional `Style` section beside the colours:

```json
"Style": { "Border": "Single", "Density": "Compact", "StatusMarkers": "Words", "Background": "Dark" }
```

`Border` is the frame for panels, tables and inputs that use the default frame (`Rounded`, `Single`, `Double`,
`Heavy` or `Ascii`); a widget that names its own frame keeps it, and the ASCII glyph tier still draws ASCII.
`Density` is `Comfortable` (the default) or `Compact`. Compact removes boxed table cell padding, reduces default
unboxed table and list gaps from two cells to one, removes default horizontal panel padding and closes default
gaps between framed layout tiles. A one-cell default gap between unframed children stays one; larger default
gaps shrink by one. Explicit padding, gaps and column gaps stay as authored. `Set-ConsoleSetting Density Compact`
or `CONSOLEMONSTER_DENSITY=compact` overrides the theme; C# uses `TerminalProfile.WithDensity`.
`StatusMarkers` is `Symbols` (✓ ✗ ▴) or `Words` (`[OK]` `[FAIL]` `[WARN]` at every glyph tier). `Background` is the
background the theme is made for, which `Export-ConsoleFrame` uses for the page. Omitted fields inherit from
`BaseTheme`; the `Light` presets are made for a light background. Exports write version `2` only when the style
differs from the default, so existing version `1` files stay as they are. `BaseTheme` can be `Dark`, `Light`,
`HighContrast`, `ColorBlindSafe`, `ColorBlindSafeDark` or `ColorBlindSafeLight`. Without it, unspecified styles use terminal
defaults. A base names a built-in preset, not another file, and does not detect the background: `ColorBlindSafe`
is always the dark variant, so a file for light terminals uses `ColorBlindSafeLight`.

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
`-ExportDirectory ./presets` to write all five starting preset files. The script
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

## Schemes and version 3

`Import-ConsoleTheme` converts a Windows Terminal scheme JSON, an XML iTerm2 `.itermcolors` plist in sRGB, or a flat
base16 YAML file into a theme. Export a single Windows Terminal scheme entry when settings contain several.
Import always reports the checks; a failing colour scheme is still imported.

```powershell
Import-ConsoleTheme -Path ./my-scheme.yaml -Accessible -OutFile ./my-theme.json
Set-ConsoleTheme -Path ./my-theme.json
Test-ConsoleTheme -Path ./my-theme.json
```

Without `-OutFile`, import writes `<source-name>.theme.json` beside its source. It does not change the active theme.
`-Accessible` chooses pass, warning and fail from the scheme's colours, then adjusts OKLCH lightness where needed,
keeping hue and at least half the source chroma. If that cannot meet the bars, it tries another palette colour in
the same status family: blue-to-cyan, orange-to-yellow or red-to-magenta. The report lists the adjusted tokens. The source
palette is retained unchanged. Unusual palettes can still fail; inspect the actual measurements.

A version 3 file embeds the scheme instead of depending on a catalogue entry:

```json
{
  "SchemaVersion": 3,
  "Name": "Team scheme",
  "BaseScheme": {
    "Scheme": {
      "Name": "My terminal",
      "Foreground": "#eeeeee",
      "Background": "#112233",
      "Palette": [
        "#000000", "#cc3333", "#44aa44", "#cccc44", "#3377cc", "#aa44aa", "#44aaaa", "#cccccc",
        "#777777", "#ff5555", "#66cc66", "#eeee66", "#6699ff", "#cc66cc", "#66cccc", "#ffffff"
      ],
      "Selection": "#334455",
      "Cursor": "#ffffff"
    }
  },
  "Variant": "Accessible",
  "Tokens": { "Accent": { "Foreground": "#6699ff" } }
}
```

`Palette` contains exactly sixteen RGB colours in ANSI order. `Selection`, `Cursor` and `SignatureAccent` are optional.
`Variant` is omitted for the faithful theme or set to `Accessible`. `BaseTheme` and `BaseScheme` are mutually exclusive.
Named catalogue references arrive separately. Tokens and style fields override the derived theme. Exports retain the
scheme and only token overrides, so derivation remains the source of the theme. Versions 1 and 2 still load; ordinary
preset/style files continue exporting in their existing version without requiring version 3.

`Set-ConsoleTheme -Path ./my-theme.json -Accessible -Save` selects the accessible derivation without editing the file.
The Theme setting stores the path followed by ` · accessible`, so new sessions retain that choice. Explicit file
colour overrides remain explicit, even if they equal the faithful colour or fail a check. A decoration-only override
keeps the accessible variant's derived foreground and background.

`Terminal` is a separate preset that emits palette indexes at every colour depth. For a custom version 3 token,
`"Foreground": { "PaletteIndex": 4 }` keeps the blue index even on a true-colour terminal; it cannot be combined with
RGB fallbacks. `"BaseTheme": "Terminal"` inherits those roles. `Terminal.Palette` exposes the guarded startup probe.
Checking Terminal uses the measured palette or reports Unknown. Exporting a Terminal-themed session captures the
measured palette and defaults, or the background variant defaults when probing is unavailable; the capability
decisions state which choice was made. `Auto` keeps its existing behaviour.
