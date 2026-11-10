---
title: Accessibility
description: Accessible output, reduced motion, high-contrast and colour-blind palettes, and meaning that never depends on colour alone.
slug: accessibility
order: 10
group: start
summary: "One switch for accessible output, reduced motion on its own, named palettes and status that never relies on colour."
chips: [Accessible, Reduced motion, No colour]
card: theme-dark
---

ConsoleMonster's output works for people who use a screen reader, need high contrast, can't tell red from green or find motion distracting. Each of these is a setting you turn on once, for the whole session; nothing in your script changes.

| You want | Set | What changes |
|---|---|---|
| Output a screen reader follows | `CONSOLEMONSTER_ACCESSIBLE=1` | Prompts ask numbered questions, apps, menus and grids show static or numbered views, live sessions narrate on new lines instead of redrawing, status symbols always show and motion is reduced. |
| Less movement, same layout | `CONSOLEMONSTER_MOTION=reduced` | Spinners show a static glyph and the screen redraws at most once a second; the final state is kept. |
| No colour | `NO_COLOR=1` | Colour goes, the layout and status words stay. |
| High contrast | `CONSOLEMONSTER_THEME=highcontrast` | The high-contrast palette. On Windows it is chosen automatically when High Contrast is on and no theme is set. |
| Colour-blind safe | `CONSOLEMONSTER_THEME=colorblind` | A palette that keeps pass, warning and failure apart without relying on red and green, in a dark or light variant chosen from the background. |

## Meaning never depends on colour

Every status carries a symbol or a word as well as a colour: in tables, status lists, trees, gauges, bars, calendars, flows and diagrams. Focus is shown with a pointer, reverse video or brackets, so you can see where you are with every colour removed. Without colour, chart series get their own marks and the legend shows them.

{{< cm-frame ids="nocolor,plain" caption="`NO_COLOR=1` keeps the layout and the status words; `CONSOLEMONSTER_MODE=plain` also switches to ASCII." >}}

## High contrast

{{< cm-frame ids="theme-dark,theme-contrast" caption="The same summary in `Theme.Dark` and `Theme.HighContrast`." >}}

## Colour-blind safe

Pass is blue, warning is yellow (dark gold on light backgrounds) and failure is rose (magenta on light backgrounds) and underlined. The three stay apart for protanopia and deuteranopia, and every status keeps its symbol.

{{< cm-frame ids="theme-colorblind,theme-colorblind-light" caption="The same summary in `Theme.ColorBlindSafe` and `Theme.ColorBlindSafeLight`." >}}

`CONSOLEMONSTER_THEME=colorblind` and `Set-ConsoleTheme ColorBlindSafe` pick the light or dark variant from the terminal background. Where the background can't be detected, `colorblind-light` and `colorblind-dark`, or `Set-ConsoleTheme ColorBlindSafeLight` and `ColorBlindSafeDark`, choose one.

In code, `Theme.HighContrast`, `Theme.ColorBlindSafe` and `Theme.ColorBlindSafeLight` are the same palettes, and a [theme file](/docs/themes/) can start from any of them.

## Accessible output

`CONSOLEMONSTER_ACCESSIBLE=1` is meant for the whole session. `Set-ConsoleSetting Accessible On -Save` keeps it for every session without a profile line, and `Set-ConsoleTheme ColorBlindSafe -Save`, `Set-ConsoleSetting Motion Reduced -Save` and `Set-ConsoleSetting Mouse Off -Save` do the same for the palette, motion and the mouse, and `Set-ConsoleSetting StatusMarkers Words -Save` writes statuses as `[OK]`, `[FAIL]` and `[WARN]` instead of symbols; `Show-ConsoleSettings` previews them first. A single prompt can ask for it with `-Mode Accessible`, and in C# `InteractiveOptions.Default.Mode = InteractionMode.Accessible` applies it everywhere. Explicit requests for rich or live output don't override it.

Live sessions in accessible mode write one line per change, the same narration they use in CI logs and redirected output:

{{< cm-frame id="profile-ci" caption="Stream narration: one line per change, no redraws, no colour." >}}

## Reduced motion

`CONSOLEMONSTER_MOTION=reduced` is for people who want the normal dashboard without the movement. `LiveSessionOptions.ReducedMotion` and `ConsoleAppOptions.ReducedMotion` set it in code, and `-ReducedMotion` on `Show-ConsoleApp`, `Invoke-ConsoleDashboard`, `Use-ConsoleProgress`, `Start-ConsoleRun`, `Invoke-ConsoleRun` and `Watch-Console` sets it for one call. Accessible mode always implies it.

## Next

[Environment overrides](/docs/environment/) lists every variable, and [themes](/docs/themes/) shows the palettes and theme files.
