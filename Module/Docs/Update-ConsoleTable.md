---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleTable
## SYNOPSIS
Adds, updates, removes or clears rows of a live table (New-ConsoleLiveTable) in the running dashboard.

-Key identifies the row: a new key adds a row, an existing key replaces its values. -Remove deletes the row and -Clear deletes them all; the column widths stay.

## SYNTAX
### Row (Default)
```powershell
Update-ConsoleTable [-Key] <string> [[-Value] <Object[]>] [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

### Remove
```powershell
Update-ConsoleTable [-Key] <string> -Remove [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

### Clear
```powershell
Update-ConsoleTable -Clear [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Adds, updates, removes or clears rows of a live table (New-ConsoleLiveTable) in the running dashboard.

-Key identifies the row: a new key adds a row, an existing key replaces its values. -Remove deletes the row and -Clear deletes them all; the column widths stay.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleTable -Name jobs -Key $job.Id -Value $job.Name, 'Running', $elapsed
```

Track a job from start to finish

## PARAMETERS

### -Clear
Removes every row.

```yaml
Type: SwitchParameter
Parameter Sets: Clear
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
The stable row key.

```yaml
Type: String
Parameter Sets: Row, Remove
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The live table name (default table).

```yaml
Type: String
Parameter Sets: Row, Remove, Clear
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Remove
Removes the row with that key.

```yaml
Type: SwitchParameter
Parameter Sets: Remove
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: Row, Remove, Clear
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
The values, one per column (numbers, dates, durations and statuses go through the column presenters).

```yaml
Type: Object[]
Parameter Sets: Row
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
