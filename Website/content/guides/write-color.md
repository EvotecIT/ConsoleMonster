---
title: Coming from Write-Color
description: Move Write-Color lines to Write-ConsoleText, with the same pieces-and-colours call, theme roles, alignment and a text log.
slug: write-color
card: shaping
order: 2
---

`Write-ConsoleText` keeps the call shape of `Write-Color` from [PSWriteColor](https://github.com/EvotecIT/PSWriteColor): a list of text pieces and a colour for each. Most lines change only the command name and one or two parameter names. Both modules can be loaded in the same session, so a script can move one line at a time.

{{< cm-frame id="write-consoletext" script="true" caption="`Write-ConsoleText` on Windows PowerShell 5.1: pieces with colours, markup, tabs, spaces, padding with a character and a gradient." >}}

## Parameter map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Write-Color | Write-ConsoleText | Difference |
| --- | --- | --- |
| `-Text` (`-T`) | `-Text` (first position) | Pieces are literal. Rich text from `New-ConsoleText` is accepted too. |
| `-Color` (`-C`, `-ForegroundColor`, `-FGC`) | `-Color` | ConsoleColor names, theme roles (`Pass`, `Warn`, `Fail`, `Muted`, `Accent`), `#RRGGBB` and `rgb()`. Pieces beyond the colours take the first one, as in Write-Color. Without `-Color` the terminal's own colour is used; Write-Color uses White. |
| `-BackGroundColor` (`-B`, `-BGC`) | `-BackgroundColor` | Same rule for extra pieces. |
| `-StartTab` (`-Indent`), `-StartSpaces` | `-Indent` | Counts spaces. A tab is usually eight cells, so `-StartTab 1` becomes `-Indent 8`. |
| `-LinesBefore`, `-LinesAfter` | same | |
| `-ShowTime`, `-DateTimeFormat` | `-ShowTimestamp`, `-TimestampFormat` | Same default format, `yyyy-MM-dd HH:mm:ss`. |
| `-NoNewLine` | same | Also works in transcripts and jobs. |
| `-HorizontalCenter` | `-Alignment Center` | Measured in terminal cells, so emoji and CJK text centre correctly. `-Width` sets the width to centre in. |
| `-LogFile` (`-L`) | `-LogFile` | Each line is logged with its timestamp. A new log is UTF-8 without a byte order mark; an existing Write-Color log keeps its UTF-16 encoding. |
| `-LogTime`, `-LogRetry`, `-Encoding` | No parameter | The log always carries the time, tries twice under a file lock with a warning per failed attempt, and picks the encoding as above. |
| `-PadLeft`, `-PadRight`, `-PadCenter`, `-PadCharacter` | No parameter | Pad the string first (`'FS01'.PadRight(10, '.')`) or use `-Alignment` with `-Width`. |
| `-NoConsoleOutput` (`-HideConsole`), `-OutputStream` | No parameter | For a log-only line, use `Add-Content`. |

</div>

## Shared sample data

Install with `Install-Module ConsoleMonster -Scope CurrentUser` and run `Import-Module ConsoleMonster`; for the left-hand examples, `Install-Module PSWriteColor`. Each example uses the same rows; none changes a real server.

```powershell
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
```

## 1. Coloured pieces

<div class="cm-migration">
<div>

**Write-Color**

```powershell
foreach ($server in $servers) {
    $status = if ($server.Status -eq 'Pass') { 'Green' } else { 'Yellow' }
    Write-Color -Text $server.Name, ' at ', $server.Site, ': ', $server.Status `
        -Color Cyan, Gray, White, Gray, $status
}
```

</div>
<div>

**ConsoleMonster**

```powershell
foreach ($server in $servers) {
    Write-ConsoleText -Text $server.Name, ' at ', $server.Site, ': ', $server.Status `
        -Color Accent, Muted, Muted, Muted, $server.Status
}
```

</div>
</div>

Theme roles are colours too, so a status word can colour itself: `Pass` is green and `Warn` yellow in the active theme, and both follow a light or dark theme without changes. ConsoleColor names still work and render as `Write-Host` shows them.

## 2. Timestamps, indentation and a log

<div class="cm-migration">
<div>

**Write-Color**

```powershell
foreach ($server in $servers) {
    Write-Color -Text $server.Name, ' pending: ', $server.Pending `
        -Color Cyan, Gray, Yellow -StartTab 1 -ShowTime -LogFile ./patching.log
}
```

</div>
<div>

**ConsoleMonster**

```powershell
foreach ($server in $servers) {
    Write-ConsoleText -Text $server.Name, ' pending: ', $server.Pending `
        -Color Accent, Muted, Warn -Indent 8 -ShowTimestamp -LogFile ./patching.log
}
```

</div>
</div>

The log receives plain text with the time first, so last month's Write-Color log and this month's lines sit in one file. Add ``-SplitLines "`n"`` to keep a multi-line message aligned under the time, or `-Wrap` to wrap a long one with a hanging indent.

## 3. A centred heading and a line that continues

<div class="cm-migration">
<div>

**Write-Color**

```powershell
Write-Color -Text 'Patch window opens' -Color Yellow -HorizontalCenter -LinesBefore 1
Write-Color -Text 'Checking ', $servers[1].Name, ' ' -Color Gray, Cyan -NoNewLine
Write-Color -Text 'done' -Color Green
```

</div>
<div>

**ConsoleMonster**

```powershell
Write-ConsoleText -Text 'Patch window opens' -Role Warn -Alignment Center -LinesBefore 1
Write-ConsoleText -Text 'Checking ', $servers[1].Name, ' ' -Color Muted, Accent -NoNewLine
Write-ConsoleText -Text 'done' -Role Pass
```

</div>
</div>

`-Role` colours every piece that has no colour of its own. For a line that updates in place while work runs, use `Show-ConsoleStatusLine 'Checking ', $name -Color Muted, Accent -Spinner` and end it with `Show-ConsoleStatusLine 'Checked' -Status Pass -Complete`; outside a real console each call writes a plain line.

## Differences to know

- `Write-ConsoleText` adds `-Markup` (`'[pass]Ready[/] in [muted]3 s[/]'`), `-Decoration`, `-Link`, `-Gradient`, `-Wrap`, `-Width` and `-SplitLines`. Pieces stay literal; only `-Markup` is parsed.
- In a transcript each call is one line. In jobs and remoting the colours travel with the output as information records. Redirected output is plain text. [Hosts, CI and transcripts](/docs/hosts-and-ci/) covers each route.
- `DarkRed` is ANSI red and `Red` is bright red, so a converted script looks as it did with `Write-Host -ForegroundColor`.
- Log options are fixed rather than parameters: no `-LogTime $false`, no `-Encoding` and no retry count.

## Sources

Write-Color's parameters and defaults are from [Write-Color.ps1 in PSWriteColor](https://github.com/EvotecIT/PSWriteColor/blob/master/Public/Write-Color.ps1), checked 10 November 2026. ConsoleMonster's are in the [Write-ConsoleText reference](/powershell/commands/write-consoletext/).

## Next

- [The PowerShell page](/powershell/): rich text, status lines and gradients.
- [Themes](/docs/themes/): the roles `-Color` and `-Role` accept.
- [Coming from Format-Table](/guides/format-table/): the tables beside your messages.
- [All migration guides](/guides/coming-from/).
