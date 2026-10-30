---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Export-ConsoleRecording
## SYNOPSIS
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations without an external executable. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.

## SYNTAX
### Session (Default)
```powershell
Export-ConsoleRecording [-RecordingPath] <string> [[-Path] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [-FinalFrame] [-Preset <string>] [-Fit <string>] [-Background <string>] [-BackgroundColor <string>] [-BackgroundEndColor <string>] [-FontFamily <string>] [-AtSeconds <double>] [-Chapter <string>] [-PerChapter] [-IdleLimitSeconds <double>] [-LoopCount <int>] [-MaxFontSize <double>] [-FontFallbacks <string[]>] [-Pace <RecordingPace>] [-ChapterHoldSeconds <double>] [-HoldLastFrameSeconds <double>] [-TrimStartSeconds <double>] [-TrimEndSeconds <double>] [-TrimStartChapter <string>] [-TrimEndChapter <string>] [-ChapterSpeeds <hashtable>] [<CommonParameters>]
```

### Dashboard
```powershell
Export-ConsoleRecording [-Dashboard] <IWidget> [-RecordingPath] <string> [[-Path] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-FramesPerSecond <double>] [-Speed <double>] [-Theme <ExportTheme>] [-Title <string>] [-ChromeStyle <WindowChromeStyle>] [-PassThru] [-Preset <string>] [-Fit <string>] [-Background <string>] [-BackgroundColor <string>] [-BackgroundEndColor <string>] [-FontFamily <string>] [-IdleLimitSeconds <double>] [-LoopCount <int>] [-MaxFontSize <double>] [-FontFallbacks <string[]>] [-Pace <RecordingPace>] [-ChapterHoldSeconds <double>] [-HoldLastFrameSeconds <double>] [-TrimStartSeconds <double>] [-TrimEndSeconds <double>] [-TrimStartChapter <string>] [-TrimEndChapter <string>] [-ChapterSpeeds <hashtable>] [<CommonParameters>]
```

## DESCRIPTION
Exports a terminal session or dashboard recording to slide images, animations, video, a player or asciicast.

PNG selects the final frame, -AtSeconds a source time, -Chapter a completed chapter, or -PerChapter numbered slides. GIF and APNG are managed animations without an external executable. Without -Dashboard, reads a whole-session asciicast v2 from Invoke-ConsoleRecording -Interactive or Invoke-ConsoleRecording. Html writes a self-contained player with chapters, speed, scrubbing and copy text; Svg is animated; Text and Markdown include scrollback; -FinalFrame writes a static SVG. With -Dashboard, replays Invoke-ConsoleDashboard widget updates into the same definition, preserving the dashboard HTML, SVG and asciicast v3 exports; Markdown writes timestamped fenced frames through the bundled add-on. Add #t=30000 to an HTML address to open at 30 seconds.

## EXAMPLES

### EXAMPLE 1
```powershell
# Requires an existing session.cast recorded by Invoke-ConsoleRecording.
            Export-ConsoleRecording -RecordingPath ./session.cast -Format Text
```

Return a text document from an existing terminal recording

### EXAMPLE 2
```powershell
# Requires run.jsonl recorded with this dashboard definition.
            $dashboard = New-ConsoleDashboard -Title 'Build' -Content { New-ConsoleStatusList -WidgetId steps -Content { 'Compile' } }
Export-ConsoleRecording -Dashboard $dashboard -RecordingPath ./run.jsonl -Format Svg
Export-ConsoleRecording -Dashboard $dashboard -RecordingPath ./run.jsonl -Format Html -Width 120 -Height 36
```

Return SVG and HTML from an existing matching dashboard recording

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

### -ChapterHoldSeconds
Extra export hold at every chapter marker, unaffected by speed or idle capping.

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

### -ChapterSpeeds
Hashtable of chapter labels to positive speed multipliers, for example @{ Install = 4; Results = 1 }. Multiplies Speed for source and reading delays; explicit holds retain their duration. Labels must be unique in the recording.

```yaml
Type: Hashtable
Parameter Sets: Session, Dashboard
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ChromeStyle
None (default), MacOS, Windows or Linux. Choose a window style to show chrome; None hides it.

```yaml
Type: WindowChromeStyle
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: None
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
Png, Gif, Apng, Html, Svg, Asciicast, Text, Markdown, FencedText or PlayerData. The destination extension selects a default; .json means PlayerData. Text applies to terminal sessions; Markdown also exports dashboard recordings as timestamped fenced frames.

```yaml
Type: String
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Html, Svg, Asciicast, PlayerData, Text, Markdown, FencedText, Png, Gif, Apng

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

### -HoldLastFrameSeconds
Final screen hold after the source timeline and reading holds (default 2 seconds).

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

### -IdleLimitSeconds
Maximum source delay on an unchanged screen; zero disables capping. Reading and explicit holds are never capped.

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

### -Pace
Slow, Normal (default), Fast or Source. Adds reading time after output settles. Explicitly specifying Pace overrides recorded demo pace directives. Source adds no reading time; use IdleLimitSeconds 0 to retain all source delays.

```yaml
Type: RecordingPace
Parameter Sets: Session, Dashboard
Aliases: None
Possible values: Source, Slow, Normal, Fast

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
With -Path, also writes the file to the pipeline.

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

### -RecordingPath
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

### -TrimEndChapter
End before this chapter marker. Its output and hold are excluded.

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

### -TrimEndSeconds
Exclusive source position at which to end. Cannot be combined with TrimEndChapter.

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

### -TrimStartChapter
Begin at this chapter marker, retaining its hold and all later output.

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

### -TrimStartSeconds
Inclusive source position at which to begin. Cannot be combined with TrimStartChapter.

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

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

- `System.Management.Automation.PSObject`

## OUTPUTS

- `System.String`
- `System.IO.FileInfo`

## RELATED LINKS

- None
