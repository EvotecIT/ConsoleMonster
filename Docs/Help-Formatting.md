# Native PowerShell help in the console

`Enable-ConsoleFormatting -Feature Help` gives help typed at the prompt the existing `Show-ConsoleHelp` view. It is an
explicit session choice on Windows PowerShell 5.1 and PowerShell 7. It stays out of `-Feature All`, because help objects
are also useful script output and the interception of native formatting should be a deliberate choice.

```powershell
Import-Module ConsoleMonster
Enable-ConsoleFormatting -Feature Help

Get-Help Get-Process
Get-Help Get-Process -Detailed
Get-Help Get-Process -Full
Get-Help Get-Process -Examples
Get-Help Get-Process -Parameter Name
Get-Process -?

# Long topics use ConsoleMonster's pager through PowerShell's help function.
help Get-Process -Full

Disable-ConsoleFormatting -Feature Help
```

The view uses PowerShell's installed help: the synopsis, syntax per parameter set, parameter descriptions,
highlighted examples, inputs and outputs, notes and links. `-Full`, `-Detailed`, `-Examples` and `-Parameter` select the
corresponding sections. `-Online` and `-ShowWindow` keep PowerShell's native behavior. You can still request the same
view explicitly with `Show-ConsoleHelp Get-Process` without enabling the session feature.

`help` pages long content. The footer reads **Space next · q quit**; Space continues and q closes the pager. `Get-Help`
and `-?` print their help without adding paging. About topics remain PowerShell's literal text until the Markdown
feature is available; enabling Help does not introduce a Markdown dependency.

Ctrl+C stops the current pipeline and releases the pager's input lease. Quitting with q returns normally. Both paths
restore the console input mode before the next prompt.

## Script output

Only a proven direct route to the console uses the rich view. If the destination cannot be proved, PowerShell keeps
control. These examples produce the same native objects and file bytes with Help enabled or disabled:

```powershell
Get-Help Get-Process | Out-String
Get-Help Get-Process | Select-String 'Name'
Get-Help Get-Process | Out-File ./process-help.txt
Get-Help Get-Process > ./process-help.txt
$helpObject = Get-Help Get-Process -Full
$helpObject.parameters.parameter | Select-Object name
```

The same rule covers output variables, jobs, remoting, CI and active transcripts. `Get-Help` remains the native
cmdlet. The session installs an output adapter and a forwarding `help` function, and disabling the feature restores
the previous functions and aliases it replaced. Other enabled ConsoleMonster features keep their own settings.

## Proof

The [Help formatting contract suite](../Module/Tests/ConsoleMonster.HelpFormatting.Tests.ps1) exercises the native
producer on both PowerShell runtimes. Its fixture supplies full, detailed, examples, parameter and about help without
depending on downloaded help files. It compares serialized help objects, `Out-String`, pipeline continuations,
assignments, output variables, files, redirection, jobs, remoting where a local transport is available, and transcripts.
The host harness separately exercises actual direct-host rendering, paging and restoration. Unsupported engine or
host state always falls back to native help.
