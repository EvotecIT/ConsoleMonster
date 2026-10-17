---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Set-ConsoleRegion
## SYNOPSIS
Replaces the content of a region (New-ConsoleRegion) in the running dashboard.

The new widget is shown from the next frame on; the surrounding frame stays, and live widgets inside the new content (lanes, logs, live tables) can be updated by name right away. Use it from the dashboard script, like the Update-Console* commands.

## SYNTAX
### Content (Default)
```powershell
Set-ConsoleRegion [-Name] <string> [[-Content] <scriptblock>] [-Widget <IWidget>] [-Session <LiveSession>] [<CommonParameters>]
```

### Clear
```powershell
Set-ConsoleRegion [-Name] <string> -Clear [-Session <LiveSession>] [<CommonParameters>]
```

## DESCRIPTION
Replaces the content of a region (New-ConsoleRegion) in the running dashboard.

The new widget is shown from the next frame on; the surrounding frame stays, and live widgets inside the new content (lanes, logs, live tables) can be updated by name right away. Use it from the dashboard script, like the Update-Console* commands.

## EXAMPLES

### EXAMPLE 1
```powershell
Set-ConsoleRegion -Name 'Name'
```


### EXAMPLE 2
```powershell
Set-ConsoleRegion -Name 'Name' -Clear
```


## PARAMETERS

### -Clear
Empties the region.

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

### -Content
A script block whose widgets become the content.

```yaml
Type: ScriptBlock
Parameter Sets: Content
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The region name.

```yaml
Type: String
Parameter Sets: Content, Clear
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Session
The live session to update. Defaults to $ConsoleSession or the running dashboard.

```yaml
Type: LiveSession
Parameter Sets: Content, Clear
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Widget
The widget that becomes the content.

```yaml
Type: IWidget
Parameter Sets: Content
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `ConsoleMonster.Layout.IWidget`

## OUTPUTS

- `None`

## RELATED LINKS

- None
