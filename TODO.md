# ConsoleMonster roadmap

## Before the first 2.0.0 release
- Validate on Windows Server 2016 (16 colours), 2019, 2022, 2025, Server Core, VS Code, SSH (ConPTY) and the PowerShell ISE fallback.
- Owner review of the prototype screenshots and the public API names.
- Decide the package signing setup (`CertificateThumbprint` in `Build/project.build.json` is empty for local validation builds).
- Heat strip widget from the v1 widget list.
- Per-widget narration options for stream mode.
- Light background detection in Windows Terminal: needs an OSC 11 query that never consumes user input (today `CONSOLEMONSTER_THEME=light` is required there).
- Table v2: column sorting and filtering in live tables and horizontal scrolling as an alternative to hiding columns.
- Inspector: PowerShell format data (`*.format.ps1xml`) views as an input to column selection.

## v1.x (only with a real consumer)
- ChartForgeX.Terminal: ChartForgeX scenes on the ConsoleMonster canvas.
- Hyperlinks (OSC 8) in markup (`[link=...]` is parsed and shown as text today; the Link cell presenter already emits OSC 8).
- Markdown-ish text.
- Declarative JSON layouts with data binding.
- Spectre.Console adapter (widgets as `IRenderable`).
- `Microsoft.Extensions.Logging` provider writing into a log region.
- Remote sources (named pipe or HTTP event stream) so a console can watch another process or service.

## Later
- Prompts and input, mouse, full interactive TUI applications.
