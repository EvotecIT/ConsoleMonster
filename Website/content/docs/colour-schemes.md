---
title: Colour schemes
description: Dracula, Nord, Solarized, Gruvbox, Catppuccin and Tokyo Night, drawn faithfully and accessibly, with what the theme checker says about each.
slug: colour-schemes
order: 4
group: start
card: scheme-dracula
apis: [Set-ConsoleTheme, Theme.FromScheme, Test-ConsoleTheme]
chips: [Themes, Schemes, Accessibility]
summary: "Ten terminal colour schemes, faithful and accessible, side by side."
toc: false
---

A colour scheme is what every terminal already stores: sixteen ANSI colours, a foreground and a background.
ConsoleMonster derives a whole theme from one, so widgets look like the terminal they run in, and keep looking like it
in exports and recordings.

Each scheme comes in two forms:

- **Faithful** uses the published colours unchanged. It looks exactly like the scheme, and the checker says honestly
  where it falls short: statuses that look alike with colour blindness, or text below 4.5:1 contrast.
- **Accessible** is built from the same colours. It first picks the pass, warning and fail colours from the scheme's
  own sixteen that stay furthest apart, then adjusts lightness (never hue) where that is not enough. Every accessible
  variant below passes every check.

Choose one for every session:

```powershell
Set-ConsoleTheme Dracula -Save                  # faithful
Set-ConsoleTheme Dracula -Accessible -Save      # saved as 'Dracula · accessible'
Set-ConsoleTheme Solarized -Save                # Solarized Dark or Light, whichever suits the terminal
Set-ConsoleTheme 'Catppuccin Latte' -Save       # one variant, whatever the background
Test-ConsoleTheme -Detailed                     # every measurement of the active theme
```

`Show-ConsoleSettings` lists the schemes in their own group, each family with its variants and an accessible entry
beside each, and redraws the preview on that scheme's background as you move through them. A family name
([Solarized](#solarized), [Gruvbox](#gruvbox), [Catppuccin](#catppuccin), [Tokyo Night](#tokyo-night)) picks the variant
for your terminal's background (dark when it cannot tell); a variant name such as `Gruvbox Light` or `Tokyo Night Dark`
always chooses that one. [Dracula](#dracula) and [Nord](#nord) are dark only. Status words (`Set-ConsoleSetting StatusMarkers Words -Save`) work with any scheme.

Every figure below is the same program, the settings picker's preview, drawn on the scheme's own background. Under each
window is the checker's verdict; open it for the full report.

## Dracula

{{< cm-frame ids="scheme-dracula,scheme-dracula-accessible" script="false" >}}

Dracula's light counterpart is part of Dracula Pro, not the free scheme, so it has no light variant here.

## Nord

{{< cm-frame ids="scheme-nord,scheme-nord-accessible" script="false" >}}

## Solarized

{{< cm-frame ids="scheme-solarized-dark,scheme-solarized-dark-accessible" script="false" >}}

In Solarized Dark, bright black is the background colour itself, so muted text uses the scheme's white instead of
drawing invisibly.

{{< cm-frame ids="scheme-solarized-light,scheme-solarized-light-accessible" script="false" >}}

## Gruvbox

{{< cm-frame ids="scheme-gruvbox-dark,scheme-gruvbox-dark-accessible" script="false" >}}

{{< cm-frame ids="scheme-gruvbox-light,scheme-gruvbox-light-accessible" script="false" >}}

## Catppuccin

{{< cm-frame ids="scheme-catppuccin-mocha,scheme-catppuccin-mocha-accessible" script="false" >}}

{{< cm-frame ids="scheme-catppuccin-latte,scheme-catppuccin-latte-accessible" script="false" >}}

## Tokyo Night

{{< cm-frame ids="scheme-tokyo-night-dark,scheme-tokyo-night-dark-accessible" script="false" >}}

{{< cm-frame ids="scheme-tokyo-night-day,scheme-tokyo-night-day-accessible" script="false" >}}

## The program behind the figures

Each window runs this program with the scheme's theme. In C#, choose one by name,
`Terminal.Theme = Theme.FromName("Dracula · accessible");`, or walk `Theme.Schemes` and derive either form with
`Theme.FromScheme(entry.Scheme, accessible)`.

{{< cm-frame id="scheme-dracula" frame="false" >}}

## Sources and licences

Every palette is read from the scheme's own repository, never from an aggregate collection, and is used unchanged in
the faithful form.

| Scheme | Source | Licence |
|---|---|---|
| Dracula | [dracula/windows-terminal](https://github.com/dracula/windows-terminal) | MIT |
| Nord | [nordtheme/nord](https://github.com/nordtheme/nord) | MIT |
| Solarized Dark, Solarized Light | [altercation/solarized](https://github.com/altercation/solarized) | MIT |
| Gruvbox Dark, Gruvbox Light | [morhetz/gruvbox](https://github.com/morhetz/gruvbox) | MIT |
| Catppuccin Mocha, Catppuccin Latte | [catppuccin/windows-terminal](https://github.com/catppuccin/windows-terminal) | MIT |
| Tokyo Night Dark, Tokyo Night Day | [folke/tokyonight.nvim](https://github.com/folke/tokyonight.nvim) | Apache-2.0 |

## Next

- [Themes](/docs/themes/): built-in themes, following your terminal and importing any other scheme with
  `Import-ConsoleTheme`.
- [Check a theme](/docs/themes/#check-a-theme): what `Test-ConsoleTheme` measures and how to read its report.
- [Accessibility](/docs/accessibility/): status words, high contrast and the colour-blind safe themes.
