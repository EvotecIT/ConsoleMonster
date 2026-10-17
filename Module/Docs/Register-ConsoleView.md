---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Register-ConsoleView
## SYNOPSIS
Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Format-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). -Remove deletes a registration. Modules can register their own types when they load.

## SYNTAX
### Register (Default)
```powershell
Register-ConsoleView [-TypeName] <string[]> [-View <ConsoleViewKind>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <scriptblock>] [-Group <string>] [<CommonParameters>]
```

### Remove
```powershell
Register-ConsoleView [-TypeName] <string[]> -Remove [<CommonParameters>]
```

## DESCRIPTION
Registers how a type is shown by Enable-ConsoleFormatting and Out-ConsoleDefault.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Format-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). -Remove deletes a registration. Modules can register their own types when they load.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-ConsoleView -TypeName @('Name')
```


### EXAMPLE 2
```powershell
Register-ConsoleView -TypeName @('Name') -Remove
```


## PARAMETERS

### -Group
A group name: consecutive objects of types in the same group are shown in one view.

```yaml
Type: String
Parameter Sets: Register
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
A property name or script block that groups rows under headings.

```yaml
Type: Object
Parameter Sets: Register
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
The properties, as in Format-ConsoleTable.

```yaml
Type: Object[]
Parameter Sets: Register
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Remove
Removes the registrations instead.

```yaml
Type: SwitchParameter
Parameter Sets: Remove
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A script block for each object's title ($_ is the object): a shared title heads the view, different titles group the rows.

```yaml
Type: ScriptBlock
Parameter Sets: Register
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TypeName
The type names (as in PSObject.TypeNames).

```yaml
Type: String[]
Parameter Sets: Register, Remove
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -View
The view: Table (default), Cards, Tree, Inspector or Chart.

```yaml
Type: ConsoleViewKind
Parameter Sets: Register
Aliases: None
Possible values: Empty, Text, Code, Inspector, Table, Cards, Tree, Chart, Grid

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
