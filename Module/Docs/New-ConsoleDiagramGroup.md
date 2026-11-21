---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolediagramgroup/
schema: 2.0.0
---
# New-ConsoleDiagramGroup
## SYNOPSIS
Groups components and nested diagram groups.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDiagramGroup [-Label] <string> [[-Content] <scriptblock>] [-Boundary <bool>] [<CommonParameters>]
```

## DESCRIPTION
Content emits New-ConsoleDiagramNode and New-ConsoleDiagramGroup objects. The grouped tree keeps all children at narrow widths. Boundary controls the topology outline.

## EXAMPLES

### EXAMPLE 1
```powershell
$diagramGroupOptions = @{
    Label = 'Office LAN'
    Content = {
        $diagramNodeOptions = @{
            Label = '12 PCs'
            Kind = 'Workstation'
            To = @('Firewall')
        }
        New-ConsoleDiagramNode @diagramNodeOptions
    }
}
New-ConsoleDiagramGroup @diagramGroupOptions
```

Office components.

### EXAMPLE 2
```powershell
$diagramOptions = @{
    Title = 'Production services'
    Content = {
        $diagramGroupOptions = @{
            Label = 'Production'
            Content = {
                $diagramNodeOptions = @{
                    Id = 'api'
                    Label = 'Orders API'
                }
                New-ConsoleDiagramNode @diagramNodeOptions
            }
        }
        New-ConsoleDiagramGroup @diagramGroupOptions
    }
}
New-ConsoleDiagram @diagramOptions | Show-Console
```

Compose an inventory diagram group

## PARAMETERS

### -Boundary
Whether to draw the group outline.

```yaml
Type: Boolean
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $true
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
Emits nodes and nested groups.

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

### -Label
Visible heading for the diagram group that contains related nodes.

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

- `ConsoleMonster.Widgets.DiagramGroup`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolediagramgroup/](https://consolemonster.dev/powershell/commands/new-consolediagramgroup/)
