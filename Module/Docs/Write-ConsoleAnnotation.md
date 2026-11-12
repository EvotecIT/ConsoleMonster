---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/write-consoleannotation/
schema: 2.0.0
---
# Write-ConsoleAnnotation
## SYNOPSIS
Writes a CI error, warning or notice, or a styled terminal status line.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleAnnotation -Level <AnnotationLevel> -Message <string> [-Title <string>] [-File <string>] [-Line <Int32>] [-Column <Int32>] [<CommonParameters>]
```

## DESCRIPTION
GitHub Actions and Azure Pipelines receive native annotations with escaped literal text. Azure notices and GitLab annotations are ordinary lines. Other hosts show a status line with a source-file link where supported. This command does not change the process exit code or write to PowerShell's error stream.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleAnnotation -Level Error -Message 'Missing configuration' -File ./build.ps1 -Line 12 -Column 5
```

Point to a build error

### EXAMPLE 2
```powershell
$result = [pscustomobject]@{ Name = 'Inventory'; Detail = 'Two servers need an update' }
Write-ConsoleAnnotation -Level Warning -Title $result.Name -Message $result.Detail
```

Highlight an inventory warning in a CI log

## PARAMETERS

### -Column
The one-based source column.

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

### -File
The optional source file; relative paths resolve from the current PowerShell location. Files inside the provider workspace use checkout-relative paths with forward slashes; outside paths stay as supplied. Terminal labels are relative to the current location and show the file and line once, linked where supported.

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

### -Level
Error, Warning or Notice.

```yaml
Type: AnnotationLevel
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Error, Warning, Notice

Required: True
Position: named
Default value: Error
Accept pipeline input: False
Accept wildcard characters: False
```

### -Line
The one-based source line.

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

### -Message
The literal annotation text.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
An optional title; Azure and plain lines prepend it to the message.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Void`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/write-consoleannotation/](https://consolemonster.dev/powershell/commands/write-consoleannotation/)
