---
title: Linux and SSH
description: What ConsoleMonster detects on Linux and macOS terminals and over SSH, and a login overview for your servers.
slug: linux-and-ssh
order: 11
group: hosts
card: login
chips: [Linux, SSH]
summary: "Detection on Unix terminals and a login overview for your servers."
---

The module runs on PowerShell 7 on Linux and macOS, and the .NET library on every platform .NET runs on. Detection follows the terminal, not the operating system:

- **Glyphs follow the locale.** Unix terminals draw what the locale's character set allows: `LC_ALL`, then `LC_CTYPE`, then `LANG` decide, as POSIX says. A UTF-8 locale gets the full glyph set (box drawing, braille, status symbols); `C`, `POSIX` and other non-UTF-8 locales get ASCII, so nothing prints as question marks.
- **Colour follows `TERM` and `COLORTERM`.** `COLORTERM=truecolor` (or `24bit`) gives 24-bit colour, a `TERM` containing `256color` gives 256 colours, and other xterm-style terminals 16. `TERM=dumb` and `NO_COLOR` turn colour off.
- **Redirected output is plain text.** Pipes, files, `ssh host command` and CI logs get lines without escape codes, and live views narrate as lines.
- **The size comes from the terminal**, and live views follow window resizes.

`Get-ConsoleCapability` prints what was detected and why, and the [environment overrides](/docs/environment/) pin any decision.

{{< cm-demo id="login" heading="h2" >}}

The script prints host facts, health checks and disk use once and exits, so login stays fast. Every collector is cheap and falls back quietly, so a missing tool never blocks a login, and `-Sample` uses invented data so the layout can be tried anywhere.

## Why the login guards look like that

Step 4 above runs the overview only for interactive SSH sessions with a terminal. `ssh host command` also loads the pwsh profile, so the pwsh guard checks for a terminal (`SSH_TTY`) and output that is not redirected, not only `SSH_CONNECTION`, and `CM_OVERVIEW_SHOWN` keeps nested shells from printing it twice. Bash reads `~/.bash_profile` instead of `~/.profile` when the first exists, so add the bash block there in that case.

Output that is not a terminal (`scp`, `rsync`, `ssh host command`) is skipped by both guards: the pwsh guard needs a terminal and output that is not redirected, the bash one an interactive shell, and ConsoleMonster prints plain text when output is redirected anyway.

## Known limit: start-up time

Each login that runs the overview starts PowerShell and imports the module before anything prints, which takes noticeably longer than a shell script. Measure it on your servers with `Measure-Command { pwsh -NoProfile -File ~/bin/Example-LoginOverview.ps1 }` before turning it on for everyone, keep the collectors cheap, and keep it to interactive sessions as above. A .NET tool published with native AOT starts faster when that matters.

## Related

For a ready-made login summary, use the [host overview](/docs/host-overview/). Any decision detection makes can be pinned with an [environment override](/docs/environment/).
