---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consoleoutline/
schema: 2.0.0
---
# New-ConsoleOutline
## SYNOPSIS
Creates an outline for Show-ConsoleApp: a list of headings to jump between, such as settings groups or the sections of a long page.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleOutline [-Key] <string> [-Items] <Object[]> [-SelectedIndex <int>] [-Disabled] [-TabIndex <int>] [-OnChange <scriptblock>] [<CommonParameters>]
```

## DESCRIPTION
The current entry is marked. Up/Down, Home/End and the wheel move the selection; Enter or a click opens it, which makes it current (the final state holds its title). An entry with a Target leads to that control: opening it focuses the control, scrolling a surrounding scroll view to it, and unfolds it when it is a collapsible section. New-ConsoleBreadcrumb -Outline shows the path to the current entry.

## EXAMPLES

### EXAMPLE 1
```powershell
$proxy = New-ConsoleCollapsible -Key Proxy -Content { New-ConsoleTextInput -Key ProxyServer -Label 'Server' }
$dns = New-ConsoleCollapsible -Key Dns -Label 'DNS' -Content { New-ConsoleTextInput -Key DnsServers -Label 'Servers' }
Show-ConsoleApp -Content {
    New-ConsoleSplitter -Ratio 0.3 -First {
        New-ConsoleOutline -Key Sections -Items ([pscustomobject]@{ Title = 'Proxy'; Target = $proxy }), ([pscustomobject]@{ Title = 'DNS'; Target = $dns })
    } -Second { $proxy; $dns }
}
```

Jump between the sections of a settings page.

### EXAMPLE 2
```powershell
New-ConsoleOutline -Key sections -Items Inventory, Network, Storage | Show-Console
```

Compose an inventory outline

## PARAMETERS

### -Disabled
Disables the outline and removes it from keyboard focus traversal.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $false
Accept pipeline input: False
Accept wildcard characters: False
```

### -Items
The entries: titles, or records with Title, Level (1 for the top) and Target (a control to open).

```yaml
Type: Object[]
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
Unique key in the application's state; the value is the current entry's title.

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

### -OnChange
Runs once each time an entry is opened, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.

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

### -SelectedIndex
The index of the entry that starts current; -1 (the default) for none.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: -1
Accept pipeline input: False
Accept wildcard characters: False
```

### -TabIndex
Keyboard focus order. Equal values preserve layout declaration order.

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 0
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `ConsoleMonster.Interactive.Outline`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoleoutline/](https://consolemonster.dev/powershell/commands/new-consoleoutline/)
