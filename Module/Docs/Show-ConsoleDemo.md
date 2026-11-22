---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consoledemo/
schema: 2.0.0
---
# Show-ConsoleDemo
## SYNOPSIS
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleDemo [[-Slug] <string>] [-Mode <InteractionMode>] [-Step <int>] [<CommonParameters>]
```

## DESCRIPTION
Gallery picker
--------------

Omitting Slug opens a picker grouped by category. Type to filter. Focus a demo and press Enter to run it. Escape closes the picker without running a demo. In a session that cannot ask for a selection, supply a slug from Get-ConsoleDemo.

Demo run
--------

Supplying Slug runs the chosen demo directly. Each script defines its own output and interaction. It runs in a child scope of the current PowerShell runspace and uses the current host and loaded ConsoleMonster commands. Get-ConsoleDemo exposes the same script in its Source property. Output and errors follow the normal PowerShell streams.

Snapshot
--------

Mode Snapshot displays one recorded state selected by Step. The runner supplies the scripted input or queued live updates for that state. The copied demo source has no capture parameters.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleDemo
```

Pick a demo from the list and run it.

### EXAMPLE 2
```powershell
$demoOptions = @{
    Slug = 'fleet'
}
Show-ConsoleDemo @demoOptions
```

Run the fleet demo.

### EXAMPLE 3
```powershell
$demoOptions = @{
    Slug = 'assessment'
    Mode = 'Snapshot'
    Step = 0
}
Show-ConsoleDemo @demoOptions
```

Render the first snapshot state from assessment. Step is zero-based. Use Get-ConsoleDemo -Slug 'assessment' to read the Steps metadata before choosing a later state.

### EXAMPLE 4
```powershell
Get-ConsoleDemo | Sort-Object -Property Category, Slug |
    Select-Object -Property Category, Slug, Title

$demo = Get-ConsoleDemo -Slug 'chart-table'
$demo.Source
```

Inspect every bundled example, grouped by gallery category. Use a slug from the list to retrieve its complete PowerShell source, then use the same slug with Show-ConsoleDemo when you want to run it.

## PARAMETERS

### -Mode
Renders one snapshot instead of running live interactions.

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Snapshot

Required: False
Position: named
Default value: Rich
Accept pipeline input: False
Accept wildcard characters: False
```

### -Slug
Exact demo slug from Get-ConsoleDemo. Without it, the demos are listed to pick from.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Step
Deterministic snapshot state, starting at zero. See the demo's Steps metadata for its snapshot count.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Management.Automation.PSObject`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/show-consoledemo/](https://consolemonster.dev/powershell/commands/show-consoledemo/)
