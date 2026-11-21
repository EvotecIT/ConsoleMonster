---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/powershell/commands/show-consolereplay/
schema: 2.0.0
---
# Show-ConsoleReplay
## SYNOPSIS
Rewinds a recorded run: an in-terminal player with a timeline, markers, failures, warnings and log search.

## SYNTAX
### __AllParameterSets
```powershell
Show-ConsoleReplay [-Dashboard] <IWidget> [[-RecordingPath] <string>] [-Title <string>] [-Speed <double>] [-Inline] [-AutoPlay] [-Height <int>] [-PlaybackMode <PlaybackMode>] [-FramesPerSecond <double>] [-MaxFrames <int>] [<CommonParameters>]
```

## DESCRIPTION
Plays a dashboard recording created with RecordingPath or AutoRecord on Invoke-ConsoleDashboard. Without RecordingPath, it uses the newest automatic recording.

Replay player
-------------

The player displays the reconstructed dashboard and a timeline with the recording's markers. Space plays or pauses. Left and Right step through frames, PgUp and PgDn scrub, Home and End jump to the ends, and + or - changes speed. Use n or p for the next or previous marker, f or Shift+F for failures, w or Shift+W for warnings, and l or Shift+L for log entries. Press / to search log text and Enter for the next match. Press q to close the player. Inline places it below the current cursor.

Unattended playback and marker output
-------------------------------------

PlaybackMode Automatic replays without reading keys. When interactive playback is requested in a session without an interactive console, the command prints the markers as lines.

## EXAMPLES

### EXAMPLE 1
```powershell
# Requires build.jsonl recorded with this dashboard definition.
$dashboardOptions = @{
    Title = 'Build'
    Content = {
        $statusListOptions = @{
            WidgetId = 'steps'
            Content = { 'Compile' }
        }
        New-ConsoleStatusList @statusListOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$replayOptions = @{
    Dashboard = $dashboard
    RecordingPath = './build.jsonl'
}
Show-ConsoleReplay @replayOptions
```

Play an existing matching dashboard recording

### EXAMPLE 2
```powershell
# Requires build.jsonl recorded with this dashboard definition.
$dashboardOptions = @{
    Title = 'Build'
    Content = {
        $statusListOptions = @{
            WidgetId = 'steps'
            Content = { 'Compile' }
        }
        New-ConsoleStatusList @statusListOptions
    }
}
$dashboard = New-ConsoleDashboard @dashboardOptions
$replayOptions = @{
    Inline = $true
    AutoPlay = $false
    Dashboard = $dashboard
    RecordingPath = './build.jsonl'
}
Show-ConsoleReplay @replayOptions
```

Open an existing recording inline and paused. Press Right to step one frame, Space to play or pause, and q to close the player.

## PARAMETERS

### -AutoPlay
Starts playback immediately; use -AutoPlay:$false to open paused.

```yaml
Type: SwitchParameter
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: $true
Accept pipeline input: False
Accept wildcard characters: False
```

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
Default value: 10
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
Default value: 0
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
Default value: $false
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
Default value: 3000
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
Default value: Interactive
Accept pipeline input: False
Accept wildcard characters: False
```

### -RecordingPath
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

### -Speed
The starting speed, 0.25 to 16 (default 1).

```yaml
Type: Double
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: 1
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

- [https://consolemonster.dev/powershell/commands/show-consolereplay/](https://consolemonster.dev/powershell/commands/show-consolereplay/)
