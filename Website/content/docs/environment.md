---
title: Environment overrides
description: NO_COLOR and the CONSOLEMONSTER_* variables pin what detection would otherwise decide, for CI, tests, screenshots and accessibility.
slug: environment
order: 4
group: start
card: overrides-plain
apis: [NO_COLOR, CONSOLEMONSTER_*]
summary: "NO_COLOR and the CONSOLEMONSTER_* variables."
---

Variables are read when the profile is detected. An invalid value is ignored and noted in the profile's decisions, so `Get-ConsoleCapability` shows why it had no effect.

| Variable | Values | Effect |
|---|---|---|
| `NO_COLOR` | any value | No colour anywhere, including host output in PowerShell. Status symbols stay. |
| `CONSOLEMONSTER_COLOR` | `none`, `16`, `256`, `truecolor` or `24bit` | The colour depth. |
| `CONSOLEMONSTER_GLYPHS` | `full`, `basic`, `ascii` | The glyph tier: box drawing, block and braille characters, status symbols and spinners. |
| `CONSOLEMONSTER_MODE` | `interactive` (or `live`), `stream`, `plain` | Interactive redraws in place; stream writes append-only lines; plain writes ASCII lines without colour or escape sequences. |
| `CONSOLEMONSTER_THEME` | `dark`, `light`, `auto` | The theme preference. `auto` reads the background in conhost. |
| `CONSOLEMONSTER_CULTURE` | a culture name, `invariant` or `current` | Numbers and dates in tables and presenters. An unknown name is ignored. |
| `CONSOLEMONSTER_LANGUAGE` | a language name such as `de` or `pl` | The language of built-in labels, separately from the number culture. German, Spanish, French and Italian are built in; others fall back to English. |
| `CONSOLEMONSTER_EMOJI_WIDTH` | `1`, `2` or `auto` | The width of emoji, for consoles that draw them narrow. |
| `CONSOLEMONSTER_AMBIGUOUS_WIDTH` | `1`, `2` or `auto` | The width of East Asian ambiguous characters. |
| `CONSOLEMONSTER_NONINTERACTIVE` | `1` | Prompts return their default or fail with a message that names the parameter to pass. |
| `CONSOLEMONSTER_ACCESSIBLE` | `1` | Prompts ask plain numbered questions with no redraws, and every answer is confirmed as text. |
| `CONSOLEMONSTER_UPDATE_GOLDEN` | `1` | `GoldenFile` rewrites golden files instead of comparing. |

Detection also reads `CI` and the variables of common CI systems (GitHub Actions, Azure Pipelines, Jenkins, GitLab, TeamCity), `TERM` (`dumb` selects plain mode), `COLORTERM` and `WT_SESSION`.

## The same table under overrides

The script is the PowerShell page's `Format-ConsoleTable` example; only the environment changes.

{{< cm-frame ids="nocolor,plain" caption="NO_COLOR=1 keeps the layout and the status words; CONSOLEMONSTER_MODE=plain also switches to ASCII." >}}

## In code

The same choices are available without the environment: `TerminalProfile.Fixed(width, height, colors, glyphs)` for a profile with no detection, and `WithCulture`, `WithThemePreference`, `WithSize`, `WithColorDepth` and `WithMode` to change one value. Live sessions take `LiveSessionOptions.Profile`, `Theme` and `Culture`.