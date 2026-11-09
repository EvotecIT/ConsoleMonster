# Proposal: colour schemes as the source of themes

Status: PR 1 implements the theme checker (section 4), `Test-ConsoleTheme` and the picker summary. Scheme derivation,
terminal palettes, import and the catalogue remain proposals. This design replaces "A theme that follows the terminal", "Importing colour schemes" and the scheme parts of
"Checking a theme" in [Settings-And-Themes.md](Settings-And-Themes.md) with one design, and adds a curated catalogue.

## Why

People ask for their scheme by name: Dracula, Nord, Solarized, Catppuccin, Gruvbox. Adding presets one at a time
would give us a pile of hand-tuned token tables that drift, ignore light variants, and quietly break the colour-blind
guarantee the built-in presets keep. Most of those people already run that scheme in their terminal, so what they
really want is for ConsoleMonster to look like their terminal, and to keep looking like it in exports and recordings.

## The idea

A **scheme** is what every terminal already stores: 16 ANSI colours plus a foreground and a background (and,
optionally, selection and cursor colours). Windows Terminal, iTerm2, base16, Alacritty, kitty and VS Code all reduce
to it. ConsoleMonster gets one function that **derives a theme from a scheme**, and everything else uses it:

| Source | What the scheme holds | Where it comes from |
|---|---|---|
| `Terminal` preset | palette indexes 0–15 (no RGB) | the terminal itself |
| An imported scheme | RGB values | `Import-ConsoleTheme` from a terminal's scheme file |
| The catalogue | RGB values, with provenance | data files shipped with ConsoleMonster |

Accessibility is a **modifier on any scheme**, not a separate theme: "Dracula, colour-blind safe" is Dracula's own
colours rearranged so pass, warning and fail stay apart, and status words (`StatusMarkers = Words`) already work with
any theme. A Dracula fan with deuteranopia should not have to choose between the two.

## 1. Deriving a theme

One mapping, the same for every source, so a scheme looks like itself everywhere:

| Tokens | From |
|---|---|
| `Text`, `Title` (bold), `Header` (bold), `Strong` (bold) | foreground |
| `Muted`, `Border`, `Skip`, `Pending`, `Low` | bright black |
| `Subtle` | bright black, dim |
| `Pass` | green |
| `Warn` | yellow |
| `Fail` | red |
| `Info`, `Running` | cyan |
| `Accent` | blue |
| `Accent2` | magenta |
| `Highlight`, `Focus` | the selection colour when the scheme has one, otherwise blue as a background |
| `Surface` | the background moved 8% toward the foreground |
| `SurfaceAccent` | blue on `Surface` |
| `Series1`–`Series6` | blue, yellow, magenta, cyan, green, red: neighbours differ in lightness as well as hue |
| `Critical` | the background colour on a red fill |
| `Gap` | the background |

Bright variants are used where the normal one is too dark against the background (contrast below 3:1), which is
common for blue on dark schemes. A catalogue entry may name a **signature accent** (Dracula's purple, Nord's frost
blue) to replace `Accent`, because that colour is what makes the scheme recognisable; nothing else is hand-tuned.

**Colour-blind modifier.** `Pass` takes the scheme's blue, `Warn` its yellow, `Fail` its red, underlined, and
`Critical` the red fill. The checker (section 4) confirms those three stay apart under protanopia, deuteranopia and
tritanopia; where a scheme's own colours cannot do it, the entry says so instead of shipping a variant that only
looks safe.

## 2. Following the terminal

The `Terminal` preset emits palette indexes at every colour depth, so the terminal's scheme decides the colours and
ConsoleMonster always matches it, including a scheme someone set up for low vision. This needs a `ThemeColor` that
stays a palette index at 256-colour and true-colour depths.

To check it and to export it, ConsoleMonster needs the actual RGB values. Most modern terminals answer the OSC 4
palette query (`ESC ] 4 ; n ; ? BEL`), the same way they answer the OSC 11 background query
`TerminalBackground` already sends. Reading the 16 colours once at start-up (with the same timeout and the same
"interactive, not redirected" guard) turns the live terminal into a scheme:

- exports and recordings of a `Terminal`-themed session use the real colours instead of guessing;
- `Test-ConsoleTheme -Theme Terminal` checks what the user actually sees;
- `Export-ConsoleTheme -Theme Terminal` saves it as a file to share.

Where the terminal does not answer, the preset still works on screen; exports fall back to the scheme's background
variant defaults and say so in the decisions.

## 3. Schemes in files

A theme file can start from a scheme instead of a preset, and override only what it needs:

```json
{
  "SchemaVersion": 3,
  "Name": "Dracula (team)",
  "BaseScheme": "Dracula",
  "Modifier": "ColorBlindSafe",
  "Style": { "StatusMarkers": "Words" },
  "Tokens": { "Accent": { "Foreground": { "TrueColor": "#ff79c6" } } }
}
```

`BaseScheme` names a catalogue scheme or embeds one (`"Scheme": { "Foreground": ..., "Background": ...,
"Palette": [16 colours] }`); `BaseTheme` keeps working for the built-in presets. `Import-ConsoleTheme` writes this
form, so an imported scheme stays a scheme: its derivation improves when ConsoleMonster's does, and it can take the
colour-blind modifier like any other.

## 4. Checking

`Test-ConsoleTheme` (section 4 of the settings proposal) is the gate for everything here. It runs on derived themes
the same way as on hand-made ones: contrast against the background, pass, warning and fail apart in normal vision and
under the three simulations, statuses distinguishable without colour. Its results decide:

- whether a catalogue scheme ships as a preset, and with which modifier variants;
- what `Import-ConsoleTheme` reports about a user's scheme (it imports anyway and lists what fails);
- what `Show-ConsoleSettings` shows next to a scheme while it is highlighted.

## 5. The catalogue

A small, curated set, shipped as data (one JSON file per scheme family, embedded in the library), each with its
source URL, version and licence:

- Dracula (and its official light variant, if its published colours pass the checker)
- Nord
- Solarized Dark and Solarized Light
- Gruvbox Dark and Gruvbox Light
- Catppuccin Mocha and Catppuccin Latte
- Tokyo Night
- One Dark

Rules for entry, so the list stays useful rather than long:

- the scheme is widely used and published under a licence that allows redistribution (all of the above are MIT, to be
  confirmed per scheme), with the notice kept beside the data;
- the derived theme passes the checker's contrast checks; schemes whose statuses are too close for colour-blind users
  ship without a `ColorBlindSafe` variant rather than with a broken one;
- light and dark variants of one family are paired, so `Set-ConsoleTheme Solarized` picks the variant for the
  detected background, as `ColorBlindSafe` does today;
- colours are the upstream values, never adjusted; the only per-scheme choice is the signature accent.

A test walks the catalogue: every entry derives, round-trips through a theme file and passes the checker, so adding a
scheme is adding data and a review, not code.

## 6. Where people meet it

- **Theme setting and `Set-ConsoleTheme`:** values become the built-in presets, `Terminal`, and the catalogue names,
  with `-ColorBlindSafe` as a switch (`Set-ConsoleTheme Dracula -ColorBlindSafe -Save`) and a matching `Modifier`
  setting.
- **`Show-ConsoleSettings`:** the Theme list groups Built in, Follow my terminal, Schemes and Your files; the preview
  already redraws per highlight and gains the checker line ("statuses apart for colour-blind users: yes / no").
- **The site:** a scheme gallery page, every catalogue scheme rendering the same summary in its dark and light
  variants, built by SiteFrames like the theme frames today.
- **C#:** `Theme.FromScheme(TerminalScheme, ThemeModifier)`, `TerminalScheme.Parse...` for the import formats,
  `Theme.Schemes` for the catalogue and `Terminal.Palette` for the probed colours.

## Delivery

Three PRs, each useful on its own:

1. **The checker.** `Test-ConsoleTheme` with the contrast, colour-vision and marker checks, a C# report, and the
   checker line in the picker. It works on any `ITheme`, so it needs nothing from the other two and comes first: it is
   the gate the catalogue is held to.
2. **Schemes and the terminal.** `TerminalScheme`, the derivation with the colour-blind modifier, palette-index
   colours, the `Terminal` preset, the OSC 4 probe, version 3 theme files with `BaseScheme` and `Modifier`, and
   `Import-ConsoleTheme` for Windows Terminal, iTerm2 and base16. It changes theme files, so it follows compact
   density, which also adds to them.
3. **The catalogue.** The schemes above as data with their notices, pairing, the catalogue test, the picker groups
   and the site gallery. It needs both.

## Open questions

- Should `Auto` prefer `Terminal` over the built-in `Dark`/`Light` when the OSC 4 probe succeeds? It would make
  ConsoleMonster match every terminal out of the box, but changes today's default look; my preference is to offer it
  in the picker first and decide after people use it.
- Which import formats beyond Windows Terminal, iTerm2 and base16 are worth their parsers.
