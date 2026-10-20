---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Update-ConsoleActivity
## SYNOPSIS
Starts, updates or finishes an activity of an activity list (New-ConsoleActivityList) in the running dashboard.

-Id identifies the activity: a new id adds a row, an existing one updates it, and -Complete removes it (with the activities under it). -Activity is the title, -Status a short text after it, -Percent the share done (leave it out for work of unknown length, which spins) and -SecondsRemaining an estimate shown as time left. -ParentId indents the activity under another one. The time each activity has taken follows the session clock, so a simulated clock (New-ConsoleClock) drives it too. Safe to call from any runspace or thread.

## SYNTAX
### Update (Default)
```powershell
Update-ConsoleActivity [-Id] <int> [[-Activity] <Object>] [-Status <string>] [-Percent <double>] [-SecondsRemaining <double>] [-ParentId <int>] [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

### Complete
```powershell
Update-ConsoleActivity [-Id] <int> -Complete [-Name <string>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Starts, updates or finishes an activity of an activity list (New-ConsoleActivityList) in the running dashboard.

-Id identifies the activity: a new id adds a row, an existing one updates it, and -Complete removes it (with the activities under it). -Activity is the title, -Status a short text after it, -Percent the share done (leave it out for work of unknown length, which spins) and -SecondsRemaining an estimate shown as time left. -ParentId indents the activity under another one. The time each activity has taken follows the session clock, so a simulated clock (New-ConsoleClock) drives it too. Safe to call from any runspace or thread.

## EXAMPLES

### EXAMPLE 1
```powershell
Update-ConsoleActivity -Name running -Id 7 -Activity 'SRV01 · Cert chain'
Update-ConsoleActivity -Name running -Id 7 -Complete
```

Show a check while it runs, then remove it

### EXAMPLE 2
```powershell
1..4 | ForEach-Object { Update-ConsoleActivity -Name running -Id 9000 -Activity 'Writing fleet.html' -Percent ($_ * 25) }
```

A report written in steps, with a bar

## PARAMETERS

### -Activity
The title.

```yaml
Type: Object
Parameter Sets: Update
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Complete
Finishes the activity: its row (and the rows under it) disappear.

```yaml
Type: SwitchParameter
Parameter Sets: Complete
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
The activity id (a new id adds a row).

```yaml
Type: Int32
Parameter Sets: Update, Complete
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The activity list name (default $progress).

```yaml
Type: String
Parameter Sets: Update, Complete
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParentId
The id of the activity this one is indented under.

```yaml
Type: Int32
Parameter Sets: Update
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Percent
The share done, 0 to 100; leave it out for work of unknown length (it spins).

```yaml
Type: Double
Parameter Sets: Update
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SecondsRemaining
An estimate of the seconds left, shown as time left.

```yaml
Type: Double
Parameter Sets: Update
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: Update, Complete
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
A short text after the title ("retrying", "3 of 8").

```yaml
Type: String
Parameter Sets: Update
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

- `None`

## RELATED LINKS

- None
