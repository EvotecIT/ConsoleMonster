# ConsoleMonster roadmap

## Before the first 2.0.0 release
- Validate on Windows Server 2016 (16 colours), 2019, 2022, 2025, Server Core, VS Code, SSH (ConPTY) and the PowerShell ISE fallback.
- Owner review of the prototype screenshots and the public API names.
- Decide the package signing setup (`CertificateThumbprint` in `Build/project.build.json` is empty for local validation builds).
- Spinner widget and heat strip from the v1 widget list.
- Stream-mode narration prefix (elapsed time) and per-widget narration options.

## v1.x (only with a real consumer)
- ChartForgeX.Terminal: ChartForgeX scenes on the ConsoleMonster canvas.
- Hyperlinks (OSC 8) in markup (`[link=...]` is parsed and shown as text today).
- Markdown-ish text.
- Declarative JSON layouts with data binding.
- Spectre.Console adapter (widgets as `IRenderable`).
- `Microsoft.Extensions.Logging` provider writing into a log region.
- Remote sources (named pipe or HTTP event stream) so a console can watch another process or service.

## Later
- Prompts and input, mouse, full interactive TUI applications.
