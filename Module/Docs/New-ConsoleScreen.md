---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleScreen
## SYNOPSIS
Creates a screen for Show-ConsoleApp: its content, its breadcrumb title and the keys that work on it.

Put New-ConsoleScreen commands next to each other in the app content: together they are one screen stack, and the first is where the app starts. To open a screen, assign its key to $State.Screen in a key binding or callback; assigning a screen that is already open goes back to it. Escape or Backspace returns to the previous screen when the focused input does not use them. Inputs inside a screen keep their values while other screens show, and each screen remembers its focused input. The app header (Show-ConsoleApp -Title) shows the open screens as breadcrumbs, and the footer lists the keys that work on the current screen.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleScreen [-Key] <string> [-Content] <scriptblock> [-Title <Object>] [-KeyBinding <Object>] [<CommonParameters>]
```

## DESCRIPTION
Creates a screen for Show-ConsoleApp: its content, its breadcrumb title and the keys that work on it.

Put New-ConsoleScreen commands next to each other in the app content: together they are one screen stack, and the first is where the app starts. To open a screen, assign its key to $State.Screen in a key binding or callback; assigning a screen that is already open goes back to it. Escape or Backspace returns to the previous screen when the focused input does not use them. Inputs inside a screen keep their values while other screens show, and each screen remembers its focused input. The app header (Show-ConsoleApp -Title) shows the open screens as breadcrumbs, and the footer lists the keys that work on the current screen.

## EXAMPLES

### EXAMPLE 1
```powershell
$servers = @(
    [pscustomobject]@{ Name = 'web01'; Role = 'Web'; Status = 'Pass' }
    [pscustomobject]@{ Name = 'sql01'; Role = 'Database'; Status = 'Fail' }
)
Show-ConsoleApp -Title Servers -Content {
    New-ConsoleScreen -Key list -Title Servers -KeyBinding ([ordered]@{
        '1-2' = 'open', { param($Number, $State) $State.Server = $Number; $State.Screen = 'server' }
    }) -Content {
        'Type a number to open a server.'
        $servers | New-ConsoleTable -Property Name, Role, Status -StatusProperty Status
    }
    New-ConsoleScreen -Key server -Title { $servers[$State.Server - 1].Name } -Content {
        New-ConsoleBinding -Content { $servers[$State.Server - 1] | New-ConsoleKeyValue }
    }
}
```

Open a server from a numbered list, and go back with Escape

## PARAMETERS

### -Content
Inputs and widgets on the screen, built once when the app starts. Use New-ConsoleBinding inside it for parts that change with state.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Key
The key that opens the screen, unique within the app. The current key is $State.Screen.

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

### -KeyBinding
Keys that work on this screen, as an ordered dictionary of key to 'label', { action }. The action receives $State; '1-12' opens numbered items and passes the number first: param($Number, $State).

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

### -Title
The breadcrumb title: text, or a script block that builds it from $State (the item a screen shows). Defaults to the key.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Interactive.AppScreen`

## RELATED LINKS

- None
