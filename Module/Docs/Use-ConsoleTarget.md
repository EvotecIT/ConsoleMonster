---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://consolemonster.dev/
schema: 2.0.0
---
# Use-ConsoleTarget
## SYNOPSIS
Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.

Inside the script block every ConsoleMonster command (Write-ConsoleText, Show-ConsoleTable, Write-ConsoleText -Markup, live dashboards, prompts) writes to the target instead of the console. With -Path the output goes to a UTF-8 file, with -Writer to a TextWriter, with -Target to a target object (ConsoleMonster.Output.ITerminalTarget); with none of them it is returned as one string. -Width, -Glyphs and -ColorDepth lay the output out for the file or string; prompts there have nobody to answer and use their defaults. The script block's own output is passed through.

## SYNTAX
### String (Default)
```powershell
Use-ConsoleTarget [-ScriptBlock] <scriptblock> [-Width <int>] [-Glyphs <GlyphTier>] [-ColorDepth <ColorDepth>] [<CommonParameters>]
```

### Path
```powershell
Use-ConsoleTarget [-ScriptBlock] <scriptblock> -Path <string> [-Append] [-Width <int>] [-Glyphs <GlyphTier>] [-ColorDepth <ColorDepth>] [<CommonParameters>]
```

### Writer
```powershell
Use-ConsoleTarget [-ScriptBlock] <scriptblock> -Writer <TextWriter> [-Width <int>] [-Glyphs <GlyphTier>] [-ColorDepth <ColorDepth>] [<CommonParameters>]
```

### Target
```powershell
Use-ConsoleTarget [-ScriptBlock] <scriptblock> -Target <ITerminalTarget> [-Width <int>] [-Glyphs <GlyphTier>] [-ColorDepth <ColorDepth>] [<CommonParameters>]
```

## DESCRIPTION
Runs a script block with ConsoleMonster output going somewhere else: a file, a text writer, any target, or a string.

Inside the script block every ConsoleMonster command (Write-ConsoleText, Show-ConsoleTable, Write-ConsoleText -Markup, live dashboards, prompts) writes to the target instead of the console. With -Path the output goes to a UTF-8 file, with -Writer to a TextWriter, with -Target to a target object (ConsoleMonster.Output.ITerminalTarget); with none of them it is returned as one string. -Width, -Glyphs and -ColorDepth lay the output out for the file or string; prompts there have nobody to answer and use their defaults. The script block's own output is passed through.

## EXAMPLES

### EXAMPLE 1
```powershell
Use-ConsoleTarget -Path .\report.txt -Width 120 { $servers | Show-ConsoleTable; Write-ConsoleText -Markup '[pass]done[/]' }
```

Save a report as plain text

### EXAMPLE 2
```powershell
$text = Use-ConsoleTarget -Width 80 { Get-Service | Select-Object -First 5 | Show-ConsoleTable }
```

Capture a table as a string

## PARAMETERS

### -Append
Appends to the file instead of replacing it.

```yaml
Type: SwitchParameter
Parameter Sets: Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ColorDepth
The colour depth for -Path, -Writer or the string (default None: plain text).

```yaml
Type: ColorDepth
Parameter Sets: String, Path, Writer, Target
Aliases: None
Possible values: None, Ansi16, Ansi256, TrueColor

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Glyphs
The glyph tier for -Path, -Writer or the string (default Ascii).

```yaml
Type: GlyphTier
Parameter Sets: String, Path, Writer, Target
Aliases: None
Possible values: Ascii, Basic, Full

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
A file to write to (UTF-8 without a byte order mark; replaced unless -Append).

```yaml
Type: String
Parameter Sets: Path
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ScriptBlock
The commands whose output goes to the target.

```yaml
Type: ScriptBlock
Parameter Sets: String, Path, Writer, Target
Aliases: None
Possible values:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Target
A target object (ConsoleMonster.Output.ITerminalTarget).

```yaml
Type: ITerminalTarget
Parameter Sets: Target
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Width
The width in cells for -Path, -Writer or the string (default 0: terminal width, falling back to 100).

```yaml
Type: Int32
Parameter Sets: String, Path, Writer, Target
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Writer
A TextWriter to write to (it stays open).

```yaml
Type: TextWriter
Parameter Sets: Writer
Aliases: None
Possible values:

Required: True
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
