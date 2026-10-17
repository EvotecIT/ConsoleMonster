---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Export-ConsoleRecording
## SYNOPSIS
Exports a dashboard recording to an HTML player, an animated SVG or asciicast.

Recordings (written by Start-ConsoleDashboard -RecordPath) hold updates, not pictures, so they are played into the same dashboard definition at a fixed size and frame rate. Html writes a self-contained page with a small player (play/pause, speed, scrubbing; add #t=30000 to the address to open at 30 seconds). Svg writes an animated image with CSS keyframes and no script, for READMEs. Asciicast writes asciicast v3 for asciinema and other players (output events only). Identical frames are merged; a long recording is sampled less often.

## SYNTAX
### __AllParameterSets
```powershell
Export-ConsoleRecording [-Dashboard] <IWidget> [-Path] <string> [[-Destination] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Exports a dashboard recording to an HTML player, an animated SVG or asciicast.

Recordings (written by Start-ConsoleDashboard -RecordPath) hold updates, not pictures, so they are played into the same dashboard definition at a fixed size and frame rate. Html writes a self-contained page with a small player (play/pause, speed, scrubbing; add #t=30000 to the address to open at 30 seconds). Svg writes an animated image with CSS keyframes and no script, for READMEs. Asciicast writes asciicast v3 for asciinema and other players (output events only). Identical frames are merged; a long recording is sampled less often.

## EXAMPLES

### EXAMPLE 1
```powershell
Export-ConsoleRecording -Dashboard 'Value' -Path 'C:\Path'
```


## PARAMETERS

### -Dashboard
The dashboard (or widget tree) the recording was made with.

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

### -Destination
The file to write. Without it the document goes to the pipeline.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: OutFile
Possible values:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Format
Html, Svg or Asciicast (default: from the -Destination extension, else Html).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Html, Svg, Asciicast

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FramesPerSecond
Frames per second sampled from the recording (default 10).

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
Screen height in cells (default 30).

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

### -PassThru
With -Destination, also writes the file to the pipeline.

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

### -Path
The recording (.jsonl).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Speed
Playback speed baked into the export (default 1).

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

### -Theme
Dark (default) or Light.

```yaml
Type: ExportTheme
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Auto, Dark, Light

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The title (page title, title bar, asciicast title).

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

### -Width
Screen width in cells (default 100).

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

### -WindowChrome
Draws a window frame with a title bar (Html and Svg).

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `None`

## OUTPUTS

- `System.String`
- `System.IO.FileInfo`

## RELATED LINKS

- None
