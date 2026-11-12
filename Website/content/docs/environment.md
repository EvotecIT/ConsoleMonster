---
title: Environment overrides
description: NO_COLOR and the CONSOLEMONSTER_* variables pin what detection would otherwise decide, for CI, tests, screenshots and accessibility.
slug: environment
order: 5
group: start
card: overrides-plain
apis: [NO_COLOR, CONSOLEMONSTER_*]
summary: "NO_COLOR and the CONSOLEMONSTER_* variables."
---

Variables are read when the profile is detected. They take precedence over the [settings file](/docs/themes/#keep-a-theme-for-every-session) that `Show-ConsoleSetting` and `Set-ConsoleSetting -Save` write, and `Get-ConsoleSetting` shows which one each value came from. An invalid value is ignored and noted in the profile's decisions, so `Get-ConsoleCapability` shows why it had no effect. `CONSOLEMONSTER_NOW` is the exception: it is read each time a date is shown, and an invalid value falls back to the clock without a note.

| Variable | Values | Effect |
|---|---|---|
| `NO_COLOR` | any value | No colour anywhere, including host output in PowerShell. Status symbols stay. |
| `CONSOLEMONSTER_COLOR` | `none`, `16`, `256`, `truecolor` or `24bit` | The colour depth. |
| `CONSOLEMONSTER_GLYPHS` | `full`, `basic`, `ascii` | The glyph tier: box drawing, block and braille characters, status symbols and spinners. |
| `CONSOLEMONSTER_MODE` | `interactive` (or `live`), `stream`, `plain` | Interactive redraws in place; stream writes append-only lines; plain writes ASCII lines without colour or escape sequences. |
| `CONSOLEMONSTER_CI` | `github`, `azure`, `gitlab`, `none` | Overrides native CI groups, annotations and job-summary routing. Without it, only `GITHUB_ACTIONS=true`, `TF_BUILD=true` and `GITLAB_CI=true` select a protocol. Unknown values disable provider commands. This does not change terminal capability detection. |
| `CONSOLEMONSTER_THEME` | `dark`, `light`, `highcontrast`, `colorblind`, `colorblind-light`, `colorblind-dark`, `auto` | The palette. `auto` honors Windows High Contrast before reading the console background; `colorblind` picks its light or dark variant from the background the same way; an explicit theme takes precedence. |
| `CONSOLEMONSTER_BORDER` | `rounded`, `single`, `double`, `heavy`, `ascii` or `auto` | The frame for panels, tables and inputs that use the default frame; `auto` uses the theme's. A widget's own frame stays. |
| `CONSOLEMONSTER_DENSITY` | `comfortable` or `compact` | Default widget spacing. Comfortable keeps standard spacing; Compact tightens padding and gaps. Explicit widget spacing stays as authored; an unset variable uses the theme. |
| `CONSOLEMONSTER_STATUS_MARKERS` | `symbols`, `words` or `auto` | Status symbols (✓ ✗ ▲) or bracketed words (`[OK]` `[FAIL]` `[WARN]`) in status columns, lists and headers; `auto` uses the theme's. |
| `CONSOLEMONSTER_CULTURE` | a culture name, `invariant` or `current` | Numbers and dates in tables and presenters. An unknown name is ignored. |
| `CONSOLEMONSTER_LANGUAGE` | a language name such as `de` or `pl` | The language of built-in labels, separately from the number culture. German, Spanish, French and Italian are built in; others fall back to English. |
| `CONSOLEMONSTER_EMOJI_WIDTH` | `1`, `2` or `auto` | The width of emoji, for consoles that draw them narrow. |
| `CONSOLEMONSTER_AMBIGUOUS_WIDTH` | `1`, `2` or `auto` | The width of East Asian ambiguous characters. |
| `CONSOLEMONSTER_NONINTERACTIVE` | `1` | Prompts return their default or fail with a message that names the parameter to pass. |
| `CONSOLEMONSTER_ACCESSIBLE` | `1` | Numbered prompts, static apps, menus and grids, and append-only live narration. Status symbols stay visible and reduced motion is implied. |
| `CONSOLEMONSTER_MOTION` | `reduced` | Keeps the normal layout with static spinners and at most one redraw per second. |
| `CONSOLEMONSTER_NOMOUSE` | `1` | Prompts, menus, pagers, the object explorer and apps leave the mouse to the terminal, for its own selection and scrollback. Every action keeps its key. |
| `CONSOLEMONSTER_HYPERLINKS` | `on`, `off`, `auto` (also `true`/`1`, `false`/`0`) | Clickable OSC 8 links for URLs and file paths. Detection turns them on in Windows Terminal, VS Code, iTerm2, WezTerm, Kitty and Ghostty (outside tmux, screen and Zellij panes), VTE terminals and Konsole; redirected, CI, stream and plain output never get them and print the target after the text instead, as `text (url)`. |
| `CONSOLEMONSTER_NOW` | an ISO 8601 time with `Z` or an offset, such as `2026-10-21T12:00:00Z` | The "now" that relative dates (`3 h ago`) are counted from, for recordings and tests. Read each time a value is shown; unset or invalid uses the clock. |
| `CONSOLEMONSTER_SETTINGS` | a file path, or `none` | The settings file to read and save instead of `settings.json` in `%APPDATA%\ConsoleMonster` or `~/.config/ConsoleMonster`; `none` ignores it, for tests and recordings that need fixed output. |
| `CONSOLEMONSTER_UPDATE_GOLDEN` | `1` | `GoldenFile` rewrites golden files instead of comparing. |

Detection also reads `CI` and the variables of common CI systems (GitHub Actions, Azure Pipelines, Jenkins, GitLab, TeamCity), `TERM` (`dumb` selects plain mode), `COLORTERM` and `WT_SESSION`.

Redirected output (files, pipes and CI build logs) is as wide as `COLUMNS` when it is set. Without it, PowerShell commands use the width the host reports and other programs use 100 columns; `Get-ConsoleCapability` reports the width that applies. A terminal window always uses its own width.

[Accessibility](/docs/accessibility/) explains accessible output, reduced motion and the high-contrast and colour-blind palettes, with frames.

## The same table under overrides

The script below is the same for both frames; only the environment changes. Detection reads the variables once per session, so run the script in a new one (`powershell -NoProfile -File .\format-consoletable.ps1` on Windows PowerShell 5.1), then clear the variable:

```powershell
$env:NO_COLOR = 1
pwsh -NoProfile -File ./format-consoletable.ps1
Remove-Item Env:NO_COLOR

$env:CONSOLEMONSTER_MODE = 'plain'
pwsh -NoProfile -File ./format-consoletable.ps1
Remove-Item Env:CONSOLEMONSTER_MODE
```

In the session you are in, `Get-ConsoleCapability -Refresh` detects again after you change a variable.

In bash, set it for one command: `NO_COLOR=1 pwsh -File ./format-consoletable.ps1`.

{{< cm-frame ids="nocolor,plain" caption="`NO_COLOR=1` keeps the layout and the status words; `CONSOLEMONSTER_MODE=plain` also switches to ASCII." >}}

## Relative dates in recordings

`CONSOLEMONSTER_NOW` sets the moment that relative dates count from. It is read each time a value is shown, so it also reaches widgets made earlier; remove it after the recording.

{{< cm-frame id="relative-dates" caption="Three dates shown with the `DateRelative` presenter, counted from `CONSOLEMONSTER_NOW` instead of the clock." >}}

## Clickable links

In Windows Terminal, VS Code, iTerm2, WezTerm, Kitty and Ghostty (outside tmux, screen and Zellij panes), VTE terminals and Konsole, URLs and file paths in tables, lists and log lines become OSC 8 links. Elsewhere they stay text. A column can ask for it with the `Link` presenter; `Write-ConsoleText -Link` and `New-ConsoleText -Link` give a link its own label:

```powershell
Import-Module ConsoleMonster

Get-ChildItem -File | Show-ConsoleTable -Property Name, Length, @{ Name = 'FullName'; Header = 'Open'; Presenter = 'Link' }
Write-ConsoleText 'Project documentation' -Link 'https://consolemonster.dev/docs/'

# Off for this session, then back to detection.
$env:CONSOLEMONSTER_HYPERLINKS = 'off'
Get-ConsoleCapability -Refresh | Select-Object Kind, SupportsHyperlinks
Remove-Item Env:CONSOLEMONSTER_HYPERLINKS
Get-ConsoleCapability -Refresh | Select-Object Kind, SupportsHyperlinks
```

## In code

The same choices are available without the environment: `TerminalProfile.Fixed(width, height, colors, glyphs)` for a profile with no detection, and `WithCulture`, `WithThemePreference`, `WithSize`, `WithColorDepth` and `WithMode` to change one value. Live sessions take `LiveSessionOptions.Profile`, `Theme` and `Culture`.

```csharp
using ConsoleMonster;
using ConsoleMonster.Capabilities;
using ConsoleMonster.Widgets;

KeyValueList facts = new KeyValueList().Add("Status", "passed").Add("Duration", "4.2 s");
// No colour, then plain output, for these two writes only:
Terminal.Write(facts, Terminal.Profile.WithColorDepth(ColorDepth.None));
Terminal.Write(facts, Terminal.Profile.WithMode(OutputMode.Plain));
```

In PowerShell, `Get-ConsoleCapability` shows the profile these variables produced, with the reason for each value.

## Related

What each variable overrides is explained in [capability detection](/docs/capabilities/). Without any of them, [hosts, CI and transcripts](/guides/hosts-and-ci/) shows what each environment gets.
