---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Send-ConsoleEvent
## SYNOPSIS
Sends a raw live event to any widget of a running dashboard.

The general form behind Update-Console, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

## SYNTAX
### __AllParameterSets
```powershell
Send-ConsoleEvent [-WidgetId] <string> [-Operation] <string> [-Key <string>] [-Text <string>] [-Status <string>] [-Value <double>] [-Total <double>] [-TimeMilliseconds <long>] [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Sends a raw live event to any widget of a running dashboard.

The general form behind Update-Console, for custom widgets and less common operations (for example the dashboard's title or status). Operations are defined by each widget.

## EXAMPLES

### EXAMPLE 1
```powershell
Send-ConsoleEvent -WidgetId dashboard -Operation status -Text 'deploying wave 2' -Status running
```

Change the dashboard status in the header

## PARAMETERS

### -Key
The item key.

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

### -Operation
The operation (advance, state, set, add, write, title, status...).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
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
A state word.

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

### -Text
Text.

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

### -TimeMilliseconds
Milliseconds since the run began. Zero lets the session stamp the event; supply the original time when forwarding timestamped updates.

```yaml
Type: Int64
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Total
A second numeric value (total, maximum).

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Value
A numeric value.

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
The widget name.

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

- `None`

## RELATED LINKS

- None
