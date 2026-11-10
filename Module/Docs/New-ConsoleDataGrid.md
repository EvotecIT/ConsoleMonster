---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleDataGrid
## SYNOPSIS
Creates a named, embeddable data grid for Show-ConsoleApp.

## SYNTAX
### Grid (Default)
```powershell
New-ConsoleDataGrid [-Key] <string> [[-Label] <string>] -Grid <Grid> [-Height <int>] [-NoQuickSearch] [-LinkKey <string>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

### Objects
```powershell
New-ConsoleDataGrid [-Key] <string> [[-Label] <string>] -InputObject <psobject[]> [-Property <Object[]>] [-Height <int>] [-NoQuickSearch] [-LinkKey <string>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control. -InputObject lists ordinary objects with the same columns as Show-ConsoleGrid (-Property picks them); -Grid embeds a listing prepared in .NET.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp -Mode Snapshot -Content { New-ConsoleDataGrid -Key Inventory -Grid 'Inventory' -Height 18 }
```

Embed an empty listing in an application

### EXAMPLE 2
```powershell
Show-ConsoleApp -Content { New-ConsoleDataGrid -Key Services -InputObject (Get-Service) -Property Name, Status, StartType -Height 12 }
```

List services with three columns

## PARAMETERS

### -Disabled
Disables editing and removes the input from keyboard focus traversal.

```yaml
Type: SwitchParameter
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Grid
The listing's columns, original objects and row identities.

```yaml
Type: Grid
Parameter Sets: Grid
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Natural content height; the terminal viewport can constrain it further.

```yaml
Type: Int32
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: 20
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
The objects to list, one row each; the row is the object a selection returns.

```yaml
Type: PSObject[]
Parameter Sets: Objects
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state and final hashtable.

```yaml
Type: String
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
Visible label; defaults to the name using the core control's labels.

```yaml
Type: String
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LinkKey
The state key shared with linked controls (New-ConsoleChartView, New-ConsoleSelect, another grid): the selected row's position in the listing. A position it cannot show is ignored.

```yaml
Type: String
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoQuickSearch
Leaves printable keys to the app's command registry.

```yaml
Type: SwitchParameter
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnChange
Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.

```yaml
Type: ScriptBlock
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
Columns: names, wildcards and calculated properties with the Show-ConsoleTable column options.

```yaml
Type: Object[]
Parameter Sets: Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

```yaml
Type: Int32
Parameter Sets: Grid, Objects
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValidationMessage
Initial validation text displayed below the input.

```yaml
Type: String
Parameter Sets: Grid, Objects
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.DataGrid`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoledatagrid/](https://consolemonster.dev/powershell/commands/new-consoledatagrid/)
