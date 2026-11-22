---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/read-consolepath/
schema: 2.0.0
---
# Read-ConsolePath
## SYNOPSIS
Asks for a file or folder path, with completion.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsolePath [[-Title] <string>] [-Kind <PathKind>] [-MustExist] [-Default <string>] [-Extension <string[]>] [-BasePath <string>] [-ShowHidden] [-Suggestions <int>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Matching entries of the typed folder are listed under the box (folders first); Tab completes the common part or the focused entry, Up and Down move through them. -Kind File or Folder and -MustExist are checked on Enter. Returns the path as typed (environment variables and ~ are expanded only when checking).

## EXAMPLES

### EXAMPLE 1
```powershell
$pathOptions = @{
    MustExist = $true
    Title = 'Import file'
    Kind = 'File'
    Extension = @('.csv')
}
$file = Read-ConsolePath @pathOptions
```

An existing CSV file

### EXAMPLE 2
```powershell
$pathOptions = @{
    MustExist = $true
    Title = 'Inventory folder'
    Default = '.'
    Kind = 'Folder'
}
$path = Read-ConsolePath @pathOptions
$textOptions = @{
    Text = @("Inventory folder: $path")
}
Write-ConsoleText @textOptions
```

Select an existing inventory directory

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'prompt-tour' | Select-Object -ExpandProperty Source
```

Read the gallery script to see Read-ConsolePath in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -BasePath
The folder relative paths start from (default: the current location).

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

### -Default
The path initially shown in the editor; pressing Enter accepts it.

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

### -Extension
File extensions offered as suggestions (.csv, .json).

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

### -Kind
What is accepted: Any, File or Folder.

```yaml
Type: PathKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Any, File, Folder

Required: False
Position: named
Default value: Any
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mode
Forces how to ask: Rich (in-place prompt), Line (question and typed answer), Accessible (numbered, no redraws) or None (use the default or fail).

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

### -MustExist
Requires the path to exist.

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

### -ShowHidden
Includes hidden files and folders in completion suggestions.

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

### -Suggestions
Maximum visible completion suggestions; default six.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 6
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The prompt displayed above the input control.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: Enter a value
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.String`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/read-consolepath/](https://consolemonster.dev/powershell/commands/read-consolepath/)
