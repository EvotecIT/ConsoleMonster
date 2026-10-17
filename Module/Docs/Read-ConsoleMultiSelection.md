---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Read-ConsoleMultiSelection
## SYNOPSIS
Asks the user to pick any number of the choices and returns the chosen objects.

Everything Read-ConsoleSelection does, plus checkboxes: Space toggles (on a group it toggles the whole group, shown as [–] when partly selected; Left and Right collapse and expand groups), Ctrl+A selects all shown, Ctrl+N none, Ctrl+T inverts, Ctrl+O shows only the selected ones. -Minimum and -Maximum are enforced, and a live count shows how many are selected. The chosen objects are written to the pipeline in list order.

## SYNTAX
### __AllParameterSets
```powershell
Read-ConsoleMultiSelection [-Title] <string> [[-Choices] <Object[]>] [-Selected <Object>] [-Minimum <Int32>] [-Maximum <Int32>] [-Collapsed] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-Badge <Object>] [-Hotkey <Object>] [-Disabled <scriptblock>] [-Preview <scriptblock>] [-PageSize <int>] [-Description <string>] [-ParameterName <string>] [-Mode <InteractionMode>] [<CommonParameters>]
```

## DESCRIPTION
Asks the user to pick any number of the choices and returns the chosen objects.

Everything Read-ConsoleSelection does, plus checkboxes: Space toggles (on a group it toggles the whole group, shown as [–] when partly selected; Left and Right collapse and expand groups), Ctrl+A selects all shown, Ctrl+N none, Ctrl+T inverts, Ctrl+O shows only the selected ones. -Minimum and -Maximum are enforced, and a live count shows how many are selected. The chosen objects are written to the pipeline in list order.

## EXAMPLES

### EXAMPLE 1
```powershell
Read-ConsoleMultiSelection -Title 'Value'
```


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

### -Collapsed
Starts with every group collapsed.

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

### -Maximum
The most choices accepted.

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

### -Minimum
The fewest choices accepted.

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

### -Selected
Pre-selected choices: the objects or their displayed texts, or a scriptblock returning $true for selected ones.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: Default
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
