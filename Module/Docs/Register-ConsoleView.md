---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Register-ConsoleView
## SYNOPSIS
Registers how a type is shown by Enable-ConsoleFormatting.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Show-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). Unregister-ConsoleView removes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.

## SYNTAX
### Register (Default)
```powershell
Register-ConsoleView [-TypeName] <string[]> [-View <ConsoleViewKind>] [-Property <Object[]>] [-GroupBy <Object>] [-Title <scriptblock>] [-Group <string>] [-Shaper <scriptblock>] [<CommonParameters>]
```

### File
```powershell
Register-ConsoleView [-Path] <string[]> [<CommonParameters>]
```

## DESCRIPTION
Registers how a type is shown by Enable-ConsoleFormatting.

Objects whose type names include -TypeName are shown in the given view when they reach the console. -Property picks the columns as in Show-ConsoleTable (names, calculated properties with Presenter options), -GroupBy groups rows under headings, and types with the same -Group are shown together (files and folders). Unregister-ConsoleView removes a registration. Modules can register their own types when they load, or ship a data-only ModuleName.consoleviews.psd1 file without a dependency on ConsoleMonster. Use -Path to import a view file explicitly.

## EXAMPLES

### EXAMPLE 1
```powershell
Register-ConsoleView -TypeName 'Contoso.Server' -View Table -Property Name, @{ Name = 'State'; Expression = { $_.State }; Presenter = 'StatusWord' }, Uptime
```

Show a module's result objects as a table

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

### -Path
Imports a data-only .consoleviews.psd1 file. Modules can ship a same-named file beside their manifest without depending on ConsoleMonster.

```yaml
Type: String[]
Parameter Sets: File
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Property
The properties, as in Show-ConsoleTable.

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

### -Shaper
A script block that turns each object of the type into what is shown instead ($_ is the object), for example an
ordered dictionary of the fields worth showing. It applies in every command (Show-ConsoleTable, -List, -Tree,
Show-Console, Show-ConsoleGrid, Compare-ConsoleObject, Out-ConsoleDefault), also to deserialized objects whose type
names match. Given alone (without -View, -Property, -GroupBy, -Title or -Group), only the shaper is registered.

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
Parameter Sets: Register
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
Possible values: Empty, Text, Code, Inspector, Table, Cards, Tree, Chart, Grid, Auto, List, Json, Error, Markdown, Rule

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
