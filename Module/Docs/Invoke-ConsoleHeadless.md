---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Invoke-ConsoleHeadless
## SYNOPSIS
Runs a script (usually an app) on a headless terminal with scripted input and returns the screens it drew.

## SYNTAX
### __AllParameterSets
```powershell
Invoke-ConsoleHeadless [-ScriptBlock] <scriptblock> [-InputScript <string>] [-Width <int>] [-Height <int>] [-ArgumentList <Object[]>] [<CommonParameters>]
```

## DESCRIPTION
The script runs in this session against a headless terminal of the given size, so apps, prompts, menus and dashboards draw there exactly as in a console window. The input is queued first in the recorder's grammar: key names (Down, Enter, Ctrl+K), quoted text, paste:, resize:WxH, wait:ms and the mouse at zero-based cells (click:x,y, dclick:, rclick:, move:, wheel:x,y,n, drag:x1,y1,x2,y2). Input ends after it, so an app that reads past the last key finishes. The result has the final screen's Text and Cells, every distinct screen in Frames and the script's Output, so a Pester test can assert on what the user would have seen. To watch an app in a browser or drive it from an agent, set CONSOLEMONSTER_HEADLESS and CONSOLEMONSTER_DEBUG_SOCKET before starting it; the browser host itself is a C# API.

## EXAMPLES

### EXAMPLE 1
```powershell
$result = Invoke-ConsoleHeadless -Width 56 -Height 10 -InputScript 'Down click:4,5 Escape' {
    Show-ConsoleApp -Content {
        New-ConsoleSelect -Key Server -Choices WEB01, SQL01, FS01
        New-ConsoleBinding -Content { param($State) "Picked $($State.Server)" }
    }
}
$result.Text | Should -Match 'Picked FS01'
```

Drive a list with a key and a click, then check the screen.

### EXAMPLE 2
```powershell
Invoke-ConsoleHeadless -Width 60 -Height 12 -InputScript 'Enter' {
    Read-ConsoleSelect -Title 'Server' -Choices web01, web02
}
```

Run a selection against scripted terminal input

## PARAMETERS

### -ArgumentList
Arguments passed to the script block.

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

### -Height
Terminal height in cells (default 30).

```yaml
Type: Int32
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 30
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputScript
The keys, text and mouse actions to send, in the recorder's input grammar.

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

### -ScriptBlock
The script to run, usually one that calls Show-ConsoleApp or a prompt.

```yaml
Type: ScriptBlock
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Terminal width in cells (default 0: terminal width, falling back to 100).

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

- `ConsoleMonster.PowerShell.ConsoleHeadlessResult`: What a script drew on a headless terminal: the final screen, every distinct screen, and the script's output.

## RELATED LINKS

- [https://consolemonster.dev/powershell/commands/invoke-consoleheadless/](https://consolemonster.dev/powershell/commands/invoke-consoleheadless/)
