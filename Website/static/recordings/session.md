```text
PS C:\demo> git log --oneline --graph --color -5
* c5a0005 (HEAD -> main) Document local recording
* ed6e38c Track build progress
* 6d56a12 Render report table
* 5272ee2 Add synthetic fixtures
* 1471238 Start sample app
PS C:\demo> Invoke-DemoBuild
Build succeeded. 24 tests passed.
PS C:\demo> Show-DemoReport
Build report
╭────────────┬─────────────┬──────────╮
│ Check      │ Result      │ Duration │
├────────────┼─────────────┼──────────┤
│ Compile    │ ✓ Passed    │ 1.8 s    │
│ Unit tests │ ✓ 24 passed │ 2.4 s    │
│ Package    │ ✓ Ready     │ 0.6 s    │
╰────────────┴─────────────┴──────────╯

PS C:\demo> Get-Item C:\demo\missing.json
Get-Item: Cannot find path 'C:\demo\missing.json' because it does not exist.
PS C:\demo> Show-DemoToken $env:API_TOKEN
API_TOKEN=••••••• (redacted)
PS C:\demo> exit
```
