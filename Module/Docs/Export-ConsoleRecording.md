---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Export-ConsoleRecording
## SYNOPSIS
Exports a terminal session or dashboard recording to a player, SVG or asciicast.

Without -Dashboard, reads a whole-session asciicast v2 from Start-ConsoleRecording or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Start-ConsoleDashboard widget updates into the same definition, preserving the existing dashboard HTML, SVG and asciicast v3 exports. Add #t=30000 to an HTML address to open at 30 seconds.

## SYNTAX
### Session (Default)
```powershell
Export-ConsoleRecording [-Path] <string> [[-Destination] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [-FinalFrame] [<CommonParameters>]
```

### Dashboard
```powershell
Export-ConsoleRecording [-Dashboard] <IWidget> [-Path] <string> [[-Destination] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Exports a terminal session or dashboard recording to a player, SVG or asciicast.

Without -Dashboard, reads a whole-session asciicast v2 from Start-ConsoleRecording or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Start-ConsoleDashboard widget updates into the same definition, preserving the existing dashboard HTML, SVG and asciicast v3 exports. Add #t=30000 to an HTML address to open at 30 seconds.

## EXAMPLES

### EXAMPLE 1
```powershell
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Svg -Destination .\run.svg
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Html -Destination .\run.html -Width 120 -Height 36
```

An animated SVG and an HTML player of the same run

## PARAMETERS

### -ChromeStyle
MacOS (default), Windows, Linux or None. Specifying a style enables chrome unless -WindowChrome:$false is explicit. None always hides chrome.

```yaml
Type: WindowChromeStyle
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: [ConsoleMonster.Export.WindowChromeStyle]::MacOS
Accept pipeline input: False
Accept wildcard characters: False
```

### -Dashboard
The dashboard (or widget tree) the recording was made with.

```yaml
Type: IWidget
Parameter Sets: Dashboard
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
Parameter Sets: Session, Dashboard
Aliases: OutFile
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinalFrame
With a session cast, exports the final visible frame as static SVG.

```yaml
Type: SwitchParameter
Parameter Sets: Session
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Format
Html, Svg, Asciicast, Text, Markdown, FencedText or PlayerData. The destination extension selects a default; .json means PlayerData. Text and Markdown apply to terminal sessions.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Html, Svg, Asciicast, PlayerData, Text, Markdown, FencedText

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
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Dashboard playback height in cells (default 30). Session casts retain their recorded dimensions.

```yaml
Type: Int32
Parameter Sets: Session, Dashboard
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
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Session .cast file, or dashboard .jsonl with -Dashboard.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Speed
Playback speed baked into the export (default 1).

```yaml
Type: Double
Parameter Sets: Session, Dashboard
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
Parameter Sets: Session, Dashboard
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
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Dashboard playback width in cells (default 100). Session casts retain their recorded dimensions.

```yaml
Type: Int32
Parameter Sets: Session, Dashboard
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
Parameter Sets: Session, Dashboard
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
