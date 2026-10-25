---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Export-ConsoleRecording
## SYNOPSIS
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations; MP4 needs ffmpeg on PATH. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.

## SYNTAX
### Session (Default)
```powershell
Export-ConsoleRecording [-Path] <string> [[-Destination] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [-FinalFrame] [-Preset <string>] [-Fit <string>] [-Background <string>] [-BackgroundColor <string>] [-BackgroundEndColor <string>] [-FontFamily <string>] [-AtSeconds <double>] [-Chapter <string>] [-PerChapter] [-IdleLimitSeconds <double>] [-LoopCount <int>] [-Options <RecordingExportOptions>] [-MaxFontSize <double>] [-FontFallbacks <string[]>] [<CommonParameters>]
```

### Dashboard
```powershell
Export-ConsoleRecording [-Dashboard] <IWidget> [-Path] <string> [[-Destination] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [-Preset <string>] [-Fit <string>] [-Background <string>] [-BackgroundColor <string>] [-BackgroundEndColor <string>] [-FontFamily <string>] [-IdleLimitSeconds <double>] [-LoopCount <int>] [-Options <RecordingExportOptions>] [-MaxFontSize <double>] [-FontFallbacks <string[]>] [<CommonParameters>]
```

## DESCRIPTION
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations; MP4 needs ffmpeg on PATH. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.

## EXAMPLES

### EXAMPLE 1
```powershell
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Svg -Destination .\run.svg
Export-ConsoleRecording -Dashboard $dashboard -Path .\run.jsonl -Format Html -Destination .\run.html -Width 120 -Height 36
```

An animated SVG and an HTML player of the same run

## PARAMETERS

### -AtSeconds
Source recording time for PNG, before playback speed or idle compression.

```yaml
Type: Double
Parameter Sets: Session
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Background
Slide background: Theme (default), Solid or Gradient.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Theme, Solid, Gradient

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BackgroundColor
Solid background or gradient start as #RGB or #RRGGBB.

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

### -BackgroundEndColor
Gradient end as #RGB or #RRGGBB; omitted uses the theme background.

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

### -Chapter
Exports the completed view of this chapter as PNG.

```yaml
Type: String
Parameter Sets: Session
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChromeStyle
MacOS (default), Windows, Linux or None. Specifying a style enables chrome unless -WindowChrome:$false is explicit. None always hides chrome.

```yaml
Type: WindowChromeStyle
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: MacOS
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
The file to write, or a directory with -PerChapter. Graphics require a destination; text formats can go to the pipeline.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FinalFrame
With a session cast, exports the final visible frame as static SVG or PNG.

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

### -Fit
Slide fitting: Content trims unused rows and columns with two trailing columns of padding; Terminal keeps the complete grid. Animations use one stable size throughout the clip.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Content, Terminal

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FontFallbacks
Additional raster font-family fallbacks, in preferred order.

```yaml
Type: String[]
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FontFamily
Installed raster font; omitted uses embedded JetBrains Mono.

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

### -Format
Png, Gif, Apng, Mp4, Html, Svg, Asciicast, Text, Markdown, FencedText or PlayerData. The destination extension selects a default; .json means PlayerData. Text applies to terminal sessions; Markdown also exports dashboard recordings as timestamped fenced frames.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Html, Svg, Asciicast, PlayerData, Text, Markdown, FencedText, Png, Gif, Apng, Mp4

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

### -IdleLimitSeconds
Maximum unchanged-frame delay in graphics exports; zero preserves timing.

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

### -LoopCount
Total animation plays: zero repeats forever, one plays once.

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

### -MaxFontSize
Maximum raster font size in pixels; default 72.

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

### -Options
Typed C# RecordingExportOptions, including sampling limits, markers, custom themes and playback settings. Use this instead of individual playback settings; the object is not modified.

```yaml
Type: RecordingExportOptions
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

### -PerChapter
Writes numbered PNG slides to the destination directory, one per chapter.

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

### -Preset
Graphics canvas: Docs, Slide16x9, Slide4x3 or Social.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Docs, Slide16x9, Slide4x3, Social

Required: False
Position: named
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
Dark (default), Light or HighContrast.

```yaml
Type: ExportTheme
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Auto, Dark, Light, HighContrast

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
Dashboard playback width in cells (default 0: terminal width, falling back to 100). Session casts retain their recorded dimensions.

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
Draws a window frame with a title bar in HTML, SVG and graphics exports.

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
