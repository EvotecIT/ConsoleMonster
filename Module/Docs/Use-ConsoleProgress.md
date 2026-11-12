---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/use-consoleprogress/
schema: 2.0.0
---
# Use-ConsoleProgress
## SYNOPSIS
Runs a script and shows its Write-Progress calls as an inline progress area.

## SYNTAX
### __AllParameterSets
```powershell
Use-ConsoleProgress [-ScriptBlock] <scriptblock> [-PrintFinalState] [-FramesPerSecond <double>] [-NoLive] [-ReducedMotion] [<CommonParameters>]
```

## DESCRIPTION
The script runs on the pipeline thread with the caller's variables. While it runs, Write-Progress (Activity, Status, CurrentOperation, PercentComplete, SecondsRemaining, Id and ParentId nesting, -Completed) is drawn as nested activities with bars, below the cursor and without the alternate screen; Write-Host and console output scroll above the area. When the script ends the final state stays (or disappears with -PrintFinalState:$false) and the script's output is returned. Calls the script makes directly are covered; commands inside other modules call their own Write-Progress, which PowerShell shows as usual. Inside the script, $ConsoleSession is this progress area's session, so Wait-ConsoleDashboard can capture or pause it. In jobs, remoting, the ISE and redirected output the script simply runs with $ConsoleSession set to null. If another runspace owns the terminal's progress area, the script also runs with PowerShell's native progress. Works on Windows PowerShell 5.1 and PowerShell 7.

## EXAMPLES

### EXAMPLE 1
```powershell
Use-ConsoleProgress {
    foreach ($i in 1..50) {
        Write-Progress -Activity 'Copying' -Status "File $i of 50" -PercentComplete ($i * 2)
        Start-Sleep -Milliseconds 50
    }
}
```

An existing loop, unchanged

### EXAMPLE 2
```powershell
Use-ConsoleProgress -PrintFinalState:$false {
                foreach ($server in 'web01', 'web02') {
                    Write-Progress -Activity 'Inspect servers' -Status $server -PercentComplete 50
                    [pscustomobject]@{ Server = $server; Result = 'Pass' }
                }
            } | Show-ConsoleTable
```

Return server results and suppress the final progress area

## PARAMETERS

### -FramesPerSecond
Redraw rate (default 15).

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 15
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoLive
Disables live redraws and uses append-only output.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrintFinalState
Prints the final progress state in scrollback (default true); pass -PrintFinalState:$false to leave nothing behind.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $true
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReducedMotion
Disables animation and limits live redraws to once a second without changing the layout. Accessible mode always enables reduced motion.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScriptBlock
The work to run while native Write-Progress calls update the inline progress area.

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

- [https://consolemonster.dev/powershell/commands/use-consoleprogress/](https://consolemonster.dev/powershell/commands/use-consoleprogress/)
