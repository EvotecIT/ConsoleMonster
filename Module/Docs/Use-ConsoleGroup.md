---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Use-ConsoleGroup
## SYNOPSIS
Runs a script inside a collapsible CI log group or beneath a muted terminal heading.

Uses GitHub Actions, Azure Pipelines or GitLab CI commands when detected. CONSOLEMONSTER_CI can force github, azure, gitlab or none. Groups nest and close in finally, including when the script throws. The script runs with the caller's variables and returns its output.

## SYNTAX
### __AllParameterSets
```powershell
Use-ConsoleGroup [-Title] <string> [-ScriptBlock] <scriptblock> [<CommonParameters>]
```

## DESCRIPTION
Runs a script inside a collapsible CI log group or beneath a muted terminal heading.

Uses GitHub Actions, Azure Pipelines or GitLab CI commands when detected. CONSOLEMONSTER_CI can force github, azure, gitlab or none. Groups nest and close in finally, including when the script throws. The script runs with the caller's variables and returns its output.

## EXAMPLES

### EXAMPLE 1
```powershell
Use-ConsoleGroup 'Build' { dotnet build }
```

Group a build log

## PARAMETERS

### -ScriptBlock
The work to run inside the group.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The literal log heading.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.Object`

## RELATED LINKS

- None
