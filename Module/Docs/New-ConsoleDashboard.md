---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleDashboard
## SYNOPSIS
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Start-ConsoleDashboard to run it live, or to Write-ConsoleWidget to print it once.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDashboard [-Content] <scriptblock> -Title <Object> [-Subtitle <Object>] [-Footer <Object>] [-Name <string>] [-Gap <int>] [<CommonParameters>]
```

## DESCRIPTION
Defines a live console dashboard: a header with title and clock, the body and a footer.

The scriptblock emits the body widgets top to bottom. Pass the result to Start-ConsoleDashboard to run it live, or to Write-ConsoleWidget to print it once.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDashboard -Content { } -Title 'Value'
```


## PARAMETERS

### -Content
Scriptblock that emits the body widgets.

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

### -Name
Live name of the dashboard (for Send-ConsoleEvent title/status updates).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.Dashboard`

## RELATED LINKS

- None
