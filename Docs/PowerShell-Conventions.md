# PowerShell conventions

I use the same vocabulary in PowerShell and C#, with small translations where the hosts differ. A command should tell me whether it creates a definition, shows it, asks a question or runs work.

## Commands by job

Question workflows (`Read-Console*`, forms and action menus) acquire answers or run a selected action, then return control to the script.
Application controls (`New-ConsoleSelect` and friends inside `Show-ConsoleApp`) retain focus, events and mutable state together in a persistent composed screen.

| Job | Primary command | Composition or lifecycle |
| --- | --- | --- |
| Show ordinary objects or a widget | `Show-Console` | Automatic view selection; accepts an `IWidget` directly. |
| Show a table, record or hierarchy | `Show-ConsoleTable`, `Show-ConsoleList`, `Show-ConsoleTree` | `New-ConsoleTable`, `New-ConsoleKeyValue`, `New-ConsoleTree` build definitions. `Show-ConsoleList` accepts a hashtable or ordered dictionary directly. |
| Inspect an object | `Show-ConsoleObject` | Keyboard explorer, or `-StaticSnapshot`, `-AsString` or `-AsWidget`. |
| Show a separator | `Show-ConsoleRule 'Section'` | `New-ConsoleRule` builds the same widget for a layout. |
| Show data as a chart | `Show-ConsoleChart` | `New-ConsoleBarChart`, `New-ConsoleLineChart`, `New-ConsoleSparkline` and `New-ConsoleHistogram` build reusable charts. |
| Write text | `Write-ConsoleText` | `New-ConsoleText` creates rich text for another widget. |
| Show an error | `Show-ConsoleError` | Displays the supplied exception or error record; does not emit another PowerShell error record. |
| Show a one-line status | `Show-ConsoleStatusLine` | `-Complete` finishes it. A dashboard status-list item uses `Update-ConsoleStatusItem`. |
| Follow a log source | `Watch-ConsoleLog` | `New-ConsoleLogRegion` builds a dashboard destination; `Write-ConsoleLogEntry` sends a message to it. |
| Run a dashboard | `Invoke-ConsoleDashboard` | Creates the live session, waits for the script and restores the terminal. |
| Run items | `Invoke-ConsoleRun` | `Start-ConsoleRun` returns a handle; `Wait-ConsoleRun` waits and `Complete-ConsoleRun` finalizes it. |
| Adapt native progress | `Use-ConsoleProgress` | Scopes existing `Write-Progress` calls. `New-ConsoleProgress` builds a dashboard bar; `Update-ConsoleProgress` addresses it. |
| Ask for an answer | `Read-ConsoleText`, `Read-ConsoleNumber`, `Read-ConsoleConfirm`, `Read-ConsoleSelect`, `Read-ConsoleMultiSelect`, `Read-ConsolePath`, `Read-ConsoleSecret` | `New-ConsoleForm` builds a form; `Read-ConsoleForm` collects its answers. |
| Run an action menu or app | `Show-ConsoleMenu`, `Show-ConsoleApp` | `New-ConsoleMenuItem` defines an action. `New-ConsoleSelect` and the other application controls persist in app state. |
| Record a child terminal | `Invoke-ConsoleRecording` | Supply a script and `-Destination`, or use `-Interactive`. Stop and marker commands apply inside the recorded child. |
| Play dashboard events | `Show-ConsoleReplay` | `-PlaybackMode Interactive` is the player; `Automatic` plays through the same event engine without keys. |
| Export output | `Export-ConsoleFrame`, `Export-ConsoleRecording` | Frame export accepts ordinary objects as well as widgets and snapshots. `Copy-ConsoleOutput` uses the same `-Format` vocabulary. |
| Browse embedded demos | `Get-ConsoleDemo`, `Show-ConsoleDemo` | `Export-ConsoleDemo <slug> -Path` writes the standalone script. |
| Present a script | `Show-ConsolePresentation -Path talk.ps1` | `Invoke-ConsolePresentation -Path talk.ps1 -Destination talk.cast` records typed execution unattended. C# retains `ConsoleDemo.Present` and `ConsoleDemo.Record`. |

Flow strips use `New-ConsoleFlowNode`, `New-ConsoleFlowEdge` and `New-ConsoleFlowBranch` items. Pipeline rows carry `Kind` (`Node`, `Edge` or `Branch`) and the item properties. Live flow commands use `-WidgetId`; see [Flow strips](Flow.md).
