---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Show-ConsoleImage
## SYNOPSIS
Shows an image in the console, at the size you ask for in cells.

Draws real pixels in terminals with an image protocol: Kitty and Ghostty (Kitty graphics), iTerm2 and WezTerm (iTerm2 inline images) and Windows Terminal (Sixel). Other terminals with 256 colours or more get half blocks. Redirected, CI, remote, 16-colour and no-colour output, the ISE and sessions with a transcript running get one line, [image: name WxH]. Get-ConsoleCapability shows the protocol in ImageProtocol. The aspect ratio is always kept. To put an image inside a panel or dashboard, use New-ConsoleImage.

## SYNTAX
### Path (Default)
```powershell
Show-ConsoleImage [[-Path] <string>] [-Protocol <ImageProtocol>] [-Label <string>] [-Width <int>] [-Height <int>] [<CommonParameters>]
```

### Bytes
```powershell
Show-ConsoleImage -Bytes <byte[]> [-Protocol <ImageProtocol>] [-Label <string>] [-Width <int>] [-Height <int>] [<CommonParameters>]
```

## DESCRIPTION
Shows an image in the console, at the size you ask for in cells.

Draws real pixels in terminals with an image protocol: Kitty and Ghostty (Kitty graphics), iTerm2 and WezTerm (iTerm2 inline images) and Windows Terminal (Sixel). Other terminals with 256 colours or more get half blocks. Redirected, CI, remote, 16-colour and no-colour output, the ISE and sessions with a transcript running get one line, [image: name WxH]. Get-ConsoleCapability shows the protocol in ImageProtocol. The aspect ratio is always kept. To put an image inside a panel or dashboard, use New-ConsoleImage.

## EXAMPLES

### EXAMPLE 1
```powershell
Show-ConsoleImage -Path ./chart.png -Width 40
```

Show a chart 40 cells wide

### EXAMPLE 2
```powershell
Get-ChildItem ./screens -Filter *.png | Show-ConsoleImage -Height 12
```

Preview every screenshot in a folder

### EXAMPLE 3
```powershell
Show-ConsoleImage -Path ./logo.png -Width 24 -Protocol HalfBlock
```

Use half blocks, for example over a connection that drops image sequences

### EXAMPLE 4
```powershell
Show-ConsoleImage -Path ./diagram.png -Protocol Kitty
```

Use Kitty graphics in a terminal that supports it but isn't detected

## PARAMETERS

### -Bytes
Encoded image bytes, for example a chart rendered to PNG in memory.

```yaml
Type: Byte[]
Parameter Sets: Bytes
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Height
Height in cells. With -Width as well, the image fits inside that box; the aspect ratio is always kept.

```yaml
Type: Int32
Parameter Sets: Path, Bytes
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Label
The text in the placeholder that redirected and CI output get, [image: label WxH]. Defaults to the file name.

```yaml
Type: String
Parameter Sets: Path, Bytes
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A PNG, JPEG, GIF, BMP, TIFF or PPM file. Files from Get-ChildItem and path strings can also be piped in.

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Protocol
The protocol your terminal supports, when detection chose another one: Kitty, ITerm2, Sixel, HalfBlock or None (the placeholder). Redirected and CI output keep the placeholder.

```yaml
Type: ImageProtocol
Parameter Sets: Path, Bytes
Aliases: None
Possible values: None, HalfBlock, Sixel, ITerm2, Kitty

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
Width in cells. Without -Width or -Height the image takes one cell per 10 pixels, up to the window width.

```yaml
Type: Int32
Parameter Sets: Path, Bytes
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

- `None`

## RELATED LINKS

- None
