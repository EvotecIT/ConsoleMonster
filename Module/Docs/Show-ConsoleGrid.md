---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleGrid
## SYNOPSIS
Shows pipeline objects in an interactive grid and returns the rows you choose.

An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Show-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list. Without an interactive user (redirected output, a job, CI) the rows are printed as a table with a warning and nothing is returned; -Mode None fails with an error instead.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleGrid [[-Property] <Object[]>] [-InputObject <psobject>] [-Title <string>] [-OutputMode <GridSelection>] [-Filter <string>] [-SortBy <string>] [-Descending] [-Hide <string[]>] [-Command <Object[]>] [-RefreshRow <scriptblock>] [-Culture <cultureinfo>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Shows pipeline objects in an interactive grid and returns the rows you choose.

An Out-GridView for the console, on Windows PowerShell 5.1 and PowerShell 7. Typing filters the rows (every word must appear in a shown column); Up/Down, PgUp/PgDn and Ctrl+Home/Ctrl+End move; F3 sorts by the next column (Shift+F3 the previous one) and F4 reverses the order; Space marks rows (Ctrl+A marks all shown rows); Enter shows the focused row with every column; F2 chooses the columns; Ctrl+Enter (or F10) returns the marked rows, or the focused row when none is marked; Esc clears the filter, then cancels. Columns come from -Property with the same options as Show-ConsoleTable (presenters, priorities, widths, tone rules); the original objects are returned. The screen has the same header bar, panel and footer as menus and forms. In line mode the rows are a numbered list. Without an interactive user (redirected output, a job, CI) the rows are printed as a table with a warning and nothing is returned; -Mode None fails with an error instead.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Show-ConsoleGrid -Property Name, Status, StartType -Title 'Services' -Command @{ Restart = { param($Row) Restart-Service $Row.Name -ErrorAction Stop; "Restarted $($Row.Name)" } }
```

Restart selected services with confirmation and embedded output

### EXAMPLE 2
```powershell
Get-Process | Show-ConsoleGrid -Property Name, Id, @{ Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'Bytes' } -SortBy Memory -Descending -OutputMode None
```

Browse processes sorted by memory

## PARAMETERS

### -Command
In-app commands or a hashtable of action keys and script blocks. Use New-ConsoleCommand for shortcuts, optional confirmation and explicit arguments/modules. Ctrl+K opens actions; F6 cancels work.

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

### -Culture
The culture for numbers and dates (default CONSOLEMONSTER_CULTURE, then the current culture).

```yaml
Type: CultureInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Descending
Sorts descending at the start.

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

### -Filter
The filter to start with.

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

### -Hide
Columns hidden at the start (F2 shows them again).

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

### -InputObject
The objects to show (pipeline input).

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Mode
Forces how to ask: Rich (the grid), Line or Accessible (a numbered list), or None (fail without a user).

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OutputMode
What is returned: Multiple (default; the marked rows or the focused row), Single (the focused row) or None (browse only).

```yaml
Type: GridSelection
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Single, Multiple

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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RefreshRow
Refreshes each acted-on row on the caller's input thread after successful completion. Receives param($Row, $CommandKey) and returns one replacement object. Keep the callback short; action work belongs in New-ConsoleCommand.

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

### -SortBy
The column to sort by at the start.

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

### -Title
The title in the header bar (default Grid).

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
