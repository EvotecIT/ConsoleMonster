---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Use-ConsoleProgress
## SYNOPSIS
Runs a script and shows its Write-Progress calls as an inline progress area.

The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. Works on Windows PowerShell 5.1 and PowerShell 7.

## SYNTAX
### __AllParameterSets
```powershell
Use-ConsoleProgress [-ScriptBlock] <scriptblock> [-Keep] [-FramesPerSecond <int>] [<CommonParameters>]
```

## DESCRIPTION
Runs a script and shows its Write-Progress calls as an inline progress area.

The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the area disappears (or stays with -Keep) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. In jobs, remoting, the ISE and redirected output the script simply runs. Works on Windows PowerShell 5.1 and PowerShell 7.

## EXAMPLES

### EXAMPLE 1
```powershell
Use-ConsoleProgress -ScriptBlock { }
```


## PARAMETERS

### -FramesPerSecond
Redraw rate (default 15).

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

### -Keep
Leaves the final state of the progress area in the scrollback.

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

### -ScriptBlock
The script to run.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
