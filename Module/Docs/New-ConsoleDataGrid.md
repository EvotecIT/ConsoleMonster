---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleDataGrid
## SYNOPSIS
Creates a named, embeddable data grid for Show-ConsoleApp.

The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDataGrid [-Key] <string> [[-Label] <string>] -Grid <Grid> [-Height <int>] [-NoQuickSearch] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a named, embeddable data grid for Show-ConsoleApp.

The listing retains original objects and stable row identities. Each grid has its own cursor, marks and prefix search. Rows and lazy cells are rendered through the shared DataGrid control.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleDataGrid -Key Left -Grid $listing -Height 18
```

Embed an existing listing.

## PARAMETERS

### -Disabled
Disables editing and removes the input from keyboard focus traversal.

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

### -Grid
The listing's columns, original objects and row identities.

```yaml
Type: Grid
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
Unique key in the application's state and final hashtable.

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

### -Label
Visible label; defaults to the name using the core control's labels.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoQuickSearch
Leaves printable keys to the app's command registry.

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

### -OnChange
Runs once per committed edit on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. State is a hashtable; add or assign keys to write back. Output is discarded. Retains closure variables.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValidationMessage
Initial validation text displayed below the input.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.DataGrid`

## RELATED LINKS

- None
