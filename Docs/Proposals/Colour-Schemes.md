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

A scheme is **faithful by default**: `Set-ConsoleTheme Dracula` looks exactly like Dracula, and the checker says
honestly where it falls short. Every scheme also has an **accessible variant**, built from the same colours, that is
guaranteed to pass: "Dracula · accessible". Status words (`StatusMarkers = Words`) already work with any theme. A
Dracula fan with deuteranopia should not have to choose between the two.

Decision (Przemysław, 2026-11-09): faithful by default, with the accessible variant beside it. Making the accessible
variant the default was the alternative; it would surprise people who chose a scheme for how it looks.

## 1. Deriving a theme

One mapping, the same for every source, so a scheme looks like itself everywhere:

| Tokens | From |
|---|---|
| `Text`, `Title` (bold), `Header` (bold), `Strong` (bold) | foreground |
| `Muted`, `Border`, `Skip`, `Pending`, `Low` | bright black, or white when bright black has under 3:1 (see below) |
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

Each role prefers the first candidate that has at least 3:1 against the background: the normal colour, then the
bright one (or the reverse on light schemes). Muted roles try bright black, then white, then the unmodified
foreground. If none qualifies, they keep the foreground and report its low contrast. Upstream palettes need this:
Solarized's bright black *is* its background, so a literal mapping draws muted text invisibly. The faithful theme
never changes a colour, it only chooses among them. A catalogue entry may name a **signature accent** (Dracula's
purple, Nord's frost blue) to replace `Accent`, because that colour is what makes the scheme recognisable; nothing
else is hand-tuned.

**The accessible variant.** Two steps, and the second only when the first is not enough:

1. **Choose, don't change.** From the scheme's own 16 colours, pick the pass, warning and fail trio (pass from the
   blues and cyans, warning from the yellows, fail from the reds and magentas) that stays furthest apart under
   protanopia, deuteranopia and tritanopia while keeping 3:1 contrast. Fail is underlined and `Critical` keeps its
   fill, so status never depends on hue alone.
2. **Adjust lightness, never hue.** Where the best trio is still under the colour-vision bar (CIELAB distance 25), or a
   text role is under 4.5:1, move only that colour's lightness (in OKLCH, keeping hue and chroma as far as the gamut
   allows) until it passes. The variant records which colours it adjusted, and `Test-ConsoleTheme` lists them.

Measured on the catalogue below (minimum CIELAB distance between pass, warning and fail under the three simulations,
with 3:1 contrast): step 1 alone reaches the bar for Dracula (32.9), Nord (32.7), Tokyo Night Day (29.1), Catppuccin
Mocha (28.9), Gruvbox Dark (28.5), Tokyo Night (28.3) and Solarized Dark (27.0). Solarized Light (23.1) and Gruvbox
Light (23.8) need step 2, and Catppuccin Latte has no trio at 3:1, so its variant is adjusted. The literal mapping
(green, yellow, red) reaches 2 to 13 on every one of them, which is why accessibility is a variant and not a hope.
Contrast is the larger gap: most of these schemes, and all four light ones, have status colours under 4.5:1 as
published.

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
  "Variant": "Accessible",
  "Style": { "StatusMarkers": "Words" },
  "Tokens": { "Accent": { "Foreground": { "TrueColor": "#ff79c6" } } }
}
```

`BaseScheme` names a catalogue scheme or embeds one (`"Scheme": { "Foreground": ..., "Background": ...,
"Palette": [16 colours] }`); `BaseTheme` keeps working for the built-in presets. `Import-ConsoleTheme` writes this
form, so an imported scheme stays a scheme: its derivation improves when ConsoleMonster's does, and it can take the
accessible variant like any other.

## 4. Checking

`Test-ConsoleTheme` (section 4 of the settings proposal) is the gate for everything here. It runs on derived themes
the same way as on hand-made ones: contrast against the background, pass, warning and fail apart in normal vision and
under the three simulations, statuses distinguishable without colour. Its results decide:

- that every catalogue scheme's accessible variant passes everything, and what its faithful form falls short on;
- what `Import-ConsoleTheme` reports about a user's scheme (it imports anyway and lists what fails);
- what `Show-ConsoleSettings` shows next to a scheme while it is highlighted.

## 5. The catalogue

A small, curated set, shipped as data (one JSON file per scheme family, embedded in the library), each with its
source URL, version and licence:

| Scheme | Variant | Source | Licence |
|---|---|---|---|
| Dracula | Dark | [dracula/windows-terminal](https://github.com/dracula/windows-terminal) | MIT |
| Nord | Dark | [nordtheme/nord](https://github.com/nordtheme/nord) (nord0–15 and its terminal mapping) | MIT |
| Solarized Dark, Solarized Light | Dark, Light | [altercation/solarized](https://github.com/altercation/solarized) (the documented ANSI mapping) | MIT |
| Gruvbox Dark, Gruvbox Light | Dark, Light | [morhetz/gruvbox](https://github.com/morhetz/gruvbox) | MIT |
| Catppuccin Mocha, Catppuccin Latte | Dark, Light | [catppuccin/windows-terminal](https://github.com/catppuccin/windows-terminal) | MIT |
| Tokyo Night, Tokyo Night Day | Dark, Light | [folke/tokyonight.nvim](https://github.com/folke/tokyonight.nvim) `extras/windows_terminal` | Apache-2.0 |

The palettes, read from those sources on 2026-11-09, are in
[Colour-Schemes.catalogue.json](Colour-Schemes.catalogue.json) until the catalogue PR moves them into the library.
One Dark is left out for now: its terminal palettes are community ports with no single upstream to cite. Dracula's
light counterpart is part of Dracula Pro, not the free scheme, so there is no free Dracula light variant to pair.

Values come from each scheme's own repository, never from aggregate collections: the widely copied
iTerm2-Color-Schemes set has different Catppuccin bright colours and selection (and Latte's black and white swapped),
and lightens Solarized's and Nord's bright black. A small script reads the sources again, so updates are a diff, not
retyping.

Rules for entry, so the list stays useful rather than long:

- the scheme is widely used and published under a licence that allows redistribution (MIT or Apache-2.0 for the entries
  above), with the notice kept beside the data;
- its accessible variant passes every check; the faithful form ships whatever the checker says about it, and the
  picker and the gallery show that result;
- light and dark variants of one family are paired, so `Set-ConsoleTheme Solarized` picks the variant for the
  detected background, as `ColorBlindSafe` does today;
- the faithful form uses the upstream values unchanged; the only per-scheme choice is the signature accent, and only
  the accessible variant may adjust lightness.

A test walks the catalogue: every entry derives and round-trips through a theme file. Its accessible variant must
pass the checker; its faithful form keeps its reported failures. Adding a scheme is adding data and a review, not code.

## 6. Where people meet it

- **Theme setting and `Set-ConsoleTheme`:** values become the built-in presets, `Terminal`, and the catalogue names,
  with `-Accessible` as a switch (`Set-ConsoleTheme Dracula -Accessible -Save`); the Theme setting stores
  `Dracula · accessible`.
- **`Show-ConsoleSettings`:** the Theme list groups Built in, Follow my terminal, Schemes and Your files; the preview
  already redraws per highlight, lists each scheme with its accessible variant beside it, and gains the checker line
("statuses apart for colour-blind users: yes / no", "status text below 4.5:1").
- **The site:** a scheme gallery page, every catalogue scheme rendering the same summary faithfully and accessibly,
  side by side with its checker result, built by SiteFrames like the theme frames today.
- **C#:** `Theme.FromScheme(TerminalScheme, accessible: bool)`, `TerminalScheme.Parse...` for the import formats,
  `Theme.Schemes` for the catalogue and `Terminal.Palette` for the probed colours.

## Delivery

Three PRs, each useful on its own:

1. **The checker.** `Test-ConsoleTheme` with the contrast, colour-vision and marker checks, a C# report, and the
   checker line in the picker. It works on any `ITheme`, so it needs nothing from the other two and comes first: it is
   the gate the catalogue is held to.
2. **Schemes and the terminal.** `TerminalScheme`, the derivation with the accessible variant, palette-index
   colours, the `Terminal` preset, the OSC 4 probe, version 3 theme files with `BaseScheme` and `Variant`, and
   `Import-ConsoleTheme` for Windows Terminal, iTerm2 and base16. It changes theme files, so it follows compact
   density, which also adds to them.
3. **The catalogue.** The schemes above as data with their notices, pairing, the catalogue test, the picker groups
   and the site gallery. It needs both.

## Open questions

- Should `Auto` prefer `Terminal` over the built-in `Dark`/`Light` when the OSC 4 probe succeeds? It would make
  ConsoleMonster match every terminal out of the box, but changes today's default look; my preference is to offer it
  in the picker first and decide after people use it.
- Which import formats beyond Windows Terminal, iTerm2 and base16 are worth their parsers.
