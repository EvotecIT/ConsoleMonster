using ConsoleMonster;
using ConsoleMonster.Graphics;
using ConsoleMonster.Markdown;

DiagramRendererProvider.Register();

// A Mermaid fence in Markdown draws as a diagram; without Graphics it stays code.
string content = """
# Home network

Everything leaves through the router.

```mermaid
flowchart LR
  laptop[Laptop] -->|Wi-Fi| router[Router]
  phone[Phone] -->|Wi-Fi| router
  router --> dns[DNS]
  router --> internet[Internet]
```
""";
Terminal.Write(MarkdownConsole.Parse(content));
