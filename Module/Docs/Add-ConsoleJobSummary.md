---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Add-ConsoleJobSummary
## SYNOPSIS
Adds widgets or objects as Markdown to the CI job summary.

Uses the same semantic Markdown exporter as Export-ConsoleFrame. GitHub appends to GITHUB_STEP_SUMMARY. Azure writes a UTF-8 Markdown file in the job workspace and emits task.uploadsummary; the agent owns its later cleanup. GitLab and ordinary terminals write no files and return the Markdown so a script can save it. GitHub and Azure return no pipeline output.

## SYNTAX
### __AllParameterSets
```powershell
Add-ConsoleJobSummary [-InputObject] <Object> [<CommonParameters>]
```

## DESCRIPTION
Adds widgets or objects as Markdown to the CI job summary.

Uses the same semantic Markdown exporter as Export-ConsoleFrame. GitHub appends to GITHUB_STEP_SUMMARY. Azure writes a UTF-8 Markdown file in the job workspace and emits task.uploadsummary; the agent owns its later cleanup. GitLab and ordinary terminals write no files and return the Markdown so a script can save it. GitHub and Azure return no pipeline output.

## EXAMPLES

### EXAMPLE 1
```powershell
@([pscustomobject]@{ Check = 'Build'; Result = 'Passed' }) | Add-ConsoleJobSummary
```

Add a result table to the job summary

## PARAMETERS

### -InputObject
Widgets or objects to export as Markdown. Objects use the automatic view; multiple widgets are stacked.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object`

## OUTPUTS

- `System.String`

## RELATED LINKS

- None
