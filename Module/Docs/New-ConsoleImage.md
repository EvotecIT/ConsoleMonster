---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# New-ConsoleImage
## SYNOPSIS
Creates an image to place inside panels, columns and dashboards.

Inside layouts and live dashboards the image is drawn with half blocks on terminals with 256 colours or more, so it redraws cleanly with the rest of the screen; redirected and no-colour output gets [image: name WxH]. To print an image on its own with the terminal's real pixels, use Show-ConsoleImage.

## SYNTAX
### Path (Default)
```powershell
New-ConsoleImage [[-Path] <string>] [-Label <string>] [-Width <int>] [-Height <int>] [<CommonParameters>]
```

### Bytes
```powershell
New-ConsoleImage -Bytes <byte[]> [-Label <string>] [-Width <int>] [-Height <int>] [<CommonParameters>]
```

## DESCRIPTION
Creates an image to place inside panels, columns and dashboards.

Inside layouts and live dashboards the image is drawn with half blocks on terminals with 256 colours or more, so it redraws cleanly with the rest of the screen; redirected and no-colour output gets [image: name WxH]. To print an image on its own with the terminal's real pixels, use Show-ConsoleImage.

## EXAMPLES

### EXAMPLE 1
```powershell
New-ConsoleColumns -Content { New-ConsolePanel -Title 'Latency' -Content { New-ConsoleImage -Path ./latency.png -Width 40 }; New-ConsolePanel -Title 'Summary' -Content { New-ConsoleKeyValue -InputObject ([ordered] @{ P50 = '41 ms'; P99 = '180 ms' }) } } | Show-Console
```

A chart next to its numbers

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

- `ConsoleMonster.Graphics.ConsoleImage`

## RELATED LINKS

- None
