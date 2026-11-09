---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# ConvertTo-ConsoleView
## SYNOPSIS
Drafts a reusable console view from sample objects.

Returns a ready-to-paste Register-ConsoleView script. Examines the first 50 objects and chooses up to six nonempty columns, preferring DefaultDisplayPropertySet or a name or identity column. Reuses default-table presenters, with status words, byte bars for a range greater than tenfold and relative dates for the last 30 days. Input objects are not changed. Empty input produces no draft.

Keep the script in your profile, or use -AsDataFile for a ModuleName.consoleviews.psd1 file beside the module manifest. Deserialized input drafts for the base type with a warning; Selected input keeps its selected type name with a warning. A draft describes the first object's type; sample one shape at a time.

## SYNTAX
### __AllParameterSets
```powershell
ConvertTo-ConsoleView [-InputObject] <psobject[]> [-AsDataFile] [-Register] [<CommonParameters>]
```

## DESCRIPTION
Drafts a reusable console view from sample objects.

Returns a ready-to-paste Register-ConsoleView script. Examines the first 50 objects and chooses up to six nonempty columns, preferring DefaultDisplayPropertySet or a name or identity column. Reuses default-table presenters, with status words, byte bars for a range greater than tenfold and relative dates for the last 30 days. Input objects are not changed. Empty input produces no draft.

Keep the script in your profile, or use -AsDataFile for a ModuleName.consoleviews.psd1 file beside the module manifest. Deserialized input drafts for the base type with a warning; Selected input keeps its selected type name with a warning. A draft describes the first object's type; sample one shape at a time.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Process | ConvertTo-ConsoleView
```

Draft and inspect a view, then paste the returned script into your profile.

### EXAMPLE 2
```powershell
Get-ChildItem -File | ConvertTo-ConsoleView -Register
```

Register the draft now and return its script for later editing.

### EXAMPLE 3
```powershell
Get-Process | ConvertTo-ConsoleView -AsDataFile | Set-Content Processes.consoleviews.psd1
            Register-ConsoleView -Path ./Processes.consoleviews.psd1
```

Save a data-only view and load it explicitly. Modules can ship a same-named file beside their manifest.

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
Default value: None
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
Also registers the drafted columns in this session. Use Enable-ConsoleFormatting to show registered views on the console. The summary remains a commented suggestion.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject[]`

## OUTPUTS

- `System.String`

## RELATED LINKS

- None
