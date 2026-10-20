---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleStatusList
## SYNOPSIS
Creates a checklist of items with status markers.

Items come from a script block of New-ConsoleStatusItem calls (each with its own status and detail), from the pipeline, from -Data (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-ConsoleStatus.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleStatusList [[-Data] <Object>] [[-Content] <scriptblock>] [-InputObject <psobject>] [-Item <Object[]>] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a checklist of items with status markers.

Items come from a script block of New-ConsoleStatusItem calls (each with its own status and detail), from the pipeline, from -Data (labels mapped to statuses) or from -Item (labels added as pending, for a live checklist). Statuses use the shared vocabulary: Pass, Fail, Warn, Skip, Info, Running, Pending and PowerShell's words such as Success, Warning and Error. Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. To add items later, pipe the list to Add-ConsoleItem; in a live dashboard, update items with Update-ConsoleStatus.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleStatusList {
    New-ConsoleStatusItem 'Restore NuGet packages' -Status Pass -Detail '1.6 s'
    New-ConsoleStatusItem 'Compile ConsoleMonster' -Status Pass -Detail '4.7 s'
    New-ConsoleStatusItem 'Run Pester on PowerShell 7' -Status Warn -Detail '1 skipped'
    New-ConsoleStatusItem 'Pack ConsoleMonster.2.0.0.nupkg' -Status Running
} | Write-ConsoleWidget
```

A build checklist

### EXAMPLE 2
```powershell
New-ConsoleStatusList ([ordered] @{ 'SPF valid' = 'pass'; 'DMARC enforced' = 'pass'; 'MTA-STS' = 'warn' }) | Write-ConsoleWidget
```

Mail security checks from a hashtable

### EXAMPLE 3
```powershell
$checks | ForEach-Object { New-ConsoleStatusItem $_.Name -Status $_.Result -Detail $_.Duration } | New-ConsoleStatusList | Write-ConsoleWidget
```

Items from objects in the pipeline

### EXAMPLE 4
```powershell
$steps = New-ConsoleStatusList -Item 'Scan files', 'Copy changes', 'Verify'
Start-ConsoleDashboard (New-ConsoleDashboard -Title 'Backup' { $steps }) { Update-ConsoleStatus 'Scan files' -Status Pass -Detail '2.4 s' }
```

A live checklist: items start pending and are updated from the dashboard script

## PARAMETERS

### -Content
A script block that emits the items: New-ConsoleStatusItem calls or hashtables with Label, Status and Detail.

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

### -Data
Labels mapped to statuses (a script block here is taken as -Content).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Items from the pipeline: New-ConsoleStatusItem objects or hashtables with Label, Status and Detail.

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

### -Item
Item keys to add as pending (for a live checklist).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The live name (default status).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.StatusList`

## RELATED LINKS

- None
