---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/convertto-consoleview/
schema: 2.0.0
---
# ConvertTo-ConsoleView
## SYNOPSIS
Drafts a reusable console view from sample objects.

## SYNTAX
### __AllParameterSets
```powershell
ConvertTo-ConsoleView [-InputObject] <psobject[]> [-AsDataFile] [-Register] [<CommonParameters>]
```

## DESCRIPTION
Returns a ready-to-paste Register-ConsoleView script with plain property names, named one-line presenter columns and a default count summary. Examines the first 50 objects and chooses up to six nonempty columns from DefaultDisplayPropertySet or declared properties, with a name or identity column first. Reuses default-table presenters, with status words, byte bars for a range greater than tenfold and relative dates for the last 30 days. Long registrations use a splat. Input objects are not changed. Empty input produces no draft.

Keep the script in your profile, or use -AsDataFile for a ModuleName.consoleviews.psd1 file beside the module manifest. Deserialized input drafts for the base type with a warning; Selected input keeps its selected type name with a warning. A draft describes the first object's type; sample one shape at a time.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | ConvertTo-ConsoleView
```

Draft Name, Id, CPU, Handles and SI with a processes count. The Id presenter uses a named variable, plain columns stay names, and the registration uses a splat when needed. Paste the returned script into your profile; drop -Summary $summary to omit its count line.

### EXAMPLE 2
```powershell
Get-ChildItem -File | ConvertTo-ConsoleView -Register
Get-ChildItem -File
```

Register the drafted columns and summary now, and return the compact script for later editing. The next listing uses this view when console formatting is enabled.

### EXAMPLE 3
```powershell
Get-Process | ConvertTo-ConsoleView -AsDataFile | Set-Content Processes.consoleviews.psd1 -Encoding UTF8
Register-ConsoleView -Path ./Processes.consoleviews.psd1
```

Save a data-only view and load it explicitly. Modules can ship a same-named file beside their manifest.

### EXAMPLE 4
```powershell
$id = @{ Name = 'Id'; Expression = 'Id'; Presenter = 'Identifier' }
$summary = {
    param($items)
    @{ Value = @($items).Count; Label = 'processes' }
}
$view = @{
    TypeName = 'System.Diagnostics.Process'
    Property = @('Name', $id, 'CPU', 'Handles', 'SI')
    Summary = $summary
}
Register-ConsoleView @view
Get-Process
```

Keep a process draft in your profile. Plain columns remain names; the Id presenter and count summary are named variables. Remove the Summary entry to leave the count line out.

## PARAMETERS

### -AsDataFile
Returns a complete data-only .consoleviews.psd1 document with one Views entry. Expressions are property names; no script blocks are written. A name too long to fit a line under 100 characters produces a warning and no draft; omit this switch to use a script instead.

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

### -InputObject
Objects of one shape to sample. Only the first 50 objects influence the draft.

```yaml
Type: PSObject[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Register
Also registers the drafted columns and default summary in this session. Use Enable-ConsoleFormatting to show registered views on the console. The summary counts items using the type name and adds lowercase status counts with pass, warning and failure roles.

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

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `System.String`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/convertto-consoleview/](https://consolemonster.dev/powershell/commands/convertto-consoleview/)
