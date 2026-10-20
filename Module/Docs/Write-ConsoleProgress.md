---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleProgress
## SYNOPSIS
Writes a progress update, drawn by ConsoleMonster when Enable-ConsoleFormatting -Feature Progress is on.

Takes the same parameters as Write-Progress. With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias of this command for the session, so scripts and functions that call Write-Progress get ConsoleMonster's progress area: one row per activity below the cursor (nested with -ParentId), with a bar, the percentage, the status and the elapsed time. Output and errors written in between appear above the area, warnings and Write-Host lines too, and the area disappears when every activity completes or the command ends. $ProgressPreference and -ProgressAction are honoured. Without the feature, in redirected output, jobs, remoting and other hosts, it writes PowerShell's own progress record. Progress written by compiled cmdlets (Copy-Item, Invoke-WebRequest) is still shown by PowerShell.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleProgress [[-Activity] <string>] [[-Status] <string>] [[-Id] <int>] [-PercentComplete <int>] [-SecondsRemaining <int>] [-CurrentOperation <string>] [-ParentId <int>] [-Completed] [-SourceId <int>] [<CommonParameters>]
```

## DESCRIPTION
Writes a progress update, drawn by ConsoleMonster when Enable-ConsoleFormatting -Feature Progress is on.

Takes the same parameters as Write-Progress. With Enable-ConsoleFormatting -Feature Progress, Write-Progress is an alias of this command for the session, so scripts and functions that call Write-Progress get ConsoleMonster's progress area: one row per activity below the cursor (nested with -ParentId), with a bar, the percentage, the status and the elapsed time. Output and errors written in between appear above the area, warnings and Write-Host lines too, and the area disappears when every activity completes or the command ends. $ProgressPreference and -ProgressAction are honoured. Without the feature, in redirected output, jobs, remoting and other hosts, it writes PowerShell's own progress record. Progress written by compiled cmdlets (Copy-Item, Invoke-WebRequest) is still shown by PowerShell.

## EXAMPLES

### EXAMPLE 1
```powershell
foreach ($i in 1..50) { Write-Progress -Activity 'Copying' -Status "File $i of 50" -PercentComplete ($i * 2); Start-Sleep -Milliseconds 50 }
```

An existing loop, after Enable-ConsoleFormatting -Feature Progress

## PARAMETERS

### -Activity
The activity (the row's title).

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

### -Completed
The activity is done: its row (and its children) disappear.

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

### -CurrentOperation
The current operation, shown after the status.

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

### -Id
The activity's id; different ids are separate rows.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParentId
The parent activity's id: the row is nested under it.

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

### -PercentComplete
The percentage done (0-100), or -1 when it is not known.

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

### -SecondsRemaining
The seconds left, or -1 when it is not known.

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

### -SourceId
Accepted for compatibility with Write-Progress and ignored.

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

### -Status
The status shown after the bar.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
