---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleDashboard
## SYNOPSIS
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Invoke-ConsoleDashboard to run it live, or to Show-Console to print it once.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDashboard [[-WidgetId] <string>] [-Content] <scriptblock> -Title <Object> [-Subtitle <Object>] [-Footer <Object>] [-FooterHints <Object[]>] [-Gap <int>] [-NoElapsed] [<CommonParameters>]
```

## DESCRIPTION
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Invoke-ConsoleDashboard to run it live, or to Show-Console to print it once.

## EXAMPLES

### EXAMPLE 1
```powershell
$d = New-ConsoleDashboard -Title 'Deploy' -Content { New-ConsolePanel -Title 'Servers' -Content { New-ConsoleLanes -WidgetId servers }; New-ConsolePanel -Title 'Log' -Fill -Content { New-ConsoleLogRegion } }
```

A dashboard with lanes and a log that fills the rest of the screen

## PARAMETERS

### -Content
Scriptblock that emits the body widgets.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Footer
Footer text; defaults to a Ctrl+C hint.

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

### -FooterHints
Key/label dictionaries or typed KeyHint values; replaces the default Ctrl+C hint. Used when Footer is unset.

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

### -Gap
Blank rows between the body widgets.

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

### -NoElapsed
Hides the default elapsed clock in the dashboard header.

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

### -Subtitle
A subtitle shown after the title.

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

### -Title
The title shown in the header bar (markup allowed).

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WidgetId
Live name of the dashboard (for Send-ConsoleEvent title/status updates).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `ConsoleMonster.Widgets.Dashboard`

## RELATED LINKS

- None
