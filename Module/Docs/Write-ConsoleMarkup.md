---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Write-ConsoleMarkup
## SYNOPSIS
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], colours ([red], [#ff8800], [rgb(1,2,3)], [white on navy]) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them: [[ and ]]. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

## SYNTAX
### __AllParameterSets
```powershell
Write-ConsoleMarkup [-Text] <string> [<CommonParameters>]
```

## DESCRIPTION
Writes Spectre-compatible markup as styled text.

Supports [bold], [italic], [underline], colours ([red], [#ff8800], [rgb(1,2,3)], [white on navy]) and theme tokens ([pass], [fail], [warn], [muted], [accent]). Escape brackets by doubling them: [[ and ]]. Colours degrade to 256 or 16 colours, or none, as the terminal requires.

## EXAMPLES

### EXAMPLE 1
```powershell
Write-ConsoleMarkup -Text 'Value'
```


## PARAMETERS

### -Text
The markup.

```yaml
Type: String
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

- `System.String`

## OUTPUTS

- `None`

## RELATED LINKS

- None
