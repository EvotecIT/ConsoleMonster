---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleRunResult
## SYNOPSIS
Creates the result a menu run returns: a title, a status, a summary, figures and next actions.

Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -Status takes the shared status vocabulary (default Pass).

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRunResult [-Title] <string> [-Status <StatusKind>] [-Summary <string>] [-Figure <IDictionary>] [-Next <IDictionary>] [-Details <IWidget>] [<CommonParameters>]
```

## DESCRIPTION
Creates the result a menu run returns: a title, a status, a summary, figures and next actions.

Return it from the script block of $MenuContext.Run (a live dashboard started from a menu action): when the run ends, the menu shows it as a result screen with the figures as large values and the next actions as items. -Figure maps labels to values (use [ordered] to keep order); -Next maps action titles to script blocks, which run with $MenuContext like any menu action. -Status takes the shared status vocabulary (default Pass).

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleMenuItem 'Back up now' -Action {
    $dashboard = New-ConsoleDashboard -Title 'Backup' { New-ConsoleStatusList -Item 'Scan files', 'Copy changes' }
    $null = $MenuContext.Run($dashboard, {
        Update-ConsoleStatus 'Scan files' -Status Pass
        Update-ConsoleStatus 'Copy changes' -Status Pass
        New-ConsoleRunResult 'Backup finished' -Summary '1 284 files copied, 312 MB.' -Figure ([ordered] @{ files = '1 284'; copied = '312 MB' }) -Next @{
            'Show where the log is' = { $MenuContext.Toast('Log: D:\Backups\backup.log') }
        }
    })
}
```

A backup run that reports what it did

## PARAMETERS

### -Details
A widget with details under the figures (for example a table of what changed).

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Figure
Figures: labels mapped to values, shown large (use [ordered] to keep order).

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Next
Next actions: titles mapped to script blocks, run with $MenuContext like menu actions.

```yaml
Type: IDictionary
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
The outcome (default Pass): Pass, Fail, Warn or Skip, or PowerShell's words such as Success and Error.

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

### -Summary
One or two sentences under the title.

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

### -Title
The result title.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.RunResult`

## RELATED LINKS

- None
