---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleForm
## SYNOPSIS
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleForm [-Title] <string> [[-Content] <scriptblock>] [-Field <Field[]>] [-Description <string>] [-Command <string>] [-CommandStyle <CommandStyle>] [-NoReview] [-SubmitText <string>] [-StorageKey <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a form or wizard from fields (New-ConsoleField) for Read-ConsoleForm or a menu item.

The scriptblock's New-ConsoleField output becomes the fields. Fields with -Page are grouped into pages in the order the page names first appear, which turns the form into a wizard with a progress strip. Unless -NoReview is given, a review page follows: every answer, the equivalent command line (from -Command and each field's -Parameter), Run, Copy command, Save preset and Back.

## EXAMPLES

### EXAMPLE 1
```powershell
$form = New-ConsoleForm 'New share' -Command 'New-AppShare' {
    New-ConsoleField Name 'Share name' -Page Basics -Required -Parameter Name
    New-ConsoleField Path 'Folder' -Type Path -Kind Folder -MustExist -Page Basics -Parameter Path
    New-ConsoleField ReadOnly 'Read only' -Type Toggle -Page Access -Parameter ReadOnly
}
```

A two-page wizard

## PARAMETERS

### -Command
The command the equivalent command line starts with.

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

### -CommandStyle
How the equivalent command line is written: PowerShell (-Name value), Posix (--name value) or Windows (/name:value).

```yaml
Type: CommandStyle
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: PowerShell, Posix, Windows

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
A scriptblock whose New-ConsoleField output becomes the fields.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
A sentence under the title.

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

### -Field
Fields created beforehand.

```yaml
Type: Field[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoReview
Leaves out the review page.

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

### -StorageKey
The key presets and remembered choices are stored under (default: the title).

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

### -SubmitText
The caption of the submit button (default Run).

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
The title.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.Form`

## RELATED LINKS

- None
