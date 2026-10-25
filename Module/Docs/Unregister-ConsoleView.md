---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Unregister-ConsoleView
## SYNOPSIS
Removes registered ConsoleMonster views and value shapers.

Names the registered types directly or reads their names from a validated data-only .consoleviews.psd1 file. Does not remove or execute the file.

## SYNTAX
### Type (Default)
```powershell
Unregister-ConsoleView [-TypeName] <string[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

### File
```powershell
Unregister-ConsoleView -Path <string[]> [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
Removes registered ConsoleMonster views and value shapers.

Names the registered types directly or reads their names from a validated data-only .consoleviews.psd1 file. Does not remove or execute the file.

## EXAMPLES

### EXAMPLE 1
```powershell
Unregister-ConsoleView -TypeName 'Contoso.Server'
```

Remove a custom result view.

## PARAMETERS

### -Path
Data-only view files listing the type registrations to remove.

```yaml
Type: String[]
Parameter Sets: File
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TypeName
Type names whose registrations should be removed.

```yaml
Type: String[]
Parameter Sets: Type
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

- `None`

## RELATED LINKS

- None
