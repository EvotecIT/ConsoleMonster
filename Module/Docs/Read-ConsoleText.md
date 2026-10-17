---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Read-ConsoleText
## SYNOPSIS
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validate check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleText [-Title] <string> [-Default <string>] [-Placeholder <string>] [-Required] [-MinLength <int>] [-MaxLength <int>] [-Pattern <string>] [-PatternMessage <string>] [-Validate <scriptblock>] [-History <string[]>] [-Prefill] [-Multiline] [-MaxLines <int>] [-Height <int>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validate check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

## EXAMPLES

### EXAMPLE 1
```powershell
Read-ConsoleText -Title 'Value'
```


## PARAMETERS

### -Default
The default answer.

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

### -Height
The visible rows of the text area (-Multiline, default 6).

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

### -History
Earlier answers offered with Up and Down (oldest first).

```yaml
Type: String[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MaxLength
The most characters.

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

### -MaxLines
The most lines (-Multiline).

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

### -Multiline
Asks for several lines of text in a text area: soft wrap, Enter for a new line, Ctrl+Enter (or Ctrl+S) to save, paste keeps line breaks. In line mode the answer ends with an empty line.

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

### -Pattern
A regular expression the whole answer must match.

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

### -PatternMessage
The message when -Pattern does not match.

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

### -Placeholder
Muted text in the empty box.

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

### -Prefill
Puts the default in the box (editable) instead of showing it as a hint.

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

### -Required
Refuses an empty answer.

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
A validator: $_ is the answer; return $true (or nothing) to accept, $false or an error message to refuse.

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

- `System.String`

## RELATED LINKS

- None
