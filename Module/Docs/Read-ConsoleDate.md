---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Read-ConsoleDate
## SYNOPSIS
Asks for one civil date or inclusive date range.

Returns one DateTime at midnight with unspecified kind, or one DateRange with -Range. Escape follows the normal prompt cancellation policy. Line and accessible modes accept yyyy-MM-dd and culture-aware dates; non-interactive mode requires a valid explicit default.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleDate [[-Title] <string>] [-Default <Object>] [-Range] [-Selection <DateSelectionOptions>] [-InputObject <psobject>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [-StaticSnapshot] [<CommonParameters>]
```

## DESCRIPTION
Asks for one civil date or inclusive date range.

Returns one DateTime at midnight with unspecified kind, or one DateRange with -Range. Escape follows the normal prompt cancellation policy. Line and accessible modes accept yyyy-MM-dd and culture-aware dates; non-interactive mode requires a valid explicit default.

## EXAMPLES

### EXAMPLE 1
```powershell
Read-ConsoleDate -Title Window -Range -Default ([datetime]'2026-10-08')
```

Read an inclusive maintenance window.

## PARAMETERS

### -Default
Default DateTime or DateRange, validated even in non-interactive mode.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Help text under the question.

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
Optional dated status records, using Date, Status and Label members.

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
Forces how to ask: Rich (in-place prompt), Line (question and typed answer), Accessible (numbered, no redraws) or None (use the default or fail).

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

### -ParameterName
The parameter of your script that supplies this value without a prompt (named in the non-interactive error, for example -Domain).

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

### -Range
Returns one inclusive DateRange.

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

### -Selection
Shared date bounds, culture, today and immutable marked-day model.

```yaml
Type: DateSelectionOptions
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StaticSnapshot
Prints the initialized rich prompt without reading input or returning an answer. Useful for documentation and unattended previews.

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

### -Title
The question.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.DateTime`
- `ConsoleMonster.Dates.DateRange`

## RELATED LINKS

- None
