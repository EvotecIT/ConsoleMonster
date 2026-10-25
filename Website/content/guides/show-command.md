---
title: Coming from Show-Command
description: Replace Windows command dialogs with explicit terminal forms, validated answers and command previews.
slug: show-command
order: 6
toc: false
card: migration-form
---

A terminal form is useful when a guided operation must work over SSH or without Windows Desktop. ConsoleMonster can collect typed fields, validate them and show a command preview while your script retains control over execution. This replaces the workflow of a Show-Command dialog; it does not automatically generate a form for every installed cmdlet or reproduce Show-Command's command browser.

## Status and scope

Microsoft documents [Show-Command](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/show-command?view=powershell-7.5) as Windows-only, reintroduced in PowerShell 7 and requiring Windows Desktop. It is unavailable on Server Core and Nano Server. Checked 25 October 2026. It ships with PowerShell, so it has no independent module release date; this guide makes no claim that it is abandoned.

## Operation map

<div class="cm-migration-map" tabindex="0" role="region" aria-label="Scrollable comparison table">

| Show-Command workflow | ConsoleMonster | Difference |
| --- | --- | --- |
| `Show-Command -Name Get-Service` | `New-ConsoleForm` and `New-ConsoleField` | Define fields explicitly; no automatic metadata-to-form bridge. |
| Type parameters, then Run | `Read-ConsoleForm`, then an explicit command call | Submission returns answers; your script chooses what to execute. |
| `Show-Command -PassThru` | `Read-ConsoleForm -PassThru` / `Format-ConsoleCommand` | Full form result includes `Submitted`, `Values` and `CommandLine`; a preview is text. |
| Browse available commands | `Show-ConsoleHelp` / `Get-Command` | Help and discovery are separate from form entry. |
| Parameter-set tabs | Explicit fields/pages and validation | Model only the supported task; no automatic parameter-set parity. |

</div>

## Shared sample data

Use these illustrative rows while preparing a service-inventory query. The sample asks for a server name; it does not connect to the sample machines.

```powershell
Import-Module ConsoleMonster
$servers = @(
    [pscustomobject] @{ Name = 'FS01'; Site = 'Warsaw'; Pending = 4; Status = 'Pass' }
    [pscustomobject] @{ Name = 'SQL01'; Site = 'Dublin'; Pending = 12; Status = 'Warn' }
    [pscustomobject] @{ Name = 'WEB01'; Site = 'Berlin'; Pending = 0; Status = 'Pass' }
)
```

## 1. Replace a command dialog with focused questions

<div class="cm-migration">
<div>

**Before**

```powershell
# Windows Desktop; fills the native command dialog.
Show-Command -Name Get-Service
```

</div>
<div>

**ConsoleMonster**

```powershell
$form = New-ConsoleForm -Title 'Service inventory' -Command 'Get-Service' -Content {
    New-ConsoleField -Key Name -Label 'Service name' -Type Text `
        -Required -ParameterName Name
}
Set-ConsoleFormValue -Form $form -Values @{ Name = 'WinRM' }
$answers = $form | Read-ConsoleForm
if ($null -ne $answers) {
    Get-Service -Name $answers.Name
}
```

</div>
</div>

The right-hand example deliberately models one read-only command parameter. `Get-Service` itself is Windows-only in modern PowerShell, so run that last call on Windows; forms are cross-platform, target commands may not be. To use a cross-platform target, replace the command and fields with those of your own inventory function.

## 2. Preview a command without executing it

<div class="cm-migration">
<div>

**Before**

```powershell
# Show-Command returns command text instead of running it.
$command = Show-Command -Name Get-Service -PassThru
```

</div>
<div>

**ConsoleMonster**

```powershell
$form = New-ConsoleForm -Title 'Service inventory' -Command 'Get-Service' -Content {
    New-ConsoleField -Key Name -Label 'Service name' -Type Text `
        -Required -ParameterName Name
}
Set-ConsoleFormValue -Form $form -Values @{ Name = 'WinRM' }
$result = $form | Read-ConsoleForm -PassThru
if ($result.Submitted) {
    Write-ConsoleText -Text $result.CommandLine
}
```

</div>
</div>

The initial WinRM value is applied explicitly. Form command previews omit values equal to a field default, so use field defaults only when omitting that parameter has the same meaning for the target command.

`-PassThru` has different return types: Show-Command returns command text, while Read-ConsoleForm returns a structured result. Keep that result test separate from an execution decision. Never feed preview text to `Invoke-Expression`; call the command with a reviewed splat.

## 3. Build a reviewable invocation from selected rows

<div class="cm-migration">
<div>

**Before**

```powershell
# Windows Desktop: choose connection parameters for remoting.
$command = Show-Command -Name Invoke-Command -PassThru
```

</div>
<div>

**ConsoleMonster**

```powershell
$server = $servers | Read-ConsoleSelect 'Inventory target' -Display Name
$arguments = @{ Server = $server.Name; IncludeStopped = $true }
$command = Format-ConsoleCommand -Command 'Get-ServerInventory' -Arguments $arguments
Write-ConsoleText -Text $command
# Preview only. If your function exists, call it explicitly with @arguments.
```

</div>
</div>

`Format-ConsoleCommand` quotes values for a copyable command string and does not execute it. The right side models a custom `Get-ServerInventory` command; it does not require that function just to create the preview. Only execute your real function after validating and selecting its arguments.

## Differences to know

- `New-ConsoleForm` creates a definition; `Read-ConsoleForm` owns input and returns answers only after submission. Without `-PassThru`, cancellation yields no submitted answer object.
- Field `Key` identifies stored answers, `Label` is visible text and `ParameterName` controls command preview binding. They need not be identical.
- Form review and validation are task-specific. Show-Command discovers parameter sets dynamically; ConsoleMonster expects you to define the supported operation.
- Persistence is explicit through form options. Do not enable remembered answers for secrets just to imitate a dialog.
- Previewing a command does not validate authorization, reachability or target-platform support, and does not execute the command. The calling script owns that decision.

## See a complete form

This small form collects a service name without executing anything. Its initial screen comes from the exact program shown below. The [interactive wizard](/interactive/) demonstrates a larger form with dependent fields and a command review.

{{< cm-try ids="migration-form" project="ServiceSetup" >}}

## Next

Explore [interactive screens](/interactive/), [the form command reference](/powershell/commands/read-consoleform/) or [all migration guides](/guides/coming-from/).
