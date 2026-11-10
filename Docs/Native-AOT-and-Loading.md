# Native AOT and module loading

Details behind the short notes on the site's [Install](https://consolemonster.dev/docs/install/) page.

## Native AOT and trimming

The net8.0 and net10.0 targets are marked AOT-compatible. The object inspector's reflection member source is marked
`RequiresUnreferencedCode`, and `InspectorOptions.UseReflection = false` keeps trimmed builds safe.

Native AOT applications can use typed tables and the rendering and export APIs. For inspection, use dictionary, JSON or
custom member sources with reflection disabled, or explicitly preserve the properties you inspect. The general
inspection API still reports a trimming warning at the call site; suppress it only after checking those conditions.
Inferred property columns use alphabetical order when native metadata tokens are unavailable.

This AOT support applies to applications consuming the .NET library. The PowerShell module runs inside Windows
PowerShell or PowerShell 7 and is not itself a standalone native executable.

## How the module loads

The module ships net472 and net8.0 payloads for Windows PowerShell 5.1 and PowerShell 7. On PowerShell 7 it loads in its
own assembly load context, so binary modules that need different ConsoleMonster versions stay isolated. Windows
PowerShell 5.1 loads only one version of an assembly per session.

Version 2 replaces the Spectre.Console and Terminal.Gui based 0.0.x module, whose functions (`New-SpectreImage`,
`New-SpectreRule`, `New-Terminal*`) are removed; 0.0.4 stays available on the PowerShell Gallery.
