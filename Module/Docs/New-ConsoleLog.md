---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleLog
## SYNOPSIS
Creates a log region: the newest lines of a log, bottom-aligned.

In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen. Write to it explicitly with Write-ConsoleLog.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleLog [[-Name] <string>] [-Height <int>] [-Capacity <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates a log region: the newest lines of a log, bottom-aligned.

In a live dashboard the log region receives Write-Host, Write-Information, Write-Warning, Write-Verbose and error output from the runspaces Start-ConsoleDashboard owns, plus Console.WriteLine from .NET code, so nothing scribbles over the screen. Write to it explicitly with Write-ConsoleLog.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleLog -Name 'Name'
```


## PARAMETERS

### -Capacity
Lines kept in memory (default 1000).

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Rows requested when printed once (default 8).

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Name
The live name (default log).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
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

- `ConsoleMonster.Widgets.LogRegion`

## RELATED LINKS

- None
