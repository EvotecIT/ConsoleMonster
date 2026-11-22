---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consolediagramusecase/
schema: 2.0.0
---
# New-ConsoleDiagramUseCase
## SYNOPSIS
Builds a playable sequence of diagram interactions.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleDiagramUseCase [-Label] <string> [[-Content] <scriptblock>] [-Id <string>] [-Delay <int>] [-Loop] [-AutoPlay] [<CommonParameters>]
```

## DESCRIPTION
Content emits New-ConsoleDiagramStep values in playback order. Id defaults to Label. AutoPlay starts only when the keyboard view opens; static output keeps the selected step.

## EXAMPLES

### EXAMPLE 1
```powershell
$diagramUseCaseOptions = @{
    Label = 'Place an order'
    Content = {
        $diagramStepOptions2 = @{
            From = 'Shopper'
            To = 'Web'
            Label = 'POST /checkout'
        }
        New-ConsoleDiagramStep @diagramStepOptions2;
        $diagramStepOptions = @{
            Reply = $true
            From = 'Web'
            To = 'Shopper'
            Label = 'Thank you'
        }
        New-ConsoleDiagramStep @diagramStepOptions
    }
}
New-ConsoleDiagramUseCase @diagramUseCaseOptions
```

A request and reply.

### EXAMPLE 2
```powershell
$diagramOptions = @{
    Title = 'Order processing'
    Content = {
        $diagramNodeOptions2 = @{
            Id = 'api'
            Label = 'API'
        }
        New-ConsoleDiagramNode @diagramNodeOptions2
        $diagramNodeOptions = @{
            Id = 'db'
            Label = 'Database'
        }
        New-ConsoleDiagramNode @diagramNodeOptions
        $diagramUseCaseOptions = @{
            Id = 'save'
            Label = 'Save order'
            Content = {
                $diagramStepOptions = @{
                    From = 'api'
                    To = 'db'
                    Label = 'Insert'
                }
                New-ConsoleDiagramStep @diagramStepOptions
            }
        }
        New-ConsoleDiagramUseCase @diagramUseCaseOptions
    }
}
New-ConsoleDiagram @diagramOptions | Show-Console
```

Compose an inventory diagram use case

## PARAMETERS

### -AutoPlay
Starts playback on opening the keyboard view.

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

### -Content
Emits interactions in order.

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

### -Delay
Playback milliseconds per step.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 900
Accept pipeline input: False
Accept wildcard characters: False
```

### -Id
The explicit selection key.

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

### -Label
The use-case label and default id.

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

### -Loop
Repeats playback.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Widgets.DiagramUseCase`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consolediagramusecase/](https://consolemonster.dev/powershell/commands/new-consolediagramusecase/)
