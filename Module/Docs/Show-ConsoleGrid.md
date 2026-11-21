---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolegrid/
schema: 2.0.0
---
# Show-ConsoleGrid
## SYNOPSIS
Shows pipeline objects in an interactive grid and returns the rows you choose.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleGrid [[-Property] <Object[]>] [-InputObject <psobject>] [-Title <string>] [-OutputMode <GridSelection>] [-Filter <string>] [-SortColumn <string>] [-Descending] [-Exclude <string[]>] [-Command <Object[]>] [-RefreshRow <scriptblock>] [-Culture <cultureinfo>] [-Mode <InteractionMode>] [-Window] [<CommonParameters>]
```

## DESCRIPTION
Show-ConsoleGrid displays objects in Windows PowerShell 5.1 and PowerShell 7. Property configures the displayed columns using the same presenters, priorities and widths as Show-ConsoleTable. A selection returns the underlying objects to PowerShell.

Terminal grid
-------------

Use the arrow keys to focus a row. Press F10 or Ctrl+Enter to accept the selection and resume the PowerShell pipeline. Single returns the focused row. In Multiple mode, Space toggles the mark on the focused row and Ctrl+A marks all visible rows. Accept returns the marked rows, or the focused row if none are marked. In None mode, the accept keys are disabled and the grid is browse-only.

Use PgUp and PgDn to move a page at a time, or Ctrl+Home and Ctrl+End to move to the first or last row. Press Enter to inspect every column on the focused row. Type to filter. Each search word must appear in a displayed column. Press F2 to choose columns, F3 or Shift+F3 to cycle the sort column, and F4 to reverse the sort. Esc first clears an active filter. With the filter empty, Esc closes the grid without returning any objects.

When Command supplies actions, Ctrl+K opens the action list and F6 cancels work in progress. Actions run inside the grid. Accepting a selection returns the rows to the calling script.

Browser grid
------------

With Window, the browser tab has a filter, sortable column headings and a table. Click a row to focus it. In Multiple mode, hold Ctrl or Command when clicking to toggle a row, or Shift-click to mark a range. Click OK to send the selection back to PowerShell. Browse-only mode has a Close button. Click Cancel or press Esc in the page to return without a selection. If the browser cannot open, the command warns and falls back to the terminal grid.

Numbered list
-------------

Line mode prints numbered rows and prompts for a response. In Single mode, enter one row number or press Enter with an empty response to choose the first row. In Multiple mode, enter a comma-separated list of numbers, such as 1,3. An empty response with no preselection returns nothing.

Static output
-------------

With Mode None, or when the command resolves to unattended output, it prints a static grid using the initial filter, sort and visible columns. It returns no selection.

## EXAMPLES

### EXAMPLE 1
```powershell
$servers = @(
    [pscustomobject]@{ Name = 'web01'; Status = 'Pass'; Region = 'East' }
    [pscustomobject]@{ Name = 'web02'; Status = 'Warn'; Region = 'West' }
)
$gridOptions = @{
    Property = @('Name', 'Status', 'Region')
    Title = 'Servers'
    Command = @(@{ Inspect = { param($Row) $Row | Select-Object Name, Status, Region } })
}
$servers | Show-ConsoleGrid @gridOptions
```

Inspect a server inside the terminal grid. Press Ctrl+K and choose Inspect to run the command against the focused or marked rows. Press F10 or Ctrl+Enter when you want to close the grid and return the selected rows to PowerShell.

### EXAMPLE 2
```powershell
$gridOptions = @{
    Descending = $true
    Property = @('Name', 'Id', @{ Name = 'Memory'; Expression = { $_.WorkingSet64 }; Presenter = 'Bytes' })
    SortColumn = 'Memory'
    OutputMode = 'None'
}
Get-Process | Show-ConsoleGrid @gridOptions
```

Browse processes sorted by memory. None turns off selection output. Press Enter to inspect a row and Esc to leave the grid.

### EXAMPLE 3
```powershell
$gridOptions = @{
    Title = 'Preview stopping a process'
    Property = @('ProcessName', 'Id', 'CPU')
    SortColumn = 'ProcessName'
    OutputMode = 'Single'
    Window = $false
}
$selectedProcess = Get-Process | Show-ConsoleGrid @gridOptions
if ($null -ne $selectedProcess) {
    Stop-Process -InputObject @($selectedProcess) -WhatIf
}
```

Preview stopping one process from the terminal grid. Use the arrows to focus a process, then press F10 or Ctrl+Enter. PowerShell resumes with the original Process object. Stop-Process receives that object, and WhatIf previews the stop. Cancelling the grid skips the preview.

### EXAMPLE 4
```powershell
$gridOptions = @{
    Window = $true
    Property = @('Name', 'DisplayName', 'Status')
    Title = 'Stop services'
}
Get-Service | Where-Object Status -eq Running | Show-ConsoleGrid @gridOptions | Stop-Service -WhatIf
```

Preview stopping services from the browser grid. Choose rows and click OK to pass the original service objects to Stop-Service. WhatIf previews the operation for each selected service.

### EXAMPLE 5
```powershell
Get-ConsoleDemo -Slug 'service-browser' | Select-Object -ExpandProperty Source
```

Read the gallery script to see Show-ConsoleGrid in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

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
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Exclude
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
Controls how the grid prompts. Rich uses the interactive terminal grid. Line uses a numbered prompt. Accessible uses numbered prompts when a line console is available. None prints a static grid and returns no selection.

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None, Snapshot

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OutputMode
Controls what is returned when the operator accepts a selection. Multiple returns the marked rows, or the focused row if there are no marks in the terminal or browser grid. Single returns one row. None returns no objects. In the terminal, F10 and Ctrl+Enter accept a selection in Single and Multiple modes.

```yaml
Type: GridSelection
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, Single, Multiple

Required: False
Position: named
Default value: Multiple
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

### -SortColumn
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
Default value: Grid
Accept pipeline input: False
Accept wildcard characters: False
```

### -Window
Shows the grid in the default browser instead of the terminal, with the same columns, filter, sort and selection; the chosen rows return to the pipeline. The page is served from 127.0.0.1 behind a one-time address and loads nothing external. Ctrl+C or closing the tab cancels. When no browser can be opened, the terminal grid is shown instead. Sessions where nobody can answer (-Mode None, PowerShell -NonInteractive, CI) never open a browser and print the static view as without -Window. -Command and -RefreshRow apply to the terminal grid only. Set $PSDefaultParameterValues['Show-ConsoleGrid:Window'] = $true to use the browser by default, including for the Out-GridView fallback.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
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

- [https://consolemonster.dev/powershell/commands/show-consolegrid/](https://consolemonster.dev/powershell/commands/show-consolegrid/)
