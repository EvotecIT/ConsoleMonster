---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# New-ConsoleField
## SYNOPSIS
Creates a form field for New-ConsoleForm.

A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validate and the type's own checks) and the command-line parameter it supplies (-Parameter) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values). Choices are built from any objects: -Display, -GroupBy, -Describe, -Badge, -Disabled and -Selected read each object (a property name, or a scriptblock for choices given with -Choices; loaded choices take property names), and -CommandValue picks the text the equivalent command shows and the field returns.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleField [-Name] <string> [[-Label] <string>] [-Type <ConsoleFieldType>] [-Page <string>] [-Help <string>] [-Parameter <string>] [-Required] [-Default <Object>] [-Placeholder <string>] [-Choices <Object[]>] [-Display <Object>] [-GroupBy <Object>] [-Describe <Object>] [-Badge <Object>] [-Disabled <Object>] [-Selected <Object>] [-CommandValue <Object>] [-Preview <scriptblock>] [-LoadChoices <scriptblock>] [-ArgumentList <Object[]>] [-DependsOn <string[]>] [-VisibleWhen <scriptblock>] [-Validate <scriptblock>] [-Minimum <Decimal>] [-Maximum <Decimal>] [-Step <Decimal>] [-Integer] [-Kind <PathKind>] [-MustExist] [-Extension <string[]>] [-OnText <string>] [-OffText <string>] [-Pattern <string>] [-PatternMessage <string>] [-MinLength <int>] [-MaxLength <int>] [-MaxLines <int>] [-Height <int>] [-CommandPlaceholder <string>] [-NoRemember] [<CommonParameters>]
```

## DESCRIPTION
Creates a form field for New-ConsoleForm.

A field has a name (its key in the result), a label, help shown while it has focus, a default, validation (-Required, -Validate and the type's own checks) and the command-line parameter it supplies (-Parameter) for the equivalent command. -VisibleWhen shows it only while a condition on the other values holds ($Values). -DependsOn resets it when those fields change; with -LoadChoices its options are loaded again in their own runspace (a spinner shows, Esc cancels), from the current values ($Values). Choices are built from any objects: -Display, -GroupBy, -Describe, -Badge, -Disabled and -Selected read each object (a property name, or a scriptblock for choices given with -Choices; loaded choices take property names), and -CommandValue picks the text the equivalent command shows and the field returns.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleField Site 'Site' -Type Select -Choices 'Warsaw', 'Berlin' -Parameter Site
New-ConsoleField Server 'Servers' -Type MultiSelect -DependsOn Site -LoadChoices { param($Values) Get-AppServer -Site $Values.Site } -Display Name -Parameter Server
```

Dependent fields: picking a site loads its servers

### EXAMPLE 2
```powershell
New-ConsoleField Notify 'Send an email' -Type Toggle
New-ConsoleField Email 'Email' -VisibleWhen { $Values.Notify } -Validate { $_ -match '@' }
```

A conditional field

### EXAMPLE 3
```powershell
New-ConsoleField Servers 'Servers' -Type MultiSelect -DependsOn Domain -Parameter ComputerName -LoadChoices {
    param($Values)
    Get-AppServer -Domain $Values.Domain | ForEach-Object {
        [pscustomobject] @{ Name = $_.Name; Site = $_.Site; Role = $_.Role; Offline = $_.OfflineReason; Online = -not $_.OfflineReason }
    }
} -Display Name -CommandValue Name -GroupBy Site -Badge Role -Disabled Offline -Selected Online
```

Loaded servers grouped by site, with their role as a badge, offline ones disabled and online ones selected; the command shows their names

### EXAMPLE 4
```powershell
New-ConsoleField Checks 'Checks' -Type MultiSelect -Choices $checks -Display Name -CommandValue Id -GroupBy Category -Badge { "~$($_.Seconds) s" } -Parameter Check
```

Checks offered by name, returned and shown in the command as their ids, with a typical time as a badge

## PARAMETERS

### -ArgumentList
More arguments for the -LoadChoices script.

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Badge
Short muted text at the right of a choice ("~2 s", "Web"): a property name or a scriptblock. Rich text keeps its own style.

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
The choices of a Select or MultiSelect field (any objects).

```yaml
Type: Object[]
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -CommandPlaceholder
What stands in for a secret in the equivalent command.

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

### -CommandValue
The value of a choice: the text the equivalent command shows and the field returns, a property name or a scriptblock. The returned text keeps the choice object's properties, so $_.Name still works in -Preview and in the submitted values; presets and remembered choices store the text.

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

### -Default
The default (for choices: the choice object or its text; for multi-selections: several).

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

### -DependsOn
Fields whose change resets this one (and reloads its choices).

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

### -Describe
A sentence for the focused choice: a property name or a scriptblock.

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

### -Disabled
Why a choice cannot be picked: a property name or a scriptblock returning the reason. Nothing, $false or an empty string leaves the choice available; $true disables it without a reason. Disabled choices are dimmed and show the reason while they have focus.

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

### -Display
What to show for a choice: a property name or a scriptblock.

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

### -Extension
File extensions offered as path suggestions.

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

### -GroupBy
The group of a choice: a property name or a scriptblock.

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

### -Height
The visible rows of the text area dialog (TextArea, default 8).

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

### -Help
Help shown while the field has focus.

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
Whole numbers only.

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

### -Kind
What a path field accepts: Any, File or Folder.

```yaml
Type: PathKind
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Any, File, Folder

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The label (default: the name).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -LoadChoices
Loads the choices in their own runspace; the script gets the current values as $Values (and its first argument).

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

### -Maximum
The largest number, or the most choices of a MultiSelect field.

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

### -MaxLength
The most characters (TextArea).

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
The most lines (TextArea).

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
The smallest number, or the fewest choices of a MultiSelect field.

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

### -MinLength
The fewest characters of a text or secret field.

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

### -MustExist
Requires a path to exist.

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

### -Name
The key of the value in the result.

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

### -NoRemember
Keeps the value out of presets and remembered choices.

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

### -OffText
Text after a toggle that is off (default No).

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

### -OnText
Text after a toggle that is on (default Yes).

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

### -Page
The wizard page the field is on.

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

### -Parameter
The parameter the field supplies in the equivalent command (Name, -Name or --name).

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

### -Pattern
A pattern a text field must match.

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
Muted text while a text field is empty.

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

### -Preview
Preview content for the focused choice in the editor ($_ is the choice).

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

### -Required
Refuses an empty value.

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

### -Selected
Whether a choice starts selected: a property name or a scriptblock returning $true. Works for loaded choices too, so the choices a loader marks are chosen once they arrive (a preset's values take their place when one is applied).

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

### -Step
The step of a number field.

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

### -Type
The kind: Text (default), Secret, Number, Toggle, Select, MultiSelect or Path.

```yaml
Type: ConsoleFieldType
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Text, Secret, Number, Toggle, Select, MultiSelect, Path, TextArea

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Validate
A validator: $_ is the value, $Values the other values; return $true (or nothing), $false or an error message.

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

### -VisibleWhen
Shows the field only while this returns $true ($Values holds the other values).

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

- `ConsoleMonster.Interactive.Field`

## RELATED LINKS

- None
