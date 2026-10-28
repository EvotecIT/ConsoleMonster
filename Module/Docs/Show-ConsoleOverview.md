---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleOverview
## SYNOPSIS
Shows a compact, read-only summary of the local host.

Collects CPU, memory, local fixed disks, network addresses and safely available services, update/reboot and security observations. Unavailable panels are hidden. Slow sections show a pending marker after the caller budget; each probe receives a two-second cooperative timeout. At 80 columns or more panels use two columns and at most 24 rows. Below 80 columns panels stack at their natural height, preserving volumes and login rows; terminal scrolling is allowed. No elevation, child processes, remote access or system changes are used. Security data requires existing read access. Linux update and security support depends on local update-notifier and auth.log facilities; macOS optional service, update and security panels are hidden. Register application sections through OverviewSectionRegistry.Default.Register. Custom providers must honor cancellation and release their resources.

## SYNTAX
### Render (Default)
```powershell
Show-ConsoleOverview [[-Section] <string[]>] [-Width <int>] [-AsString] [-BudgetMilliseconds <int>] [-AsWidget] [<CommonParameters>]
```

### Object
```powershell
Show-ConsoleOverview [[-Section] <string[]>] [-AsObject] [-BudgetMilliseconds <int>] [<CommonParameters>]
```

## DESCRIPTION
Shows a compact, read-only summary of the local host.

Collects CPU, memory, local fixed disks, network addresses and safely available services, update/reboot and security observations. Unavailable panels are hidden. Slow sections show a pending marker after the caller budget; each probe receives a two-second cooperative timeout. At 80 columns or more panels use two columns and at most 24 rows. Below 80 columns panels stack at their natural height, preserving volumes and login rows; terminal scrolling is allowed. No elevation, child processes, remote access or system changes are used. Security data requires existing read access. Linux update and security support depends on local update-notifier and auth.log facilities; macOS optional service, update and security panels are hidden. Register application sections through OverviewSectionRegistry.Default.Register. Custom providers must honor cancellation and release their resources.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleOverview
```

Show the local login overview.

### EXAMPLE 2
```powershell
$snapshot = Show-ConsoleOverview -Section Resources,Disks -AsObject
```

Return selected data for a script.

### EXAMPLE 3
```powershell
Show-ConsoleOverview -Width 60 -AsString
```

Capture a compact plain-text overview.

## PARAMETERS

### -AsObject
Returns the immutable OverviewSnapshot instead of drawing it.

```yaml
Type: SwitchParameter
Parameter Sets: Object
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsString
Returns one plain-text string for capture or redirection on PowerShell 5.1 and 7.

```yaml
Type: SwitchParameter
Parameter Sets: Render
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsWidget
Returns the view widget without drawing or acquiring input.

```yaml
Type: SwitchParameter
Parameter Sets: Render
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BudgetMilliseconds
The caller's collection budget in milliseconds, from 1 to 900; default 750. Slow sections become pending markers. Native scheduling and rendering time are outside this waiting budget.

```yaml
Type: Int32
Parameter Sets: Render, Object
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Section
Registry section keys, in requested order. Built-ins: Resources, Disks, Network, Services, Updates, Security. Omit to collect all registered sections. Unknown keys produce an error.

```yaml
Type: String[]
Parameter Sets: Render, Object
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Layout width in cells; zero resolves the terminal width, with a 100-cell fallback.

```yaml
Type: Int32
Parameter Sets: Render
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

- `ConsoleMonster.Overview.OverviewSnapshot`
- `System.String`

## RELATED LINKS

- None
