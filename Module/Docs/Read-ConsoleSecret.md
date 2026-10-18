---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Read-ConsoleSecret
## SYNOPSIS
Asks for a secret and returns a SecureString (or a string with -AsPlainText).

The typed text is masked (or not shown at all with -Hidden); Ctrl+R reveals it while held open. The answer never appears in the scrollback summary. In remoting and the ISE the host reads it (ReadLineAsSecureString).

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleSecret [-Title] <string> [-AsPlainText] [-Hidden] [-AllowEmpty] [-MinLength <int>] [-Validate <scriptblock>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Asks for a secret and returns a SecureString (or a string with -AsPlainText).

The typed text is masked (or not shown at all with -Hidden); Ctrl+R reveals it while held open. The answer never appears in the scrollback summary. In remoting and the ISE the host reads it (ReadLineAsSecureString).

## EXAMPLES

### EXAMPLE 1
```powershell
Read-ConsoleSecret -Title 'Value'
```


## PARAMETERS

### -AllowEmpty
Accepts an empty answer.

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

### -AsPlainText
Returns a plain string instead of a SecureString.

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

### -Hidden
Shows nothing while typing (not even the length).

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

### -MinLength
The fewest characters.

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
Aliases: Parameter
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
Aliases: Prompt, Question
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Validate
A validator: $_ is the secret; return $true, $false or an error message.

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

- `System.Security.SecureString`
- `System.String`

## RELATED LINKS

- None
