---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleStatusList
## SYNOPSIS
Creates a checklist of items with status markers.

-Data maps item labels to statuses (pass, fail, warn, skip, running, pending, info). Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. Update items live with Update-ConsoleStatus.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleStatusList [[-Data] <IDictionary>] [-Item <Object[]>] [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a checklist of items with status markers.

-Data maps item labels to statuses (pass, fail, warn, skip, running, pending, info). Status is always shown with a symbol (or a [PASS]-style word in ASCII mode), never by colour alone. Update items live with Update-ConsoleStatus.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleStatusList -Name 'Name'
```


## PARAMETERS

### -Data
Labels mapped to statuses.

```yaml
Type: IDictionary
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
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

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.StatusList`

## RELATED LINKS

- None
