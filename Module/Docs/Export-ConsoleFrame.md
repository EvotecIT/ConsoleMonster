---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Export-ConsoleFrame
## SYNOPSIS
Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Show-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Markdown uses the bundled ConsoleMonster.Markdown add-on lazily: tables, records and trees retain their semantics; FencedText preserves the terminal layout. Other objects use the automatic view. Several widgets are stacked. Without -Path the document is written to the pipeline.

## SYNTAX
### __AllParameterSets
```powershell
Export-ConsoleFrame [-InputObject] <Object> [[-Path] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-ChromeStyle <WindowChromeStyle>] [-FontSize <double>] [-PassThru] [-Preset <string>] [-Fit <string>] [-Background <string>] [-BackgroundColor <string>] [-BackgroundEndColor <string>] [-FontFamily <string>] [<CommonParameters>]
```

## DESCRIPTION
Exports a widget, a rendered frame or a live-session snapshot to PNG, HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Show-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Markdown uses the bundled ConsoleMonster.Markdown add-on lazily: tables, records and trees retain their semantics; FencedText preserves the terminal layout. Other objects use the automatic view. Several widgets are stacked. Without -Path the document is written to the pipeline.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Show-ConsoleTable -AsWidget | Export-ConsoleFrame -Path .\services.html
```

A table as an HTML page

### EXAMPLE 2
```powershell
$dashboard | Export-ConsoleFrame -Format Svg -Path .\run.svg -Width 120 -Height 36 -Theme Dark -Title 'Nightly run' -WindowChrome
```

A dashboard as an SVG in a window frame, dark only

## PARAMETERS

### -Background
Slide background: Theme (default), Solid or Gradient.

```yaml
Type: String
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: None, MacOS, Windows, Linux

Required: False
Position: named
Default value: MacOS
Accept pipeline input: False
Accept wildcard characters: False
```

### -Fit
Slide fitting: Content trims unused rows and columns with two trailing columns of padding; Terminal keeps the complete grid. Animations use one stable size throughout the clip.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Content, Terminal

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
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FontSize
Natural-size font in pixels (default 14). Slide presets fit automatically, up to 72 pixels.

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

### -Format
The format: Png, Html, Svg, Markdown (semantic tables, records and trees), FencedText (a preformatted block) or Text (default: from the -Path extension, including .md/.markdown, else Html).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Html, Svg, Text, Markdown, FencedText, Png

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Height in cells for widgets (default: as tall as the widget measures).

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

### -InputObject
A widget, a rendered frame (CellBuffer), a live session, text, or objects to show as a table.

```yaml
Type: Object
Parameter Sets: __AllParameterSets
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -PassThru
With -Path, also writes the file to the pipeline.

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
The file to write (binary PNG, or UTF-8 text without a byte order mark). Without it PNG bytes or the text document go to the pipeline.

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

### -Preset
Graphics canvas: Docs, Slide16x9, Slide4x3 or Social.

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Docs, Slide16x9, Slide4x3, Social

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Theme
Auto (both HTML renderings), Dark, Light or HighContrast. Raster Auto uses Dark.

```yaml
Type: ExportTheme
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Auto, Dark, Light, HighContrast

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The page title and, with -WindowChrome, the title bar text.

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
Width in cells for widgets (default 0: terminal width, falling back to 100).

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
Draws a window frame with a title bar around the screen.

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

- `System.Object`

## OUTPUTS

- `System.String`
- `System.Byte[]`
- `System.IO.FileInfo`

## RELATED LINKS

- None
