---
title: Moving from 2.0
description: Replace removed ConsoleMonster 2.0 commands with their 2.1 equivalents.
slug: moving-from-2
order: 30
---

ConsoleMonster 2.1 consolidates the command surface. The 2.0.x names below are removed; use the replacement rather than an alias or compatibility wrapper.

Live updates and log entries require an active dashboard containing the named widget; the replay example requires an existing recording. Selection and secret examples ask for input. In a tree view, use `-Include *` to show all members. Parameter names and output can differ from 2.0; check the replacement command help.

| 2.0.x command | 2.1 replacement | Example |
| --- | --- | --- |
| `Add-ConsoleValue` | `Update-Console` | `Update-Console latency -Append -Value 12` |
| `Format-ConsoleCode` | `Show-Console -View Code` | `'Get-Process' \| Show-Console -View Code` |
| `Format-ConsoleError` | `Show-Console -View Error` | `[Exception]::new('Example') \| Show-Console -View Error` |
| `Format-ConsoleJson` | `Show-Console -View Json` | `[pscustomobject]@{ Name = 'web01' } \| Show-Console -View Json` |
| `Format-ConsoleList` | `Show-ConsoleList` | `Get-Process \| Select-Object -First 1 \| Show-ConsoleList` |
| `Format-ConsoleTable` | `Show-ConsoleTable` | `Get-Process \| Select-Object -First 5 \| Show-ConsoleTable` |
| `Format-ConsoleTree` | `Show-Console -View Tree` | `Get-Process \| Select-Object -First 1 \| Show-Console -View Tree` |
| `New-ConsoleBarChart` | `New-ConsoleChart -Type Bar` | `@{ Label = 'web'; Value = 5 } \| New-ConsoleChart -Type Bar` |
| `New-ConsoleGrid` | `New-ConsoleLayoutGrid` | `New-ConsoleLayoutGrid` |
| `New-ConsoleHistogram` | `New-ConsoleChart -Type Histogram` | `1, 2, 3 \| New-ConsoleChart -Type Histogram -Edges 0, 2, 4` |
| `New-ConsoleLineChart` | `New-ConsoleChart -Type Line` | `New-ConsoleChart -Type Line -Series 'latency'` |
| `New-ConsoleLiveTable` | `New-ConsoleTable -Live` | `[pscustomobject]@{ Name = 'web01' } \| New-ConsoleTable -Live` |
| `New-ConsoleLog` | `New-ConsoleLogRegion` | `New-ConsoleLogRegion -Height 5` |
| `New-ConsoleSparkline` | `New-ConsoleChart -Type Sparkline` | `1, 2, 3 \| New-ConsoleChart -Type Sparkline` |
| `New-ConsoleSpinner` | `New-ConsoleProgress -Indeterminate` | `New-ConsoleProgress -Indeterminate` |
| `Read-ConsoleMultiSelection` | `Read-ConsoleSelect -Multiple` | `Read-ConsoleSelect -Title 'Servers' -Choices 'web01', 'web02' -Multiple` |
| `Read-ConsoleSecret` | `Read-ConsoleText -AsSecret` | `Read-ConsoleText -Title 'Password' -AsSecret` |
| `Read-ConsoleSelection` | `Read-ConsoleSelect` | `Read-ConsoleSelect -Title 'Server' -Choices 'web01', 'web02'` |
| `Set-ConsoleRegion` | `Update-Console -Content` | `Update-Console main -Content (New-ConsoleText 'Done')` |
| `Set-ConsoleValue` | `Update-Console -Value` | `Update-Console build -Value 5` |
| `Start-ConsoleDashboard` | `Invoke-ConsoleDashboard` | `Invoke-ConsoleDashboard -Dashboard (New-ConsoleText 'Working') -ScriptBlock { 'Done' }` |
| `Start-ConsoleReplay` | `Show-ConsoleReplay -PlaybackMode Automatic` | `Show-ConsoleReplay -Dashboard (New-ConsoleText 'Replay') -Path './run.jsonl' -PlaybackMode Automatic` |
| `Update-ConsoleLane` | `Update-Console` | `Update-Console lanes -Key web01 -Status Pass` |
| `Update-ConsolePhase` | `Update-Console` | `Update-Console phases -Key build -Status Pass` |
| `Update-ConsoleProgress` | `Update-Console` | `Update-Console build -Value 5 -Total 10` |
| `Update-ConsoleSpinner` | `Update-Console` | `Update-Console spinner -Label 'Working'` |
| `Update-ConsoleStatus` | `Update-Console` | `Update-Console checks -Key dns -Status Pass` |
| `Update-ConsoleTable` | `Update-Console -Row` | `Update-Console results -Row ([pscustomobject]@{ Name = 'web01'; Status = 'Pass' })` |
| `Write-ConsoleException` | `Show-Console -View Error` | `[Exception]::new('Example') \| Show-Console -View Error` |
| `Write-ConsoleKeyValue` | `Show-ConsoleList` | `@{ Server = 'web01'; Status = 'Pass' } \| Show-ConsoleList` |
| `Write-ConsoleLog` | `Write-ConsoleLogEntry` | `Write-ConsoleLogEntry 'Finished' -Level Pass` |
| `Write-ConsoleMarkup` | `Write-ConsoleText -Markup` | `Write-ConsoleText -Markup '[pass]Done[/]'` |
| `Write-ConsoleRule` | `Show-Console -View Rule` | `Show-Console -View Rule -Title 'Results'` |
| `Write-ConsoleStatus` | `Show-ConsoleStatusLine` | `Show-ConsoleStatusLine -Text 'Ready'` |
| `Write-ConsoleTable` | `Show-ConsoleTable` | `Get-Process \| Select-Object -First 5 \| Show-ConsoleTable` |
| `Write-ConsoleTree` | `Show-Console -View Tree` | `Get-Process \| Select-Object -First 1 \| Show-Console -View Tree` |
| `Write-ConsoleWidget` | `Show-Console` | `New-ConsoleText 'Ready' \| Show-Console` |
