---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consoleaccordion/
schema: 2.0.0
---
# New-ConsoleAccordion
## SYNOPSIS
Creates an accordion for Show-ConsoleApp: collapsible sections that keep one open.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleAccordion [-Content] <Object> [<CommonParameters>]
```

## DESCRIPTION
-Content emits New-ConsoleCollapsible sections. Opening one folds the others; while a header has focus, + expands every section for reading and - folds them all. Each section keeps its own key and state.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp -Content {
    New-ConsoleAccordion -Content {
        $collapsibleOptions2 = @{
            Expanded = $true
            Key = 'Synopsis'
            Content = { 'Gets the processes running on the computer.' }
        }
        New-ConsoleCollapsible @collapsibleOptions2
        $collapsibleOptions = @{
            Key = 'Examples'
            Summary = '(2)'
            Content = {
                'Get-Process -Name pwsh'
                'Get-Process | Sort-Object CPU -Descending | Select-Object -First 5'
            }
        }
        New-ConsoleCollapsible @collapsibleOptions
    }
}
```

Fold long sections of a help page and keep one open at a time.

### EXAMPLE 2
```powershell
New-ConsoleAccordion -Content {
    $collapsibleOptions = @{
        Expanded = $true
        Key = 'network'
        Label = 'Network'
        Content = {
            $textOptions = @{
                Text = @('DNS reachable')
                Role = 'Pass'
            }
            New-ConsoleText @textOptions
        }
    }
    New-ConsoleCollapsible @collapsibleOptions
} | Show-Console
```

Compose an inventory accordion

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'help-sections' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'incident-workbench' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleAccordion in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Content
The sections of the group: New-ConsoleCollapsible output, or a script block that emits it.

```yaml
Type: Object
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

- `ConsoleMonster.Interactive.Accordion`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoleaccordion/](https://consolemonster.dev/powershell/commands/new-consoleaccordion/)
