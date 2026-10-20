---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-Console
## SYNOPSIS
Shows pipeline input in the view that fits it best.

One object becomes an inspector; objects of one shape a table (cards when the window is narrow), using their DefaultDisplayPropertySet and presenters by type (bytes for Length and Size, dates, durations, Yes/No, status markers for enums such as Running/Stopped); hierarchical objects (a child collection of the same type, or -ChildProperty) a tree; a numeric series a bar chart; strings highlighted code when a language is detected, otherwise text. -View overrides the choice; Grid opens the interactive Show-ConsoleGrid and returns what you pick.

## SYNTAX
### __AllParameterSets
```powershell
Show-Console [[-InputObject] <psobject>] [-View <ConsoleViewKind>] [-Property <Object[]>] [-ChildProperty <string>] [-Chart] [-Title <Object>] [-Width <int>] [-Depth <int>] [-AsString] [<CommonParameters>]
```

## DESCRIPTION
Shows pipeline input in the view that fits it best.

One object becomes an inspector; objects of one shape a table (cards when the window is narrow), using their DefaultDisplayPropertySet and presenters by type (bytes for Length and Size, dates, durations, Yes/No, status markers for enums such as Running/Stopped); hierarchical objects (a child collection of the same type, or -ChildProperty) a tree; a numeric series a bar chart; strings highlighted code when a language is detected, otherwise text. -View overrides the choice; Grid opens the interactive Show-ConsoleGrid and returns what you pick.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-ChildItem | Show-Console
```

Let it choose

### EXAMPLE 2
```powershell
$tree | Show-Console -ChildProperty Children
```

A tree of folders by their children

## PARAMETERS

### -AsString
Writes the view as one string of plain text instead of drawing it, to keep it in a variable or a file from any script, function or PowerShell version. Without it, Show-Console behaves like the Format-Console commands: PowerShell 7.2+ captures a view typed at the prompt ($x = ..., > file, Out-File), piped into another command the view goes on as plain lines, and inside scripts and functions or on Windows PowerShell 5.1 it is drawn on the screen.

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

### -Chart
Shows objects as a bar chart (label from the first text property, value from the first number).

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

### -ChildProperty
The property that holds each object's children, for a tree.

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

### -Depth
The deepest level an inspector or tree shows (default 3).

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

### -InputObject
The objects to show.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Property
The properties for a table, as in Format-ConsoleTable (names, wildcards, calculated properties).

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

### -Title
A title (markup).

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

### -View
The view instead of the automatic choice: Table, Cards, Tree, Inspector, Chart, Code, Text or Grid.

```yaml
Type: ConsoleViewKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Empty, Text, Code, Inspector, Table, Cards, Tree, Chart, Grid

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
The width in cells (default the window).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `None`

## RELATED LINKS

- None
