---
title: Linux and SSH
description: What ConsoleMonster detects on Linux and macOS terminals and over SSH, and a login overview for your servers.
slug: linux-and-ssh
order: 9
---

The module runs on PowerShell 7 on Linux and macOS, and the .NET library on every platform .NET runs on. Detection follows the terminal, not the operating system:

- **Glyphs follow the locale.** Unix terminals draw what the locale's character set allows: `LC_ALL`, then `LC_CTYPE`, then `LANG` decide, as POSIX says. A UTF-8 locale gets the full glyph set (box drawing, braille, status symbols); `C`, `POSIX` and other non-UTF-8 locales get ASCII, so nothing prints as question marks.
- **Colour follows `TERM` and `COLORTERM`.** `COLORTERM=truecolor` (or `24bit`) gives 24-bit colour, a `TERM` containing `256color` gives 256 colours, and other xterm-style terminals 16. `TERM=dumb` and `NO_COLOR` turn colour off.
- **Redirected output is plain text.** Pipes, files, `ssh host command` and CI logs get lines without escape codes, and live views narrate as lines.
- **The size comes from the terminal**, and live views follow window resizes.

`Get-ConsoleCapability` prints what was detected and why, and the [environment overrides](/docs/environment/) pin any decision.

{{< cm-demo id="login" heading="h2" >}}

`Example-LoginOverview.ps1` prints host facts, health checks and disk use once and exits, so login stays fast. Every collector is cheap and falls back quietly, so a missing tool never blocks a login, and `-Sample` uses invented data so the layout can be tried anywhere.

## Show it when you log in

Run it only for interactive SSH sessions with a terminal. With pwsh as the login shell, add this to `~/.config/powershell/Microsoft.PowerShell_profile.ps1`. `ssh host command` also loads the profile there, so the guard checks for a terminal (`SSH_TTY`) and output that is not redirected, not only `SSH_CONNECTION`:

```powershell
if ($env:SSH_TTY -and -not [Console]::IsOutputRedirected -and -not $env:CM_OVERVIEW_SHOWN) {
    $env:CM_OVERVIEW_SHOWN = 1
    & ~/bin/Example-LoginOverview.ps1
}
```

With bash as the login shell, add this to the end of `~/.profile` (or `~/.bash_profile` when you have one, because bash then skips `~/.profile`):

```bash
case $- in
    *i*) if [ -n "$SSH_TTY" ] && command -v pwsh >/dev/null; then
             pwsh -NoLogo -NoProfile -File ~/bin/Example-LoginOverview.ps1
         fi ;;
esac
```

Output that is not a terminal (`scp`, `rsync`, `ssh host command`) is skipped by both guards: the pwsh guard needs a terminal and output that is not redirected, the bash one an interactive shell, and ConsoleMonster prints plain text when output is redirected anyway.

## Known limit: start-up time

Each login that runs the overview starts PowerShell and imports the module before anything prints, which takes noticeably longer than a shell script. Measure it on your servers with `Measure-Command { pwsh -NoProfile -File ~/bin/Example-LoginOverview.ps1 }` before turning it on for everyone, keep the collectors cheap, and keep it to interactive sessions as above. A .NET tool published with native AOT starts faster when that matters.