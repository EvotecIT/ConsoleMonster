---
title: Test, record and drive apps
description: Run any app, prompt or dashboard on a headless terminal to test it against golden files, record it, show it in a browser or let an agent drive it.
slug: testing
toc: true
order: 19
group: apps
card: app
chips: [Headless terminal, Golden files, Browser host, Debug socket]
summary: "Headless tests, golden files, recordings, a browser view and a debug socket for agents."
---

ConsoleMonster draws through a console driver: something that takes its output, reports the window size and supplies input. The process console is one driver. `HeadlessTerminal` is another. The same app, prompt, menu or dashboard runs exactly as it would in a terminal, with the same VT output, alternate screen and cell diffs, and a built-in terminal model keeps the screen. That makes terminal apps testable, recordable and drivable the way web pages are.

| You want to | Use |
|---|---|
| Check a report or widget against a reviewed snapshot | `GoldenFile.Match` in C#, `Assert-ConsoleOutput` in PowerShell |
| Test an interactive app in CI | `HeadlessTerminal` with `Send`, `WaitForText` and `Snapshot` |
| Watch Pester runs as a live dashboard | `Invoke-ConsolePester` |
| Record an app with its log panel, footer and dialogs | `Invoke-ConsoleRecording -Headless` or `HeadlessTerminal.Record` |
| Show an app in a local web page | `BrowserHost` |
| Let an agent or a script inspect and drive a running app | `DebugSocket` |

## Golden files

A golden file is the reviewed text of what a widget draws. The check renders at a fixed width, glyph tier and culture (invariant by default), so the result is the same on every machine. The first run writes the file and fails so you can review it; a later difference fails with the first differing line, both versions and the column where they part. Set `CONSOLEMONSTER_UPDATE_GOLDEN=1` to rewrite the files after an intended change.

<div class="cm-lang" data-lang="powershell">

```powershell
It 'renders the server summary' {
    $summary = @(
        [pscustomobject] @{ Name = 'web-01'; Status = 'Ready' }
        [pscustomobject] @{ Name = 'db-01'; Status = 'Degraded' }
    ) | New-ConsoleTable -Property Name, Status
    Assert-ConsoleOutput -InputObject $summary -GoldenPath "$PSScriptRoot/Golden/summary.txt" -Width 80
}
```

</div>
<div class="cm-lang" data-lang="csharp">

```csharp
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;

var table = new Table().AddColumn("Name").AddColumn("Status")
    .AddValues("web-01", "Ready")
    .AddValues("db-01", "Degraded");
GoldenFile.Match("Golden/summary.txt", TestTerminal.RenderText(table, width: 80));
```

</div>
<div class="cm-lang" data-lang="csharp-fluent">

```csharp
using ConsoleMonster;
using ConsoleMonster.Testing;
using ConsoleMonster.Widgets;

Table table = Ui.Table().AddColumn("Name").AddColumn("Status")
    .AddValues("web-01", "Ready")
    .AddValues("db-01", "Degraded");
GoldenFile.Match("Golden/summary.txt", TestTerminal.RenderText(table, width: 80));
```

</div>

`TestTerminal` also drives prompts, menus and forms with scripted keys and captures the screens they draw, in any test framework.

## Interactive apps in CI

`Invoke-ConsoleHeadless` in PowerShell and `HeadlessTerminal` in C# run the real app loop on a terminal with no window. Send keys and clicks with the same script syntax the recorder uses (`Down Enter`, quoted text, `paste:`, `resize:80x24`, `wait:250`, and the mouse at zero-based cells: `click:4,5`, `dclick:`, `rclick:`, `drag:`, `wheel:`), then compare what is on the screen.

The program below runs a small app on a 56 × 10 headless terminal, presses Down and clicks FS01, keeps the screen and prints it in a panel, so the frame is exactly what a test would compare.

{{< cm-try key="headless" ids="headless-app" project="HeadlessTest" caption="Down and a click on FS01 at column 4, row 5: the screen the run kept." >}}

In a test, assert on the screen instead of printing it. `Invoke-ConsoleHeadless` returns the last screen's `Text` and `Cells`, every distinct screen in `Frames` and the script's `Output`; input ends after the script, so an app finishes on its own:

```powershell
It 'picks a server with the mouse' {
    $result = Invoke-ConsoleHeadless -Width 56 -Height 10 -InputScript 'Down click:4,5' {
        Show-ConsoleApp -Content { New-ConsoleSelect -Key Server -Choices WEB01, SQL01, FS01 }
    }
    $result.Text | Should -Match '› FS01'
}
```

In C#, `terminal.WaitForText` waits for a screen, and `GoldenFile.Match("picked.txt", terminal.Snapshot().ToPlainText())` compares it with a reviewed file.
`Snapshot()` returns the visible cells with their styles; `FrameRendered` reports each completed screen that differs from the previous one. `Resize(width, height)` sends the app a resize event, just as a real window would. The terminal defaults to true colour, full glyphs, hyperlinks and the invariant culture, so screens don't depend on the machine; pass a culture to test translated labels.

## Pester with a live dashboard

`Invoke-ConsolePester` takes the parameters of `Invoke-Pester` and shows the run while it happens: totals and the clock, one lane per test file with passed, failed and skipped counts, and the running test. `-FullScreen` adds the failures with expected and actual values, the slowest tests and a histogram of durations. When the run ends, a table of results by file and the failures stay in the scrollback. In CI logs and redirected output it writes one line per failure and a summary instead.

```powershell
Invoke-ConsolePester -Path ./Tests -FullScreen -ReportPath ./pester-report.html
```

## Recording what the user sees

A headless recording holds the composed screen: the app's own controls plus the captured log panel, the footer, dialogs and prompts. It is an asciicast v2 file, so it exports to GIF, APNG, SVG or an HTML player like any other recording.

```powershell
Invoke-ConsoleRecording -Headless -Width 100 -Height 30 -InputScript 'wait:800 Down Down wait:600 Enter wait:1200 Escape' -Path ./app.cast {
    Show-ConsoleApp -Content {
        New-ConsoleSelect -Key Color -Choices Red, Green, Blue
        New-ConsoleBinding -WidgetId Picked -Content { param($State) "Picked $($State.Color)" }
    }
}
Export-ConsoleRecording -RecordingPath ./app.cast -Path ./app.gif
```

`-Headless` runs the script in the current session, so your variables and functions are available. `wait:` pauses happen in real time and keep a reading pace. [Recording and replay](/docs/recording/) covers whole-session recordings of native commands.

## Browser host

`BrowserHost` serves a headless terminal as a self-contained web page, with no CDN, fonts or external scripts. The page updates as the screen changes and sends keys and clicks back to the app.

```csharp
using var terminal = new HeadlessTerminal(100, 30);
using var host = BrowserHost.Start(terminal, new BrowserHostOptions { Title = "Deploy demo" });
Console.WriteLine(host.Address);   // http://127.0.0.1:<port>/?t=<token>
await ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
    Interactive = new InteractiveOptions { Target = terminal }
});
```

Start it on the terminal of the headless program above, before the keys, and the page shows the same screens as they happen. The browser host is a C# API; to show a PowerShell app in a browser, host it from a small C# program or drive it through the debug socket below. The server listens on 127.0.0.1 only. Every request needs the token from `Address`, requests for another host name are refused, and input is accepted only when the page itself posts it. Secret patterns are masked on every screen it sends. Use it for local docs work, demos and visual tests; for a published site, record the app and embed the recording's player.

## Debug socket for agents

The debug socket lets an agent or a script inspect and drive a running app the way browser tests drive a page: read the screen as text or PNG, read the control tree with each control's bounds and layout rule, send keys, text, clicks and resizes, and wait for text. It is off unless the host starts it, in code with `DebugSocket.Start(terminal)` or, for an unmodified app or a PowerShell script, with environment variables:

```powershell
$env:CONSOLEMONSTER_HEADLESS = '120x40'
$env:CONSOLEMONSTER_DEBUG_SOCKET = '0'    # any free port
./MyTool.exe                               # or: pwsh -NoProfile -File ./inventory-app.ps1
```

The socket listens on 127.0.0.1, writes a discovery file with its port and a random token under the user's local application data folder, and accepts one JSON request per line after a `hello` with that token. The process removes these variables once it has read them, so programs it starts get a normal console. The wire protocol and its security model are documented in the repository's [Headless-Driver.md](https://github.com/EvotecIT/ConsoleMonster/blob/master/Docs/Headless-Driver.md).

## Next

- [CI integration](/docs/ci-integration/): groups, annotations and job summaries in CI logs.
- [App parts, one by one](/docs/app-parts/): small apps to drive this way.
- [Recording and replay](/docs/recording/): turn a run into a player or a GIF.
