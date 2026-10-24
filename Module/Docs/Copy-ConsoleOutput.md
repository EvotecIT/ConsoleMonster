---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Copy-ConsoleOutput
## SYNOPSIS
Copies widgets, frames, text or pipeline objects to the operating-system clipboard.

Serializes input with ConsoleExporter as Text (default), Markdown or Html and copies the source as Unicode text. Markdown uses GFM tables, property/value lists and nested tree bullets, including the shared value presenters and totals. Text preserves the console layout inside a text fence. A rendered frame or live-session snapshot retains a fenced fallback. Windows uses its native clipboard, macOS uses pbcopy, and Linux uses wl-copy (Wayland) or xclip (X11). SSH, a missing desktop/provider or a failing provider produces a non-terminating error; use -ErrorAction Stop to catch it. No output is written unless -PassThru succeeds. -WhatIf exports without accessing the clipboard.

## SYNTAX
### __AllParameterSets
```powershell
Copy-ConsoleOutput [-InputObject] <Object> [-Format <string>] [-Width <int>] [-Height <int>] [-Culture <cultureinfo>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Copies widgets, frames, text or pipeline objects to the operating-system clipboard.

Serializes input with ConsoleExporter as Text (default), Markdown or Html and copies the source as Unicode text. Markdown uses GFM tables, property/value lists and nested tree bullets, including the shared value presenters and totals. Text preserves the console layout inside a text fence. A rendered frame or live-session snapshot retains a fenced fallback. Windows uses its native clipboard, macOS uses pbcopy, and Linux uses wl-copy (Wayland) or xclip (X11). SSH, a missing desktop/provider or a failing provider produces a non-terminating error; use -ErrorAction Stop to catch it. No output is written unless -PassThru succeeds. -WhatIf exports without accessing the clipboard.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | Select-Object -First 10 Name, Id | Show-ConsoleTable -AsWidget | Copy-ConsoleOutput -Format Markdown
```

Copy a process table into a Markdown report

## PARAMETERS

### -Culture
The culture for values in objects and widgets.

```yaml
Type: CultureInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Format
Copies Text, Markdown or Html source as clipboard text (default Text).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Markdown, Html

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Height in cells, or 0 to measure the full content.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Widgets, rendered frames, live sessions, literal text or objects to display as tables.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
Also returns the serialized document after a successful copy.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Width in cells for rendering widgets and objects (default 0: terminal width, falling back to 100); single frames keep their own size.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `System.String`

## RELATED LINKS

- None
