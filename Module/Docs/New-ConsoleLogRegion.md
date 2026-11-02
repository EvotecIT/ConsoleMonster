---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleLogRegion
## SYNOPSIS
Creates a log region: the newest lines of a log, bottom-aligned.

Printed once, the lines come from a script block of Message/Level records calls (each with a level, a time and an optional Tag naming its source) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Invoke-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLogEntry. -Tag, -Level and -Filter show part of the log; hidden lines are kept.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLogRegion [[-WidgetId] <string>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-Height <int>] [-Capacity <int>] [-NoElapsed] [-Wrap] [-LevelColumn <ColumnFormat>] [-Tag <string>] [-Level <string>] [-Filter <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a log region: the newest lines of a log, bottom-aligned.

Printed once, the lines come from a script block of Message/Level records calls (each with a level, a time and an optional Tag naming its source) or from the pipeline. In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Invoke-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen; write to it explicitly with Write-ConsoleLogEntry. -Tag, -Level and -Filter show part of the log; hidden lines are kept.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLogRegion -Height 5 -Content {
    [pscustomobject]@{ Message = 'Discovered 4 domain controllers in 2 domains'; Level = 'Info'; AtSeconds = 2 }
    [pscustomobject]@{ Message = 'DC02: USN rollback check failed (event 2095)'; Level = 'Error'; AtSeconds = 44; Tag = 'DC02' }
    [pscustomobject]@{ Message = 'Using cached schema snapshot'; Level = 'Verbose'; AtSeconds = 53 }
} | Show-Console
```

A short log printed once

### EXAMPLE 2
```powershell
New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion }
```

A log that fills the rest of a dashboard

### EXAMPLE 3
```powershell
New-ConsoleLogRegion -Tag 'sql-index' -Level Warn -NoElapsed -Content {
    @{ Message = 'Rebuilding IX_Users_Name'; Tag = 'sql-index' }
    @{ Message = 'Fragmentation 41% on IX_Orders_Date'; Level = 'Warn'; Tag = 'sql-index' }
    @{ Message = 'Backup share is read-only'; Level = 'Error'; Tag = 'backup' }
} | Show-Console
```

Only warnings and errors from one job

## PARAMETERS

### -Capacity
Lines kept in memory (default 1000).

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

### -Content
A script block that emits the lines: Message/Level records calls, or strings for info lines.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Filter
Shows only lines containing this literal text. Case does not matter.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Rows requested when printed once (default 8).

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
Lines from the pipeline: Message/Level records objects or strings.

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

### -Level
Shows only lines at this level or worse: Warn shows warnings and errors, Verbose hides only debug lines.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LevelColumn
Shared LevelColumn layout settings as a hashtable, for example @{ MinWidth = 6; Alignment = 'Right' }.

```yaml
Type: ColumnFormat
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoElapsed
Hide the time at the start of each line.

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

### -Tag
Shows only lines from this source, as given by a record's Tag or Write-ConsoleLogEntry -Tag. Case does not matter.

```yaml
Type: String
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
The live widget identifier (default log).

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

### -Wrap
Wrap long lines (up to three rows each) instead of cutting them.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.LogRegion`

## RELATED LINKS

- None
