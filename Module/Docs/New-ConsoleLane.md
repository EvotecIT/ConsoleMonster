---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLane
## SYNOPSIS
Creates one lane (a worker, server or item of a parallel workload) for New-ConsoleLanes.

Use it inside the script block of New-ConsoleLanes when lanes differ: each has its own total, progress, status, result counts and detail. -Key names the lane for Update-ConsoleLane; the label defaults to the key and accepts rich text from New-ConsoleText. To add lanes to a widget made earlier, pipe it to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLane [-Key] <string> [-Label <Object>] [-Total <double>] [-Done <double>] [-Status <StatusKind>] [-Detail <Object>] [-Passed <int>] [-Failed <int>] [-Warned <int>] [-Skipped <int>] [-StartedSeconds <Double>] [-FinishedSeconds <Double>] [<CommonParameters>]
```

## DESCRIPTION
Creates one lane (a worker, server or item of a parallel workload) for New-ConsoleLanes.

Use it inside the script block of New-ConsoleLanes when lanes differ: each has its own total, progress, status, result counts and detail. -Key names the lane for Update-ConsoleLane; the label defaults to the key and accepts rich text from New-ConsoleText. To add lanes to a widget made earlier, pipe it to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLanes -Content {
    New-ConsoleLane 'DC01' -Total 40 -Done 40 -Status Pass -Passed 40
    New-ConsoleLane 'DC02' -Total 40 -Done 22 -Status Running -Passed 21 -Warned 1 -Detail 'replication'
    New-ConsoleLane 'DC03' -Total 40
} | Show-Console
```

Three servers at different points

## PARAMETERS

### -Detail
What the lane is doing now (shown literally; rich text keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Done
Steps done.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Failed
Results that failed.

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

### -FinishedSeconds
When the lane finished, measured from the start of the session. Together with -StartedSeconds, makes elapsed-time columns deterministic.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
The lane key, used by Update-ConsoleLane -Key.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The label (defaults to the key; rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Passed
Results that passed.

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

### -Skipped
Results skipped.

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

### -StartedSeconds
When the lane started, measured from the start of the session. Preserved when added to New-ConsoleLanes or Add-ConsoleItem.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The status (default Pending): Pass, Fail, Warn, Skip, Running or Pending, or PowerShell's words.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Total
Planned steps.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Warned
Results with warnings.

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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.Lane`

## RELATED LINKS

- None
