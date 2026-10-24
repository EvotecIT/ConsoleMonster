---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleApp
## SYNOPSIS
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -StaticSnapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleApp [-Content] <scriptblock> [-PassThru] [-StaticSnapshot] [-NoMouse] [-Mode <InteractionMode>] [-InputSource <IInputSource>] [-Capture <ScreenCapture>] [-Profile <TerminalProfile>] [-Theme <ITheme>] [-Live <LiveSessionOptions>] [<CommonParameters>]
```

## DESCRIPTION
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -StaticSnapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleApp {
    New-ConsoleTextInput -Name Query -OnChange { param($Value, $State) $State.Edits = 1 + $State.Edits }
    New-ConsoleTable -Bind { $services | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status
    New-ConsoleBinding { "Edits: $($State.Edits)" }
} -PassThru
```

Filter application data without leaving PowerShell.

### EXAMPLE 2
```powershell
# Using the object directly
            $capture = [ConsoleMonster.Interactive.ScreenCapture]::new(80, 24)
$keys = [ConsoleMonster.Input.ScriptedInput]::new('"abc" Escape')
Show-ConsoleApp { New-ConsoleTextInput -Name Query } -InputSource $keys -Capture $capture -PassThru
```

Capture a scripted run without a physical console.

## PARAMETERS

### -Capture
Memory capture instead of terminal output; also supplies default screen dimensions.

```yaml
Type: ScreenCapture
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Content
Layout script emitting stable inputs and widgets. Invoked once on the caller's thread.

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

### -InputSource
Input source, including ScriptedInput for examples and tests. Uses the existing core input lease.

```yaml
Type: IInputSource
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Live
Optional full-screen live rendering settings, passed directly to the core runner. Inline mode is rejected before input or state initialization.

```yaml
Type: LiveSessionOptions
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
Interaction mode override, as in Read-ConsoleText. A running app requires Rich; use -StaticSnapshot without interactive input.

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

### -NoMouse
Disables optional terminal mouse reporting while retaining keyboard operation.

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

### -PassThru
Returns final named values as one ordinary hashtable. Callback output is discarded.

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

### -Profile
Terminal capabilities and dimensions; defaults to the existing interactive profile resolution.

```yaml
Type: TerminalProfile
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -StaticSnapshot
Prints or captures one initialized snapshot without reading input; usable in CI or redirected output.

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

### -Theme
Core theme used to render controls and dependent widgets.

```yaml
Type: ITheme
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

- `System.Collections.Hashtable`

## RELATED LINKS

- None
