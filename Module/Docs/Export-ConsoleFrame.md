---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Export-ConsoleFrame
## SYNOPSIS
Exports a widget, a rendered frame or a live-session snapshot to HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Format-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Other objects are shown as a table. Several widgets are stacked. Without -Path the document is written to the pipeline.

## SYNTAX
### __AllParameterSets
```powershell
Export-ConsoleFrame [-InputObject] <Object> [[-Path] <string>] [-Format <string>] [-Width <int>] [-Height <int>] [-Theme <ExportTheme>] [-Title <string>] [-WindowChrome] [-FontSize <double>] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Exports a widget, a rendered frame or a live-session snapshot to HTML, SVG, Markdown or text.

Pipe any New-Console* widget (or Format-ConsoleTable -AsWidget) and get a self-contained HTML page (inline CSS, selectable text, a dark and a light rendering that follow the viewer's prefers-color-scheme), an SVG image (box, block and braille characters drawn as shapes, selectable text, no external assets) or plain text. Cell geometry is kept exactly. OSC 8 links become links (web and mail links only; javascript: and other schemes are dropped). Other objects are shown as a table. Several widgets are stacked. Without -Path the document is written to the pipeline.

## EXAMPLES

### EXAMPLE 1
```powershell
Get-Service | Select-Object -First 8 Name, Status, StartType | Format-ConsoleTable -AsWidget | Export-ConsoleFrame -Path .\services.html
```

A table as an HTML page

### EXAMPLE 2
```powershell
$dashboard | Export-ConsoleFrame -Format Svg -Path .\run.svg -Width 120 -Height 36 -Theme Dark -Title 'Nightly run' -WindowChrome
```

A dashboard as an SVG in a window frame, dark only

## PARAMETERS

### -FontSize
The font size in pixels (default 14).

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
The format: Html, Svg, Markdown (a fenced preformatted block) or Text (default: from the -Path extension, including .md/.markdown, else Html).

```yaml
Type: String
Parameter Sets: __AllParameterSets
Aliases: None
Possible values: Html, Svg, Text, Markdown

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
The file to write (UTF-8 without a byte order mark). Without it the document goes to the pipeline.

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

### -Theme
Auto (both renderings, following the viewer), Dark or Light.

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
Width in cells for widgets (default 100).

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
- `System.IO.FileInfo`

## RELATED LINKS

- None
