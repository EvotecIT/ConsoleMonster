---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Read-ConsoleNumber
## SYNOPSIS
Asks for a number.

Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleNumber [-Title] <string> [-Default <Decimal>] [-Minimum <Decimal>] [-Maximum <Decimal>] [-Step <decimal>] [-Integer] [-Validator <scriptblock>] [-Culture <cultureinfo>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [-StaticSnapshot] [<CommonParameters>]
```

## DESCRIPTION
Asks for a number.

Whole numbers with -Integer (returns [int]), decimals otherwise (returns [decimal]). -Minimum and -Maximum are shown as a hint and checked; Up and Down step by -Step (PgUp and PgDn by ten steps).

## EXAMPLES

### EXAMPLE 1
```powershell
$port = Read-ConsoleNumber 'Port' -Integer -Minimum 1 -Maximum 65535 -Default 443
```

A port number

## PARAMETERS

### -Culture
Culture used to parse and display the answer; omitted uses the current culture.

```yaml
Type: CultureInfo
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Default
The default.

```yaml
Type: Decimal
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Description
Help text under the question.

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

### -Integer
Accepts whole numbers only and returns [int].

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Maximum
The largest value.

```yaml
Type: Decimal
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Minimum
The smallest value.

```yaml
Type: Decimal
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mode
Forces how to ask: Rich (in-place prompt), Line (question and typed answer), Accessible (numbered, no redraws) or None (use the default or fail).

```yaml
Type: InteractionMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Rich, Line, Accessible, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ParameterName
The parameter of your script that supplies this value without a prompt (named in the non-interactive error, for example -Domain).

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

### -StaticSnapshot
Prints the initialized rich prompt without reading input or returning an answer. Useful for documentation and unattended previews.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Step
The step of Up and Down (default 1).

```yaml
Type: Decimal
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The question.

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

### -Validator
A validator: $_ is the number; return $true, $false or an error message.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.Int32`
- `System.Decimal`

## RELATED LINKS

- None
