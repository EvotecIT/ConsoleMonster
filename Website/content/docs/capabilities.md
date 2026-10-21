---
title: Capability detection
description: ConsoleMonster detects VT support, colour depth, the glyphs the font can draw, size, redirection, CI and the background, and records why.
slug: capabilities
order: 2
group: start
card: profile-detected
apis: [TerminalProfile, Get-ConsoleCapability]
summary: "What ConsoleMonster detects about the console, and the decisions it records."
---

Detection runs once and produces a `TerminalProfile`: the output mode, the colour depth, the glyph tier, the window size, the theme preference and the culture. Every value carries the reason it was chosen, which `Get-ConsoleCapability` (and `Terminal.Profile.Decisions` in C#) lists. The frames below are the script shown under the first one, run in a Windows console window with different environments.

{{< cm-frame id="profile-detected" caption="Nothing overridden: VT is available, the Windows build supports 24-bit colour, and the console font (Consolas) has no braille, so the glyph tier is Basic." >}}

## What is detected

- **Mode.** Interactive on a VT console, Stream when output is redirected or a CI system is detected, Plain when asked for. Stream and Plain write lines instead of redrawing.
- **Colour depth.** 24-bit, 256, 16 or none. `NO_COLOR` turns colour off.
- **Glyph tier.** Full, Basic or ASCII. On conhost the console font is read to choose; in stream and CI output the tier is Basic.
- **Size.** The visible window, not the screen buffer, so nothing is written past the window or into its bottom-right cell.
- **Widths.** Display widths come from Unicode 17 tables, with emoji and ambiguous widths that can be overridden. A width probe reads the console screen buffer and never reads input.
- **Background.** In conhost, `auto` reads the console background colour to choose the light or dark theme. Windows Terminal does not expose its colour scheme to console programs, so there `CONSOLEMONSTER_THEME=light` selects the light theme.

{{< cm-frame id="profile-ci" script="false" caption="With `CI=true` and `NO_COLOR=1`: stream mode, no colour, Basic glyphs and no width probing." >}}

## Overriding detection

Any decision can be pinned with an environment variable (see [Environment overrides](/docs/environment/)) or in code: `TerminalProfile.Fixed(width, height, colors, glyphs)` builds a profile without detection, and `TerminalProfile.WithCulture`, `WithThemePreference` and `WithSize` change one value. `Terminal.Write(widget, profile)` renders with it:

```csharp
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Widgets;

Rule rule = new Rule("[bold]Release notes[/]");
Terminal.Write(rule);                                                          // as detected
Terminal.Write(rule, Terminal.Profile.WithColorDepth(ColorDepth.None));       // the same console, no colour
Terminal.Write(rule, TerminalProfile.Fixed(40, 10, ColorDepth.Ansi16, GlyphTier.Ascii));  // a fixed profile
```

{{< cm-frame id="profile" script="false" caption="The profile the site uses for its captures: `CONSOLEMONSTER_COLOR`, `_GLYPHS`, `_THEME` and `_CULTURE` pinned." >}}

## Fonts and resizing during a session

In conhost, the console font is polled about once a second; a new face or size re-derives the glyph tier and the emoji and ambiguous widths. In Windows Terminal, widths are measured again after every resize, which also covers zooming with Ctrl+= and Ctrl+0. Print-once output reads the window size on every write, so output printed after a resize uses the new width.