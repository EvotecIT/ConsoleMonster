---
title: Themes
description: The dark, light and high-contrast themes, the roles they define and how their colours are reduced for 256 and 16-colour consoles.
slug: themes
order: 3
group: start
card: theme-dark
chips: [Dark, Light, High contrast]
summary: "Dark, light and high contrast, and how colours are reduced per tier."
---

Widgets never name colours directly. They use theme roles: `Text`, `Muted`, `Accent`, `Pass`, `Warn`, `Fail`, `Info`, `Skip`, `Running`, `Pending`, `Border`, `Title`, `Header`, `Surface`, `Focus` and six chart series. A theme maps each role to a 24-bit colour with its 256 and 16-colour fallbacks, so a role stays distinguishable on every tier.

{{< cm-frame ids="theme-dark,theme-light,theme-contrast" caption="One summary in Theme.Dark, Theme.Light and Theme.HighContrast." >}}

## Choosing the theme

- `CONSOLEMONSTER_THEME=light|dark|auto` (default `auto`), or `TerminalProfile.WithThemePreference` in code.
- `auto` reads the console background colour in conhost. In Windows Terminal, which does not report its scheme to console programs, `auto` picks the dark theme; set `CONSOLEMONSTER_THEME=light` for a light scheme.
- `Set-ConsoleTheme` in PowerShell, `LiveSessionOptions.Theme` for one session.

The light theme meets WCAG contrast on white, and the dark theme keeps titles readable on a light background that could not be detected, because titles, headers and highlights use the terminal's own foreground in bold.

## Markup and roles

Theme roles are markup tags (`[pass]`, `[fail]`, `[warn]`, `[muted]`, `[accent]` and the others), also as backgrounds (`[white on fail]`). In PowerShell, `-Role` on `New-ConsoleText` and `Write-ConsoleText` does the same. Without colour, a status role adds its symbol, so status is never shown by colour alone.

## Reduction per tier

Colours given as hex, `rgb()` or palette names are reduced for each tier the way the SGR encoder sends them: to the nearest of the 256 xterm colours on a 256-colour console, and to the nearest of the 16 console colours on a 16-colour one. Theme roles use the 256 and 16-colour values the theme defines instead of the nearest match. The [widget gallery](/widgets/) shows every widget at each tier.