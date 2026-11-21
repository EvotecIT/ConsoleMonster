---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolehelp/
schema: 2.0.0
---
# Show-ConsoleHelp
## SYNOPSIS
Shows a command's help: synopsis, highlighted syntax, a parameter table and highlighted examples.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleHelp [[-Name] <psobject>] [-Width <int>] [-NoExamples] [-AsWidget] [-NoParameters] [-NoPager] [<CommonParameters>]
```

## DESCRIPTION
Reads the available Get-Help -Full content, including cmdlet help files, comment-based help and PowerShell's generated help. It includes syntax by parameter set, parameter details, highlighted PowerShell examples, inputs, outputs, notes and related links. About topics use the shared Markdown reader, retaining literal HTML and safe link handling. The help is available in Windows PowerShell 5.1 and PowerShell 7.

### Help viewer

On an interactive terminal, help longer than the screen can open a document viewer. The Overview, Parameters, Examples and Related tabs organize the help. Use the outline and folding sections to navigate, / to find text, or Ctrl+K to jump to a section or parameter. Open related command help in the same viewer and use Alt+Left or Alt+Right to move through the history. Press q to close the viewer and return to PowerShell.

### Direct help output

NoPager writes the help directly. When you pipe the output to another command such as more, Out-Host -Paging or Out-File, it emits plain lines suitable for that destination.

## EXAMPLES

### EXAMPLE 1
```powershell
$helpOptions = @{
    Name = 'Get-ChildItem'
}
Show-ConsoleHelp @helpOptions
```

A cmdlet

### EXAMPLE 2
```powershell
$helpOptions = @{
    AsWidget = $true
    Name = 'Show-ConsoleTable'
}
Show-ConsoleHelp @helpOptions | Export-ConsoleFrame -Format Text
```

Through a pager

## PARAMETERS

### -AsWidget
Returns the view widget without drawing or acquiring input.

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

### -Name
The command (a cmdlet, function, alias or script path), or an about topic.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -NoExamples
Leaves out the examples.

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

### -NoPager
Writes the help to the console instead of opening the document viewer, also on an interactive terminal.

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

### -NoParameters
Leaves out the detailed parameter reference.

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

### -Width
The width in columns (default: the console's width).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.String`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/show-consolehelp/](https://consolemonster.dev/powershell/commands/show-consolehelp/)
