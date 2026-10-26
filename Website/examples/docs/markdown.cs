using ConsoleMonster.Markdown;

string content = """
# Deployment notes

Read **the plan**, keep *rollback notes*, and verify each host.
This paragraph reflows to the terminal width.

## Before the rollout

1. Check the selected environment.
2. Inspect the results:
   - web-01 is ready.
   - web-02 needs another check.

> Keep the rollback plan close.
> Confirm the target before changing it.

| Host | Checks | Notes |
| :--- | ---: | :--- |
| web-01 | 12 | Health checks passed. |
| web-02 | 3 | Verify the maintenance window. |

```powershell
Get-Service | Select-Object Name, Status
```

Reference: [deployment guide](https://example.org/deploy).
""";
MarkdownConsole.Show(content);
