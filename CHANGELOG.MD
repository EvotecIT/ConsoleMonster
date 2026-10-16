### 2.0.0 - prototype (unreleased)
#### What's new
- Rebuilt as a zero-dependency .NET library (netstandard2.0, net472, net8.0) plus a thin binary PowerShell module for Windows PowerShell 5.1 and PowerShell 7.
- Terminal capability detection with `CONSOLEMONSTER_COLOR`, `CONSOLEMONSTER_GLYPHS`, `CONSOLEMONSTER_MODE` and `NO_COLOR` overrides; Unicode-safe output through `WriteConsoleW`.
- Spectre-compatible markup, semantic themes (dark, light, high contrast) and Full/Basic/ASCII glyph tiers.
- Widgets: text, rule, panel, table, tree, key/value, status list, progress, lanes, phases, bar and stacked bar, sparkline, histogram, braille line/scatter chart, log region, logo and dashboard; layouts Rows, Columns, Grid, Padding.
- Live sessions: thread-safe updates coalesced at about 15 frames per second, console and PowerShell stream capture, Ctrl+C restore, JSON-lines record and replay.
- 38 cmdlets with a plain `Console` noun (`Write-ConsoleTable`, `New-ConsoleDashboard`, `Start-ConsoleDashboard`, ...).

#### Breaking changes
- The Spectre.Console / Terminal.Gui functions of 0.0.x (`New-SpectreImage`, `New-SpectreRule`, `New-Terminal*`) are removed; 0.0.4 remains on the PowerShell Gallery.

### 0.0.4 - 2024.10.26
#### What's new
- Bump dependencies
- Small fixes

#### What's Changed
* Bump SixLabors.ImageSharp from 2.1.3 to 2.1.7 in /Sources/ConsoleMonster by @dependabot in https://github.com/EvotecIT/ConsoleMonster/pull/2
* Bump SixLabors.ImageSharp from 2.1.7 to 2.1.8 in /Sources/ConsoleMonster by @dependabot in https://github.com/EvotecIT/ConsoleMonster/pull/3

#### New Contributors
* @dependabot made their first contribution in https://github.com/EvotecIT/ConsoleMonster/pull/2

**Full Changelog**: https://github.com/EvotecIT/ConsoleMonster/compare/v0.0.3...v0.0.4
### 0.0.3 - 2023.01.01
- Removed Libraries from GIT, you will have to build them using PSPublishModule in the builds folder if you want to play with GIT version
- Updated versions of libraries:
  - Spectre.Console 0.45.0
  - Terminal.Gui 1.8.2

### 0.0.2 - 2022.09.06
- Improve `New-SpectreImage` to take URL as input
- Small improvements for other cmdlets but really WIP

### 0.0.1 - 2022.08.28
- Initial release to test things out
