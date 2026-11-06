Import-Module ConsoleMonster

# A Mermaid fence in Markdown draws as a diagram.
$content = @'
# Home network

Everything leaves through the router.

```mermaid
flowchart LR
  laptop[Laptop] -->|Wi-Fi| router[Router]
  phone[Phone] -->|Wi-Fi| router
  router --> dns[DNS]
  router --> internet[Internet]
```
'@

Show-Console -Content $content -NoPager -View Markdown
