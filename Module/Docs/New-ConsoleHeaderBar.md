---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleHeaderBar
## SYNOPSIS
Creates a header with a title, breadcrumbs, detail and optional elapsed time.

Returns a reusable HeaderBar widget without opening a session. Status is a semantic outcome; Detail is the text beside it. HeaderRight supplies independent right-aligned content.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleHeaderBar [-Title] <Object> [-Subtitle <Object>] [-Crumb <string[]>] [-Detail <Object>] [-Status <StatusKind>] [-HeaderRight <Object>] [-ShowElapsed] [-ElapsedSeconds <Double>] [<CommonParameters>]
```

## DESCRIPTION
Creates a header with a title, breadcrumbs, detail and optional elapsed time.

Returns a reusable HeaderBar widget without opening a session. Status is a semantic outcome; Detail is the text beside it. HeaderRight supplies independent right-aligned content.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleHeaderBar 'Build' -Subtitle 'Release' -Status Pass -Detail 'Ready' | Show-Console
```

Show a compact build header.

## PARAMETERS

### -Crumb
Breadcrumbs shown before the title.

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
Current descriptive text.

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

### -ElapsedSeconds
An explicit elapsed duration; otherwise the rendering session supplies it.

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

### -HeaderRight
Independent text at the right of the header.

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

### -ShowElapsed
Shows elapsed time in the header.

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

### -Status
Semantic outcome displayed before the detail.

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

### -Subtitle
Text qualifying the title.

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
The header's title.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.HeaderBar`

## RELATED LINKS

- None
