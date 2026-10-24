---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleTable
## SYNOPSIS
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## SYNTAX
### Objects (Default)
```powershell
New-ConsoleTable [[-InputObject] <psobject[]>] [-Property <Object[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [<CommonParameters>]
```

### Bind
```powershell
New-ConsoleTable -Bind <scriptblock> [-Property <Object[]>] [-StatusProperty <string>] [-Title <Object>] [-Caption <Object>] [-Border <TableBorder>] [-NoHeaderSeparator] [-Expand] [<CommonParameters>]
```

## DESCRIPTION
Creates a console table from pipeline objects.

Columns come from -Property or from the first object's properties (or hashtable keys). Columns size to their content, numbers are right-aligned, long values wrap. A -StatusProperty column shows status markers (pass, fail, warn...) as the row's one colour signal.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | New-ConsoleTable -Title 'Services' | Show-Console
```

Services as a table with a status column

### EXAMPLE 2
```powershell
Show-ConsoleApp -Content { New-ConsoleTextInput -Key Query; New-ConsoleTable -Bind { $items | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status -StatusProperty Status }
```

Filter objects from current application state.

## PARAMETERS

### -Bind
Application-dependent object producer. Receives param($State) and injected $State as a native hashtable on the owning input thread. Emit objects, not widgets; columns and formatting use the same builder as ordinary tables.

```yaml
Type: ScriptBlock
Parameter Sets: Bind
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Border
Border: Rounded, Square, Double, Minimal or None.

```yaml
Type: TableBorder
Parameter Sets: Objects, Bind
Aliases: None
Possible values: Rounded, Square, Double, Minimal, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Caption
A muted caption below the table.

```yaml
Type: Object
Parameter Sets: Objects, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Expand
Fill the available width (spare width goes to the widest text column).

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The objects to show, one row each.

```yaml
Type: PSObject[]
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -NoHeaderSeparator
Underline the header instead of drawing a separator row under it.

```yaml
Type: SwitchParameter
Parameter Sets: Objects, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
Properties to show, in order.

```yaml
Type: Object[]
Parameter Sets: Objects, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StatusProperty
A property whose values are statuses (pass/fail/warn/skip/running).

```yaml
Type: String
Parameter Sets: Objects, Bind
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title above the table.

```yaml
Type: Object
Parameter Sets: Objects, Bind
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

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `ConsoleMonster.Widgets.Table`
- `ConsoleMonster.Interactive.AppBinding`

## RELATED LINKS

- None
