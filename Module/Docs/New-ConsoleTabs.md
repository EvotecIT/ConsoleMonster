---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/new-consoletabs/
schema: 2.0.0
---
# New-ConsoleTabs
## SYNOPSIS
Creates retained tabs with keyboard and pointer navigation.

## SYNTAX
### __AllParameterSets
```powershell
New-ConsoleTabs [[-Content] <Object>] [-InputObject <psobject>] [-SelectedIndex <int>] [-Key <string>] [-OnChange <scriptblock>] [<CommonParameters>]
```

## DESCRIPTION
Content emits records with Title and Content, or an ordered dictionary mapping page titles to widgets or script blocks. A dictionary containing both Title and Content is a page record, including when Content is null. Only the selected page receives input; switching pages retains its values and focus. With -Key, the app state holds the active page's title under that key, and -OnChange runs when the page changes.

## EXAMPLES

### EXAMPLE 1
```powershell
$appOptions = @{
    Mode = 'Snapshot'
    Content = {
        $tabsOptions = @{
            Key = 'Page'
            Content = ([ordered]@{
                    Search = { New-ConsoleTextInput -Key Query }
                    About = {
                        $textOptions = @{
                            Text = @('Inventory browser')
                        }
                        New-ConsoleText @textOptions
                    }
                })
        }
        New-ConsoleTabs @tabsOptions
    }
}
Show-ConsoleApp @appOptions
```

Compose two retained pages.

### EXAMPLE 2
```powershell
$tabsOptions = @{
    Key = 'inventory'
    Content = ([ordered]@{
            Servers = {
                $showConsoleOptions = @{
                    AsWidget = $true
                    View = 'List'
                }
                [pscustomobject]@{ Name = 'web01'; State = 'Online' } | Show-Console @showConsoleOptions
            }
            Summary = {
                $textOptions = @{
                    Text = @('One server online')
                    Role = 'Pass'
                }
                New-ConsoleText @textOptions
            }
        })
}
New-ConsoleTabs @tabsOptions | Show-Console
```

Compose two retained inventory pages

### EXAMPLE 3
```powershell
Get-ConsoleDemo -Slug 'settings-tabs' | Select-Object -ExpandProperty Source
Get-ConsoleDemo -Slug 'incident-workbench' | Select-Object -ExpandProperty Source
```

Read any of these gallery scripts to see New-ConsoleTabs in a complete example. Each lookup returns the script's source. Use the same slug with Show-ConsoleDemo to run it.

## PARAMETERS

### -Content
Page records or an ordered dictionary of page titles and content.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputObject
Page records from the pipeline.

```yaml
Type: PSObject
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -Key
Unique key in the application's state; the value is the active page's title.

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

### -OnChange
Runs once each time the active page changes, on the owning input thread. Receives param($Value, $State) and injected $Value, $State and $Control. Output is discarded.

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
Initial zero-based active page.

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

- `System.Management.Automation.PSObject`

## OUTPUTS

- `ConsoleMonster.Widgets.Tabs`

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/new-consoletabs/](https://consolemonster.dev/powershell/commands/new-consoletabs/)
