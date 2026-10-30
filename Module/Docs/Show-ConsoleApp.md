---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleApp
## SYNOPSIS
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -Mode Snapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

## SYNTAX
### Footer (Default)
```powershell
Show-ConsoleApp [-Content] <scriptblock> [-Keys <AppHostKeys>] [-SignalKeys] [-HandleInput <Func[AppState,InputEvent,bool]>] [-TerminalHandoff <TerminalHandoff>] [-Dialogs <AppDialogs>] [-Actions <AppActions>] [-Footer <Object>] [-Title <Object>] [-Subtitle <Object>] [-Detail <Object>] [-Status <StatusKind>] [-MinimumWidth <int>] [-MinimumHeight <int>] [-PassThru] [-NoMouse] [-Command <AppCommand[]>] [-SelectionKey <string>] [-Mode <InteractionMode>] [-InputSource <IInputSource>] [-Capture <ScreenCapture>] [-Profile <TerminalProfile>] [-Theme <ITheme>] [-ReflowBelow <int>] [<CommonParameters>]
```

### Hints
```powershell
Show-ConsoleApp [-Content] <scriptblock> [-Keys <AppHostKeys>] [-SignalKeys] [-HandleInput <Func[AppState,InputEvent,bool]>] [-TerminalHandoff <TerminalHandoff>] [-Dialogs <AppDialogs>] [-Actions <AppActions>] [-FooterHints <Object[]>] [-Title <Object>] [-Subtitle <Object>] [-Detail <Object>] [-Status <StatusKind>] [-MinimumWidth <int>] [-MinimumHeight <int>] [-PassThru] [-NoMouse] [-Command <AppCommand[]>] [-SelectionKey <string>] [-Mode <InteractionMode>] [-InputSource <IInputSource>] [-Capture <ScreenCapture>] [-Profile <TerminalProfile>] [-Theme <ITheme>] [-ReflowBelow <int>] [<CommonParameters>]
```

## DESCRIPTION
Runs native input controls and dependent displays in a console application.

Builds the layout once, then runs input synchronously on the caller's PowerShell thread. Callbacks are queued by controls and drained before input reads; dependent builders run on that same thread. Escape exits. Cancellation restores the terminal through the core runner. Use -Mode Snapshot to capture or print initialized values without interactive input. CONSOLEMONSTER_SNAPSHOT=1 also selects an initialized snapshot when no input source is supplied; CONSOLEMONSTER_INPUT supplies scripted keys centrally. Nothing is sent to the pipeline unless -PassThru requests one ordinary hashtable.

## EXAMPLES

### EXAMPLE 1
```powershell
$servers = @(
    [pscustomobject]@{ Name = 'web01'; Status = 'Pass' }
    [pscustomobject]@{ Name = 'web02'; Status = 'Warn' }
)
Show-ConsoleApp -Content {
    New-ConsoleTextInput -Key Query -OnChange { param($Value, $State) $State.Edits = 1 + $State.Edits }
    New-ConsoleTable -Bind { $servers | Where-Object Name -Like "*$($State.Query)*" } -Property Name, Status
    New-ConsoleBinding -Content { "Edits: $($State.Edits)" }
} -PassThru
```

Filter application data without leaving PowerShell

### EXAMPLE 2
```powershell
Show-ConsoleApp -Mode Snapshot -Content {
    New-ConsoleTextInput -Key Query -Value 'web'
    New-ConsoleBinding -Content { "Query: $($State.Query)" }
} -PassThru
```

Print initialized values without reading keyboard input

## PARAMETERS

### -Actions
App-thread action service for starting the shared worker from registry and dialog callbacks.

```yaml
Type: AppActions
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Capture
Memory capture instead of terminal output; also supplies default screen dimensions.

```yaml
Type: ScreenCapture
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Command
Shared command definitions for shortcuts and the in-app palette.

```yaml
Type: AppCommand[]
Parameter Sets: Footer, Hints
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
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Detail
Current detail beside the application heading.

```yaml
Type: Object
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Dialogs
Typed modal form service attached for this app run.

```yaml
Type: AppDialogs
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Footer
Context key bar, normally a FooterBar reading the app's command registry.

```yaml
Type: Object
Parameter Sets: Footer
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FooterHints
Dictionaries mapping key names to labels, or typed KeyHint values.

```yaml
Type: Object[]
Parameter Sets: Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -HandleInput
App-thread command registry routing before the focused input. Return true when consumed.

```yaml
Type: Func`3
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -InputSource
Input source, including ScriptedInput for examples and tests. Uses the existing core input lease.

```yaml
Type: IInputSource
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Keys
Host focus, cancel and palette gestures. Set a gesture to null to release it to the app registry.

```yaml
Type: AppHostKeys
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MinimumHeight
Minimum terminal height; zero permits any height.

```yaml
Type: Int32
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -MinimumWidth
Minimum terminal width; smaller windows show a resize message without losing focus.

```yaml
Type: Int32
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Mode
Interaction mode override, as in Read-ConsoleText. A running app requires Rich; use -Mode Snapshot without interactive input.

```yaml
Type: InteractionMode
Parameter Sets: Footer, Hints
Aliases: None
Possible values: Rich, Line, Accessible, None, Snapshot

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
Parameter Sets: Footer, Hints
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
Parameter Sets: Footer, Hints
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
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReflowBelow
Default width below which columns stack; explicit column thresholds take precedence.

```yaml
Type: Int32
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SelectionKey
App-state key containing the selected row or row collection supplied to commands.

```yaml
Type: String
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SignalKeys
Allows the terminal to generate Ctrl+C and Ctrl+backslash signals instead of app input events.

```yaml
Type: SwitchParameter
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Status
Semantic status in the application heading.

```yaml
Type: StatusKind
Parameter Sets: Footer, Hints
Aliases: None
Possible values: Pending, Running, Pass, Fail, Warn, Skip, Info, Unknown, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Subtitle
Text qualifying the application title.

```yaml
Type: Object
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TerminalHandoff
Child-terminal and last-output service attached for this app run.

```yaml
Type: TerminalHandoff
Parameter Sets: Footer, Hints
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
Parameter Sets: Footer, Hints
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
Optional heading above the application content.

```yaml
Type: Object
Parameter Sets: Footer, Hints
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

- `System.Collections.Hashtable`

## RELATED LINKS

- None
