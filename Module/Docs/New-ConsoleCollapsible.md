---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleCollapsible
## SYNOPSIS
Creates a collapsible section for Show-ConsoleApp.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleCollapsible [-Key] <string> [[-Label] <string>] [-Content <Object>] [-Summary <string>] [-Expanded] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [-ValidationMessage <string>] [<CommonParameters>]
```

## DESCRIPTION
A section is a focusable header row with its content below. Enter, Space, Left/Right and a click on the header fold and unfold it; while it is collapsed the header shows -Summary and the content's inputs leave the focus order. The final state holds whether each section is expanded. New-ConsoleAccordion groups sections so that opening one folds the others.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp -Content {
    New-ConsoleAccordion -Content {
        New-ConsoleCollapsible -Key Synopsis -Expanded -Content { 'Gets the services on a computer.' }
        New-ConsoleCollapsible -Key Examples -Summary '(2)' -Content {
            'Get-Service -Name win*'
            'Get-Service | Where-Object Status -eq Running'
        }
    }
}
```

Fold long sections of a help page and keep one open at a time.

### EXAMPLE 2
```powershell
New-ConsoleCollapsible -Key servers -Label 'Checked servers' -Expanded -Content {
                [pscustomobject]@{ Name = 'web01'; State = 'Pass' } | Show-Console -AsWidget -View List
            } | Show-Console
```

Compose an inventory collapsible

## PARAMETERS

### -Content
The section's widgets, text or a script block that emits them.

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

### -Disabled
Disables the header and removes it from keyboard focus traversal.

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

### -Expanded
Starts the section expanded.

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

### -Key
Unique key in the application's state; the value is whether the section is expanded.

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

### -Label
Header text; defaults to the key.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -OnChange
Runs once each time the section folds or unfolds, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Summary
Muted text after the header while collapsed, such as a count.

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

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### -ValidationMessage
Validation text shown in the header.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.Collapsible`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolecollapsible/](https://consolemonster.dev/powershell/commands/new-consolecollapsible/)
