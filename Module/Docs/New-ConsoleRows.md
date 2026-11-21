---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolerows/
schema: 2.0.0
---
# New-ConsoleRows
## SYNOPSIS
Stacks console widgets vertically.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleRows [[-Content] <Object>] [-Gap <int>] [-Weight <double>] [-Fill] [<CommonParameters>]
```

## DESCRIPTION
Children emitted by the scriptblock are placed top to bottom. In a live dashboard, children created with -Fill or -Weight share the remaining height.

## EXAMPLES

### EXAMPLE 1
```powershell
$rowsOptions = @{
    Gap = 1
    Content = { New-ConsoleRule -Title 'Summary'; New-ConsoleKeyValue -InputObject @{ Total = 12 } }
}
New-ConsoleRows @rowsOptions
```

Two widgets stacked with a blank row between them

### EXAMPLE 2
```powershell
$rowsOptions = @{
    Gap = 1
    Content = {
        $textOptions2 = @{
            Text = @('Inventory')
            Decoration = @('Bold')
        }
        New-ConsoleText @textOptions2
        $textOptions = @{
            Text = @('web01 and web02 are online')
            Role = 'Pass'
        }
        New-ConsoleText @textOptions
    }
}
New-ConsoleRows @rowsOptions | Show-Console
```

Stack an inventory heading and result

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'live-filter' | Select-Object -ExpandProperty Source
            Get-ConsoleDemo -Slug 'task-menu' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleRows in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Content
Scriptblock that emits the child widgets.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Fill
Take the remaining height of a live dashboard.

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

### -Gap
Blank rows between children.

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

### -Weight
Share of the space this group takes inside its parent.

```yaml
Type: Double
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

- `System.Object`

## OUTPUTS

- `ConsoleMonster.Layout.IWidget`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolerows/](https://consolemonster.dev/powershell/commands/new-consolerows/)
