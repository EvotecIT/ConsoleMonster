---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsolePhases
## SYNOPSIS
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

Phases come from -Phase (names, all pending) or from a script block of New-ConsoleStatusItem calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-ConsolePhase, which also measures their duration.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsolePhases [[-WidgetId] <string>] [[-Phase] <Object[]>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-StepCounter] [<CommonParameters>]
```

## DESCRIPTION
Creates a phase strip for pipelines: Restore › Build › Test › Pack.

Phases come from -Phase (names, all pending) or from a script block of New-ConsoleStatusItem calls, each with its status and a detail such as a duration. The active phase is emphasised and finished phases show their detail. To add phases later, pipe the strip to Add-ConsoleItem; in a live dashboard, update phases with Update-ConsolePhase, which also measures their duration.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsolePhases -Content {
    New-ConsoleStatusItem 'Discover' -Status Pass -Detail '2.1s'
    New-ConsoleStatusItem 'Collect' -Status Pass -Detail '14s'
    New-ConsoleStatusItem 'Evaluate' -Status Running -Detail '64%'
    New-ConsoleStatusItem 'Report'
} | Show-Console
```

A run part-way through

### EXAMPLE 2
```powershell
New-ConsolePhases -Phase 'Restore', 'Build', 'Test', 'Pack'
```

Build phases for a live dashboard

## PARAMETERS

### -Content
A script block that emits the phases: New-ConsoleStatusItem calls (or phase names).

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Phases from the pipeline: New-ConsoleStatusItem objects or names.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Phase
The phase names in order, all pending (a script block here is taken as -Content).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StepCounter
Draws [01/05] counters with right-aligned results from each item's Detail. Wrapped labels hang under the label text; results move below when the complete label and result cannot share a row.

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

### -WidgetId
The live name (default phases).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Phases`

## RELATED LINKS

- None
