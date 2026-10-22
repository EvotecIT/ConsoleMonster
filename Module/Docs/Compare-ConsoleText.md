---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Compare-ConsoleText
## SYNOPSIS
Compares text or files with numbered lines, highlighted words and collapsed unchanged regions.

Shows the reference and difference side by side at 120 columns or more, and inline in narrower terminals. Text headings default to Before and After; file headings use the resolved filenames. ReferenceName and DifferenceName override these headings in both the widget and unified output. Minus and plus markers distinguish changes without colour. Context controls the unchanged lines around changes. CRLF, LF and CR endings compare equally; a missing final newline remains significant. Large inputs use bounded comparison work and linear auxiliary memory, so a large changed range may produce a non-minimal diff. AsUnified returns plain unified diff text instead of rendering.

## SYNTAX
### Text (Default)
```powershell
Compare-ConsoleText [-ReferenceText] <string> [-DifferenceText] <string> [-ReferenceName <string>] [-DifferenceName <string>] [-Context <int>] [-IgnoreWhitespace] [-AsUnified] [<CommonParameters>]
```

### Path
```powershell
Compare-ConsoleText -ReferencePath <string> -DifferencePath <string> [-ReferenceName <string>] [-DifferenceName <string>] [-Context <int>] [-IgnoreWhitespace] [-AsUnified] [<CommonParameters>]
```

## DESCRIPTION
Compares text or files with numbered lines, highlighted words and collapsed unchanged regions.

Shows the reference and difference side by side at 120 columns or more, and inline in narrower terminals. Text headings default to Before and After; file headings use the resolved filenames. ReferenceName and DifferenceName override these headings in both the widget and unified output. Minus and plus markers distinguish changes without colour. Context controls the unchanged lines around changes. CRLF, LF and CR endings compare equally; a missing final newline remains significant. Large inputs use bounded comparison work and linear auxiliary memory, so a large changed range may produce a non-minimal diff. AsUnified returns plain unified diff text instead of rendering.

## EXAMPLES

### EXAMPLE 1
```powershell
Compare-ConsoleText -ReferenceText $before -DifferenceText $after -Context 1
```

Compare configuration strings with one line of context.

### EXAMPLE 2
```powershell
Compare-ConsoleText -ReferencePath .\before.txt -DifferencePath .\after.txt -IgnoreWhitespace
```

Compare files using the current PowerShell location and ignore whitespace within lines.

### EXAMPLE 3
```powershell
Compare-ConsoleText -ReferenceText $before -DifferenceText $after -ReferenceName 'Deployed' -DifferenceName 'Proposed' -AsUnified | Set-Content .\changes.diff
```

Return a unified diff with custom side names for storage or further processing.

## PARAMETERS

### -AsUnified
Returns a single plain unified diff string with LF separators and hunk ranges. Equal texts return an empty string. Without this switch, writes a widget to the host and returns no objects.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Context
The nonnegative number of unchanged lines shown before and after each change. Default three; zero shows changes only.

```yaml
Type: Int32
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DifferenceName
The new-text heading in the widget and unified output. Overrides the resolved difference filename for files or After for text. Control characters are replaced by spaces.

```yaml
Type: String
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DifferencePath
The new file's PowerShell path. The provider must be FileSystem and the path must resolve to exactly one readable file. Files are read as UTF-8 with BOM detection.

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

### -DifferenceText
The new text. Empty strings and null represent an empty document.

```yaml
Type: String
Parameter Sets: Text
Aliases: None
Possible values:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -IgnoreWhitespace
Ignores Unicode whitespace within lines. Line boundaries and a missing final newline are still compared.

```yaml
Type: SwitchParameter
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReferenceName
The original-text heading in the widget and unified output. Overrides the resolved reference filename for files or Before for text. Control characters are replaced by spaces.

```yaml
Type: String
Parameter Sets: Text, Path
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ReferencePath
The original file's PowerShell path, relative to the current location or qualified by a filesystem drive. Wildcards must resolve to exactly one file.

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

### -ReferenceText
The original text. Empty strings and null represent an empty document.

```yaml
Type: String
Parameter Sets: Text
Aliases: None
Possible values:

Required: True
Position: 0
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

## RELATED LINKS

- None
