---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Read-ConsoleSelection
## SYNOPSIS
Asks the user to pick one of the choices and returns the chosen object.

Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-Disabled) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleSelection [-Title] <string> [[-Choices] <Object[]>] [-Default <Object>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-Badge <Object>] [-Hotkey <Object>] [-Disabled <scriptblock>] [-Preview <scriptblock>] [-PageSize <int>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Asks the user to pick one of the choices and returns the chosen object.

Typing filters the list (fuzzy, matches highlighted). Choices can be grouped (-GroupBy), described (-Describe, shown for the focused choice), tagged (-Badge), picked by a key (-Hotkey; '/' then filters), disabled with a reason (-Disabled) and previewed (-Preview, beside the list when the window is wide). The chosen object itself is returned (a service, a record), not its text. In remoting and the ISE the choices are numbered and the host reads the answer.

## EXAMPLES

### EXAMPLE 1
```powershell
$service = Get-Service | Read-ConsoleSelection 'Service to restart' -Display DisplayName -Describe Name -GroupBy Status
```

Pick a service

### EXAMPLE 2
```powershell
$environment = Read-ConsoleSelection 'Environment' -Choices 'Production', 'Staging', 'Lab' -Default 'Staging'
```

Strings with a default

## PARAMETERS

### -Badge
Short right-aligned text per choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Choices
The choices: any objects (strings, services, records). The chosen objects themselves are returned.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: Items, InputObject
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Default
The default: one of the choices, or its displayed text.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Describe
A sentence for the focused choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: DescriptionProperty
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

### -Disabled
Returns why a choice is unavailable ($_ is the choice), or nothing when it is available.

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

### -Display
What to show for a choice: a property name or a scriptblock ($_ is the choice). Default: the choice as text.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: DisplayProperty
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -GroupBy
The group heading of a choice: a property name or a scriptblock.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Hotkey
A key that picks a choice directly: a property name or a scriptblock returning a letter.

```yaml
Type: Object
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

### -PageSize
The preferred number of visible rows.

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

### -Preview
Content for the preview pane ($_ is the focused choice): text, rich text, a widget, or an object shown as a list.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Object[]`

## OUTPUTS

- `None`

## RELATED LINKS

- None
