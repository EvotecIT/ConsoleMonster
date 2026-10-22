---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleBinding
## SYNOPSIS
Creates a display rebuilt from application state on the owning PowerShell input thread.

Content receives param($State) and injected $State as a native hashtable. Emit widgets or text using the ordinary layout commands. Input controls must remain in the stable outer layout. Added or assigned state keys write back through AppState; bindings should normally only read state.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleBinding [-Content] <scriptblock> [-Name <string>] [<CommonParameters>]
```

## DESCRIPTION
Creates a display rebuilt from application state on the owning PowerShell input thread.

Content receives param($State) and injected $State as a native hashtable. Emit widgets or text using the ordinary layout commands. Input controls must remain in the stable outer layout. Added or assigned state keys write back through AppState; bindings should normally only read state.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp { New-ConsoleTextInput -Name Query; New-ConsoleBinding { "Query: $($State.Query)" } }
```

Display the current input.

## PARAMETERS

### -Content
Display builder, invoked initially and after state changes on the parent input thread.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
Optional stable name for the dependent display region.

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

- `ConsoleMonster.Interactive.AppBinding`

## RELATED LINKS

- None
