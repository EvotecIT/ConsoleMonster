---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleStatusItem
## SYNOPSIS
Creates one status item, a step with a status and a detail, for New-ConsoleStatusList and New-ConsolePhases.

Use it inside the script block of New-ConsoleStatusList (a checklist) or New-ConsolePhases (a strip of stages). -Status is the shared status vocabulary: Pass, Fail, Warn, Skip, Info, Running or Pending, and PowerShell's words such as Success, Warning and Error. -Detail is a muted note on the right, such as a duration. The label is shown literally; rich text from New-ConsoleText keeps its colours. -Key names the item for live updates (Update-ConsoleStatusItem, Update-ConsolePhase) when it differs from the label. To add items to a list made earlier, pipe the list to Add-ConsoleItem.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleStatusItem [-Label] <Object> [[-Status] <StatusKind>] [-Detail <Object>] [-Key <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates one status item, a step with a status and a detail, for New-ConsoleStatusList and New-ConsolePhases.

Use it inside the script block of New-ConsoleStatusList (a checklist) or New-ConsolePhases (a strip of stages). -Status is the shared status vocabulary: Pass, Fail, Warn, Skip, Info, Running or Pending, and PowerShell's words such as Success, Warning and Error. -Detail is a muted note on the right, such as a duration. The label is shown literally; rich text from New-ConsoleText keeps its colours. -Key names the item for live updates (Update-ConsoleStatusItem, Update-ConsolePhase) when it differs from the label. To add items to a list made earlier, pipe the list to Add-ConsoleItem.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleStatusList -Content {
    New-ConsoleStatusItem 'Restore NuGet packages' -Status Pass -Detail '1.6 s'
    New-ConsoleStatusItem 'Run Pester on PowerShell 7' -Status Warn -Detail '1 skipped'
    New-ConsoleStatusItem 'Publish to PowerShell Gallery' -Status Pending
} | Show-Console
```

A build checklist

### EXAMPLE 2
```powershell
New-ConsolePhases -Content {
    New-ConsoleStatusItem 'Discover' -Status Pass -Detail '2.1s'
    New-ConsoleStatusItem 'Evaluate' -Status Running -Detail '64%'
    New-ConsoleStatusItem 'Report'
} | Show-Console
```

Phases of a run

## PARAMETERS

### -Detail
A muted detail on the right, for example a duration or a count.

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

### -Key
The key for live updates (Update-ConsoleStatusItem -Key, Update-ConsolePhase -Key); defaults to the label's text.

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

### -Label
The label (shown literally; rich text from New-ConsoleText keeps its colours).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
The status (default Pending): Pass, Fail, Warn, Skip, Info, Running or Pending, or PowerShell's words such as Success, Warning and Error.

```yaml
Type: StatusKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info

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

- `ConsoleMonster.Widgets.StatusItem`

## RELATED LINKS

- None
