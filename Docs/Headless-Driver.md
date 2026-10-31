# Headless driver, recordings, browser host and debug socket

ConsoleMonster draws through a console driver: something that takes its output, reports the window size and supplies
input. The process console is one driver. `HeadlessTerminal` is another: the same app, prompt, menu or dashboard runs
exactly as it would in a terminal (the same VT output, alternate screen and cell diffs), and a built-in terminal model
keeps the screen. That makes apps testable, recordable and drivable the way web pages are.

| You want to | Use |
|---|---|
| Test an app in CI or against a golden file | `HeadlessTerminal` with `Send`, `WaitForText` and `Snapshot` |
| Record an app, with its log panel, footer and dialogs | `Invoke-ConsoleRecording -Headless`, `HeadlessTerminal.Record` or `LiveSessionOptions.Recorder` |
| Turn captured screens into GIF, APNG, SVG or an HTML player | `ScreenCapture.ToRecordedFrames`, `Recording.Export`, `SaveGraphics` |
| Show an app in a web page for docs, demos or visual tests | `BrowserHost` |
| Let an agent inspect and drive a running app | `DebugSocket` (`CONSOLEMONSTER_HEADLESS` + `CONSOLEMONSTER_DEBUG_SOCKET`) |

## Testing an app

```csharp
using var terminal = new HeadlessTerminal(80, 24);
Task<AppState> run = ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions {
    Interactive = new InteractiveOptions { Target = terminal }
});
terminal.WaitForText("Color", TimeSpan.FromSeconds(5));
terminal.Send("Down Enter");                      // ScriptedInput syntax: keys, "text", paste:"x", resize:80x24, wait:250
terminal.WaitForText("Picked Green", TimeSpan.FromSeconds(5));
GoldenFile.Match("picked.txt", terminal.Snapshot().ToPlainText());
terminal.Send("Escape");
AppState state = await run;
```

`Snapshot()` returns the visible cells with their styles; `FrameRendered` reports every completed screen. Resize with
`Resize(width, height)`; the app gets a resize event just as it would from a real window. `CompleteInput()` ends the
input after the queued keys, so an app driven by a fixed script finishes on its own.

The terminal defaults to true colour, full glyphs, hyperlinks and the invariant culture, so screens don't depend on the
machine. Pass a culture to test translated labels.

## Recording what the user sees

A recording made in the process holds the composed screen: the app's own controls plus the captured log panel, the
footer, dialogs and prompts. It is an asciicast v2 file, so every existing export works on it.

```powershell
Invoke-ConsoleRecording -Headless -Width 100 -Height 30 -InputScript 'wait:800 Down Down wait:600 Enter wait:1200 Escape' -Path ./app.cast {
    Show-ConsoleApp -Content {
        New-ConsoleSelect -Key Color -Choices Red, Green, Blue
        New-ConsoleBinding -WidgetId Picked -Content { param($State) "Picked $($State.Color)" }
    }
}
Export-ConsoleRecording -RecordingPath ./app.cast -Path ./app.gif
Export-ConsoleRecording -RecordingPath ./app.cast -Path ./app.svg
```

`-Headless` runs the script in the current session (your variables and functions are available) against a headless
terminal of `-Width` by `-Height` cells. `wait:` pauses happen in real time, so the recording keeps a reading pace. Labels
follow the same language and number formats your console would use.

In C#, record a headless terminal with `terminal.Record(new SessionRecordingOptions { Path = "app.cast" })`. To record
an app running in a real terminal, give its live session an in-process recorder:

```csharp
using var recorder = SessionRecorder.Start(new SessionRecordingOptions { Path = "demo.cast", InProcess = true });
ConsoleApp.Run(layout, options: new ConsoleAppOptions { Live = new LiveSessionOptions { Recorder = recorder } });
recorder.Dispose();
SessionRecording.Read("demo.cast").SaveGraphics("demo.gif", GraphicsFormat.Gif);   // ConsoleMonster.Graphics
```

`LiveSessionOptions.RecordPath` still records dashboard updates for replay into the same dashboard definition
(`Show-ConsoleReplay`); it cannot hold an app's composed screen.

Screens captured with `ScreenCapture` (scripted prompts, menus and apps) export as animations too:
`capture.ToRecordedFrames(TimeSpan.FromMilliseconds(500))` gives timed frames for `Recording.Export(frames,
ExportFormat.Svg)` (also `Html` and `Asciicast`) and for `frames.SaveGraphics("flow.gif", GraphicsFormat.Gif)` in
ConsoleMonster.Graphics.

## Browser host

```csharp
using var terminal = new HeadlessTerminal(100, 30);
using var host = BrowserHost.Start(terminal, new BrowserHostOptions { Title = "Deploy demo" });
Console.WriteLine(host.Address);   // http://127.0.0.1:<port>/?t=<token>
await ConsoleApp.RunAsync(layout, options: new ConsoleAppOptions { Interactive = new InteractiveOptions { Target = terminal } });
```

The page is self-contained (no CDN, fonts or external scripts), updates as the screen changes and sends keys and clicks
back. The server listens on 127.0.0.1 only, and a request must arrive complete within 10 seconds. Every request needs the token from `Address`; a request for another host name
(DNS rebinding) is refused, and input is accepted only when posted by the page itself (matching `Origin` and the token
header). Secret patterns are masked on every screen it sends. For a published site, record the app instead and embed the
recording's player; the live host is for local docs work, demos and visual tests.

## Debug socket for agents

The debug socket lets an agent inspect and drive a running app the way browser tests drive a page. It is off unless the
host starts it:

- in code: `DebugSocket.Start(terminal)`;
- for an unmodified app such as `cmon` or the TestimoX CLI: set `CONSOLEMONSTER_HEADLESS=120x40` (the process runs on a
  headless terminal) and `CONSOLEMONSTER_DEBUG_SOCKET=0` (any free port) or a port number. `CONSOLEMONSTER_DEBUG_TOKEN`
  sets the token (at least 16 characters); otherwise a random one is generated for each run. The process removes these
  variables from its environment once it has read them, so programs it starts get a normal console and no socket.

The socket writes a discovery file, `<process id>.json` under `%LOCALAPPDATA%\ConsoleMonster\Debug` (the user's local
application data folder elsewhere), with `pid`, `port`, `token` and `protocol`. It is removed when the socket stops.
`Module/Examples/Example-DebugSocket.ps1` is a complete client: it starts an app, connects, types, waits, reads the
screen and the control tree, and quits.

### Wire protocol (version 1)

TCP on 127.0.0.1. Each request and each reply is one JSON object on one line (UTF-8, ending with `\n`, at most 1 MiB).
A request has `cmd` and optionally `id`, which the reply repeats. Numbers may be sent as JSON numbers or strings; replies
send them as numbers.

The first request on a connection must be `hello` with the token, at most 4 KiB, within 10 seconds of connecting; anything
else, including a malformed line, gets an `unauthorized` error and the connection closes.

| Request | Reply fields |
|---|---|
| `{"cmd":"hello","token":"…"}` | `protocol`, `pid`, `width`, `height` |
| `{"cmd":"screen"}` or `{"cmd":"screen","format":"text"}` | `width`, `height`, `text` (rows joined with `\n`) |
| `{"cmd":"screen","format":"png"}` | `width`, `height`, `png` (base64); the app must register the ConsoleMonster.Graphics PNG provider (`PngFrameExportProvider.Register()`) |
| `{"cmd":"tree"}` | `root` (the control tree), `focus` (the focused input's key), `truncated`, `layout` |
| `{"cmd":"keys","keys":"Down Enter"}` | none; `ScriptedInput` syntax (a key repeats at most 10000 times, and at most 100000 steps wait at a time) |
| `{"cmd":"text","text":"Ada"}` | none; types the characters literally |
| `{"cmd":"resize","width":100,"height":30}` | none; the app redraws at the new size on its next frame, so `wait` for expected text before reading the screen |
| `{"cmd":"click","x":4,"y":2,"button":1}` | none; button 1 left, 2 middle, 3 right |
| `{"cmd":"wait","text":"Done","timeout":5000}` | `found` (true or false), matched against the masked screen |

Every successful reply has `"ok":true`. A failed one has `"ok":false`, `error` (`unauthorized`, `bad-request` or
`failed`) and `message`.

A tree node has `type`, and when they apply `id` (the widget id), `key`, `label`, `focused`, `disabled`, `bounds`
(`x`, `y`, `width`, `height` in screen cells) and `children`. `root` is null when no app or dashboard is running (a prompt or menu on its own has no control tree). Bounds are reported
for input controls. The tree is read while the app runs, so it is a snapshot. `layout` is false
until the layout pass can report why each control has its size; it will then add the measured size and the constraint
that decided it.

### Security model

- **Off by default.** Nothing listens unless the host calls `DebugSocket.Start` or sets both environment variables.
  `CONSOLEMONSTER_DEBUG_SOCKET` alone does nothing on a real console.
- **Local only.** The socket binds 127.0.0.1 (never a wildcard or external address) and closes any connection whose
  remote end is not loopback. It accepts at most four connections.
- **A token per run.** A client proves it can read the discovery file, which lives in the user's own profile. On Linux
  and macOS the folder and file are created readable by that user alone (this needs .NET 8 or later; older frameworks
  refuse to start the socket there). Tokens are compared in constant time, and a process publishes one socket per folder.
- **Bounded.** At most four connections; a client that has not said hello within 10 seconds is disconnected, however
  slowly it sends.
- **No secrets in output.** Every screen goes through the same secret patterns as recordings (passwords, tokens, API
  keys, bearer headers, GitHub and cloud keys); matches are replaced with `*`, link targets that hold one are dropped,
  and `wait` matches against the masked screen. Patterns are matched per screen row, so a secret split across two rows
  is not recognised. Extra patterns go in `DebugSocketOptions.Redact`. The tree reports types, names, labels, bounds and focus, never input values. Errors from
  app code report only their type.
- **Only this process.** The socket shows the headless terminal of the process that started it, nothing else on the
  machine. The token is never printed; the start-up line on standard error shows the address and the discovery file.

## Limits

- A child process handed the terminal (`TerminalHandoff`) still needs a real console.
- The debug socket drives a headless terminal; it does not mirror a real console window.
- Layout reasons in `tree` wait for the layout pass described in the app navigation work.
