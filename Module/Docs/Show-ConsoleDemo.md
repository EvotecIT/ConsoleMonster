---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleDemo
## SYNOPSIS
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

Without a slug, it lists the demos to pick from, grouped by category. Executes the same self-contained PowerShell source offered by Export-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot selects a recorded state using scripted input or queued live updates controlled by the runner. The copied source has no capture parameters. Demo output and errors follow the normal PowerShell streams.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleDemo [[-Slug] <string>] [-StaticSnapshot] [-Step <int>] [<CommonParameters>]
```

## DESCRIPTION
Runs an embedded ConsoleMonster gallery demo in the current PowerShell runspace.

Without a slug, it lists the demos to pick from, grouped by category. Executes the same self-contained PowerShell source offered by Export-ConsoleDemo -Path. The demo runs in a child scope of the current runspace and uses the current host and loaded ConsoleMonster commands. No child process, download or temporary script file is used. Live demos can be interrupted normally. -StaticSnapshot selects a recorded state using scripted input or queued live updates controlled by the runner. The copied source has no capture parameters. Demo output and errors follow the normal PowerShell streams.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleDemo
```

Pick a demo from the list and run it.

### EXAMPLE 2
```powershell
Show-ConsoleDemo fleet
```

Run the fleet demo.

### EXAMPLE 3
```powershell
Show-ConsoleDemo assessment -StaticSnapshot -Step 0
```

Render a deterministic snapshot.

## PARAMETERS

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

### -StaticSnapshot
Renders one snapshot instead of running live interactions.

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

### -Step
Deterministic snapshot state, starting at zero. See the demo's Steps metadata for its snapshot count.

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

- `None`

## OUTPUTS

- `System.Management.Automation.PSObject`

## RELATED LINKS

- None
