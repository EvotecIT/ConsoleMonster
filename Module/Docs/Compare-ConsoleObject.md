---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Compare-ConsoleObject
## SYNOPSIS
Compares object sets or shows property drift across a baseline matrix.

## SYNTAX
### Compare (Default)
```powershell
Compare-ConsoleObject [-ReferenceObject] <Object[]> [-DifferenceObject] <Object[]> [-CaseSensitive] [-Normalize <hashtable>] [-AsWidget] [-Key <string[]>] [-CompareProperty <string[]>] [-Ignore <string[]>] [-Depth <int>] [-IncludeUnchanged] [-View <ChangeViewMode>] [-Title <Object>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Confirm
```powershell
Compare-ConsoleObject [-ReferenceObject] <Object[]> [-DifferenceObject] <Object[]> -Question <string> [-CaseSensitive] [-Normalize <hashtable>] [-Key <string[]>] [-CompareProperty <string[]>] [-Ignore <string[]>] [-Depth <int>] [-IncludeUnchanged] [-View <ChangeViewMode>] [-Title <Object>] [-TypedConfirmation <string>] [-Force] [-NoTypedWhenDestructive] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Baseline
```powershell
Compare-ConsoleObject -InputObject <Object[]> [-Name <string[]>] [-Baseline <Object>] [-CaseSensitive] [-Normalize <hashtable>] [-AsWidget] [-Key <string[]>] [-CompareProperty <string[]>] [-Ignore <string[]>] [-Depth <int>] [-IncludeUnchanged] [-View <ChangeViewMode>] [-Title <Object>] [-PassThru] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION
ReferenceObject and DifferenceObject are matched by Key (or Id, Key, Name and similar members), then compared by nested property path. Keys and string values compare case-insensitively by default; CaseSensitive applies to both. Case-only differences remain visible with IsCaseOnly and do not increase changed counts. Missing properties use a localized marker, distinct from present null or empty values. Normalize maps exact paths to scriptblocks receiving $_ or $PSItem; comparison uses their results while display retains the originals. Full, Summary, Table, SideBySide and Unified show two-set comparisons, including line diffs of long values. InputObject selects a baseline matrix with one property per row and one object per column. Baseline chooses a column by name or zero-based integer index; Name or Key sets headings. Equal rows are hidden unless IncludeUnchanged is set; case-only rows remain visible. PassThru returns ObjectChange records or BaselineRow records without drawing. AsWidget returns an IWidget for Show-ConsoleApp layouts and Export-ConsoleFrame.

## EXAMPLES

### EXAMPLE 1
```powershell
$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
$after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Key Name
```

Compare two small inventories

### EXAMPLE 2
```powershell
$before = @([pscustomobject]@{ Name = 'web01'; Port = 80 })
$after = @([pscustomobject]@{ Name = 'web01'; Port = 443 })
$accepted = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Key Name -Question 'Accept these changes?'
$accepted
```

Ask before accepting a configuration change

### EXAMPLE 3
```powershell
$controllers = @(
    [pscustomobject]@{ Name = 'dc01'; Site = 'HQ'; LdapPort = 389 }
    [pscustomobject]@{ Name = 'dc02'; Site = 'HQ'; LdapPort = 636 }
    [pscustomobject]@{ Name = 'dc03'; Site = 'Branch'; LdapPort = 636 }
)
$controllers | Compare-ConsoleObject -View Baseline -Key Name -Baseline dc02 -Ignore Name -Title 'Domain controllers'
```

Compare three domain controller settings against dc02.

### EXAMPLE 4
```powershell
$normalizers = @{ ServicePrincipalName = { $_ -replace 'dc[0-9]+', 'HOST' } }
$widget = Compare-ConsoleObject -ReferenceObject $before -DifferenceObject $after -Key Name -Normalize $normalizers -AsWidget
$widget | Export-ConsoleFrame -Format Html -Width 120 -Path ./comparison.html
```

Normalize host-specific noise while retaining the original displayed values.

## PARAMETERS

### -AsWidget
Returns a composable ChangeView or BaselineView without drawing. Cannot be combined with PassThru or Question.

```yaml
Type: SwitchParameter
Parameter Sets: Compare, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Baseline
The reference column name or zero-based integer index. Default zero. A numeric string is a name.

```yaml
Type: Object
Parameter Sets: Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CaseSensitive
Compares both match keys and string values with ordinal case sensitivity. By default case-only differences remain visible but do not increase changed counts.

```yaml
Type: SwitchParameter
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -CompareProperty
The properties to compare (default all).

```yaml
Type: String[]
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Depth
How deep nested objects are compared (default 3).

```yaml
Type: Int32
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: 3
Accept pipeline input: False
Accept wildcard characters: False
```

### -DifferenceObject
The new objects.

```yaml
Type: Object[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Force
Accepts a nonempty change set without asking. WhatIf still returns false.

```yaml
Type: SwitchParameter
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Ignore
Properties or paths (Address.City, wildcards) left out of the comparison.

```yaml
Type: String[]
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IncludeUnchanged
Lists unchanged objects or baseline rows too. Case-only differences are always visible.

```yaml
Type: SwitchParameter
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Two or more objects to compare as a baseline matrix, in column order. Accepts pipeline input.

```yaml
Type: Object[]
Parameter Sets: Baseline
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Key
The properties that identify an object (matching key). Default: Id, Key, Name and similar members.

```yaml
Type: String[]
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
One explicit column name per input object.

```yaml
Type: String[]
Parameter Sets: Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Normalize
A hashtable of exact property paths to comparison-only scriptblocks. Each receives its original value as $_ and $PSItem. Display and structured values remain original.

```yaml
Type: Hashtable
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoTypedWhenDestructive
Uses a yes/no question for removals unless TypedConfirmation is supplied.

```yaml
Type: SwitchParameter
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns the changes as objects instead of showing them.

```yaml
Type: SwitchParameter
Parameter Sets: Compare, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Question
Shows the changes, asks this question and returns a Boolean answer.

```yaml
Type: String
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReferenceObject
The old objects.

```yaml
Type: Object[]
Parameter Sets: Compare, Confirm
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
A title (markup).

```yaml
Type: Object
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TypedConfirmation
Text that must be typed to confirm the preview.

```yaml
Type: String
Parameter Sets: Confirm
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -View
The view: Full (default), Summary, Table, SideBySide, Unified or Baseline (with InputObject).

```yaml
Type: ChangeViewMode
Parameter Sets: Compare, Confirm, Baseline
Aliases: None
Possible values: Full, Summary, Table, SideBySide, Unified, Baseline

Required: False
Position: named
Default value: Full
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object[]`

## OUTPUTS

- `ConsoleMonster.Changes.ObjectChange`
- `ConsoleMonster.Changes.BaselineRow`
- `ConsoleMonster.Changes.ChangeView`
- `ConsoleMonster.Changes.BaselineView`
- `System.Boolean`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/compare-consoleobject/](https://consolemonster.dev/powershell/commands/compare-consoleobject/)
