---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Show-ConsoleReplay
## SYNOPSIS
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

Plays a recording (written by Invoke-ConsoleDashboard -RecordPath or -AutoRecord) into the same dashboard and lets you move through it: space plays and pauses, left and right step frame by frame, + and - change the speed, PgUp and PgDn scrub, Home and End jump to the ends, n/p jump to the next or previous marker, f/F to failures, w/W to warnings, l/L to log entries, and / searches the log text (Enter for the next match). Markers are added while recording with Send-ConsoleEvent -WidgetId '$marker' -Operation mark -Text 'label'. Without -Path the newest automatic recording is played. Without an interactive console the markers are printed as lines.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleReplay [-Dashboard] <IWidget> [[-Path] <string>] [-Title <string>] [-Speed <double>] [-Inline] [-Paused] [-Height <int>] [-PlaybackMode <PlaybackMode>] [-FramesPerSecond <double>] [-MaxFrames <int>] [-Options <ReplayOptions>] [<CommonParameters>]
```

## DESCRIPTION
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

Plays a recording (written by Invoke-ConsoleDashboard -RecordPath or -AutoRecord) into the same dashboard and lets you move through it: space plays and pauses, left and right step frame by frame, + and - change the speed, PgUp and PgDn scrub, Home and End jump to the ends, n/p jump to the next or previous marker, f/F to failures, w/W to warnings, l/L to log entries, and / searches the log text (Enter for the next match). Markers are added while recording with Send-ConsoleEvent -WidgetId '$marker' -Operation mark -Text 'label'. Without -Path the newest automatic recording is played. Without an interactive console the markers are printed as lines.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleReplay -Dashboard $dashboard
```

Rewind the last automatically recorded run

### EXAMPLE 2
```powershell
Show-ConsoleReplay $dashboard .\deploy.jsonl -Inline -Paused
```

Play a recording inline, paused at the start

## PARAMETERS

### -Dashboard
The dashboard (or any widget tree) whose widget names match the recording.

```yaml
Type: IWidget
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FramesPerSecond
Samples the interactive recording at this frame rate; default ten.

```yaml
Type: Double
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
The height of the recorded screen in rows (default the window, or the dashboard's own height inline).

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

### -Inline
Plays below the cursor instead of on the whole window.

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

### -MaxFrames
Maximum sampled interactive frames; default 3000.

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

### -Options
Typed C# ReplayOptions, including input, capture and session configuration. Use this instead of individual player settings; the request is not modified. Omit Session to retain PowerShell's native host relay.

```yaml
Type: ReplayOptions
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
The recording file; default the newest file in the automatic recording folder.

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

### -Paused
Opens paused at the start.

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

### -PlaybackMode
Interactive player controls (default), or automatic playback without reading keys.

```yaml
Type: PlaybackMode
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Interactive, Automatic

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Speed
The starting speed, 0.25 to 16 (default 1).

```yaml
Type: Double
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
The run name in the header (default the recorded dashboard title, or the file name for other widget trees). A recorded dashboard's own header and footer are not replayed: its title, subtitle and status show in the player's header.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `None`

## RELATED LINKS

- None
