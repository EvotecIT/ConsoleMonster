---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Read-ConsoleText
## SYNOPSIS
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validator check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

## SYNTAX
### Text (Default)
```powershell
Read-ConsoleText [[-Title] <string>] [-Default <string>] [-Placeholder <string>] [-Required] [-MinLength <int>] [-MaxLength <int>] [-Pattern <string>] [-PatternMessage <string>] [-Validator <scriptblock>] [-History <string[]>] [-PrefillDefault] [-Multiline] [-MaxLines <int>] [-Height <int>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [-StaticSnapshot] [<CommonParameters>]
```

### Secret
```powershell
Read-ConsoleText [[-Title] <string>] -AsSecret [-MinLength <int>] [-Validator <scriptblock>] [-AsPlainText] [-Hidden] [-AllowEmpty] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [-StaticSnapshot] [<CommonParameters>]
```

## DESCRIPTION
Asks for a line of text.

A default is shown as a hint (Enter on an empty box accepts it, Tab puts it in the box to edit). -Required, -MinLength, -MaxLength, -Pattern and -Validator check the answer; problems show inline under the box until fixed. -History offers earlier answers with Up and Down.

## EXAMPLES

### EXAMPLE 1
```powershell
$name = Read-ConsoleText 'Server name' -Default 'srv01' -Pattern '[a-z][a-z0-9-]{1,14}' -PatternMessage 'Lower case letters, digits and dashes'
```

A validated name

### EXAMPLE 2
```powershell
Read-ConsoleText 'Email' -Validator { if ($_ -notmatch '@') { 'Enter an email address' } }
```

A scriptblock validator ($_ is the answer; return $true, $false or an error message)

## PARAMETERS

### -AllowEmpty
With AsSecret, accepts an empty answer.

```yaml
Type: SwitchParameter
Parameter Sets: Secret
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsPlainText
With AsSecret, explicitly returns plaintext.

```yaml
Type: SwitchParameter
Parameter Sets: Secret
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AsSecret
Reads a masked secret and returns SecureString by default. History and defaults are forbidden.

```yaml
Type: SwitchParameter
Parameter Sets: Secret
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Default
The default answer.

```yaml
Type: String
Parameter Sets: Text
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
Parameter Sets: Text, Secret
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
Parameter Sets: Text
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Hidden
With AsSecret, hides even the typed length.

```yaml
Type: SwitchParameter
Parameter Sets: Secret
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
Parameter Sets: Text
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
Parameter Sets: Text
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
Parameter Sets: Text
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
Parameter Sets: Text, Secret
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
Parameter Sets: Text, Secret
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
Parameter Sets: Text
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
Parameter Sets: Text, Secret
Aliases: None
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
Parameter Sets: Text
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
Parameter Sets: Text
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
Parameter Sets: Text
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PrefillDefault
Puts the default in the box (editable) instead of showing it as a hint.

```yaml
Type: SwitchParameter
Parameter Sets: Text
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
Parameter Sets: Text
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
Parameter Sets: Text, Secret
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
Parameter Sets: Text, Secret
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Validator
A validator: $_ is the answer; return $true (or nothing) to accept, $false or an error message to refuse.

```yaml
Type: ScriptBlock
Parameter Sets: Text, Secret
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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.String`
- `System.Security.SecureString`

## RELATED LINKS

- None
