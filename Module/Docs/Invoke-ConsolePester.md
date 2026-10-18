---
external help file: ConsoleMonster-help.xml
Module Name: ConsoleMonster
online version: https://github.com/EvotecIT/ConsoleMonster
schema: 2.0.0
---
# Invoke-ConsolePester
## SYNOPSIS
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.

## SYNTAX
### Simple (Default)
```powershell
Invoke-ConsolePester [[-Path] <string[]>] [-ExcludePath <string[]>] [-TagFilter <string[]>] [-ExcludeTagFilter <string[]>] [-FullNameFilter <string[]>] [-Container <Object[]>] [-Output <string>] [-CI] [-PassThru] [-FullScreen] [-ExportHtml <string>] [-Title <string>] [-NoLiveView] [<CommonParameters>]
```

### Advanced
```powershell
Invoke-ConsolePester -Configuration <Object> [-FullScreen] [-ExportHtml <string>] [-Title <string>] [-NoLiveView] [<CommonParameters>]
```

## DESCRIPTION
Runs Pester tests with a live ConsoleMonster dashboard and a summary table.

Takes the parameters of Invoke-Pester (Path, ExcludePath, TagFilter, ExcludeTagFilter, FullNameFilter, Container, Output, CI, PassThru) or a -Configuration (a PesterConfiguration or a hashtable), and runs Pester 5 or 6, imported when first needed. While the tests run, an inline area (or the full screen with -FullScreen) shows the run totals and the clock, the test files as lanes with passed, failed and skipped counts, the running test, and with -FullScreen the failures (expected against actual, the file and line as a link), the slowest tests and a histogram of durations. When the run ends a table of results by file, the failures and the totals are written to the scrollback, and -ExportHtml saves the final dashboard. In redirected output, jobs and CI logs the output is one line per failure and a summary line. Pester's result object is returned with -PassThru (or Run.PassThru), Run.Throw and Run.Exit (-CI) are applied after the summary, and Write-Host output from tests appears above the inline area. Pester has no public reporter API, so the live view uses Pester's internal plugin steps through a guarded adapter; when the internals are not what it expects (or with Run.Parallel or Output None) Pester runs unchanged with its own output and the summary table follows. Works on Windows PowerShell 5.1 and PowerShell 7.

## EXAMPLES

### EXAMPLE 1
```powershell
Invoke-ConsolePester -Path @('C:\Path')
```


### EXAMPLE 2
```powershell
Invoke-ConsolePester -Configuration 'Value'
```


## PARAMETERS

### -CI
CI mode as in Invoke-Pester: test results are written and the process exits with the failure count.

```yaml
Type: SwitchParameter
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Configuration
A PesterConfiguration (New-PesterConfiguration) or a hashtable with the same sections.

```yaml
Type: Object
Parameter Sets: Advanced
Aliases: None
Possible values:

Required: True
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Container
Containers from New-PesterContainer (Run.Container).

```yaml
Type: Object[]
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludePath
Files or folders to leave out (Run.ExcludePath).

```yaml
Type: String[]
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExcludeTagFilter
Leave out tests with these tags (Filter.ExcludeTag).

```yaml
Type: String[]
Parameter Sets: Simple
Aliases: ExcludeTag
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ExportHtml
Saves the final dashboard as an HTML page.

```yaml
Type: String
Parameter Sets: Simple, Advanced
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FullNameFilter
Run only tests whose full name matches (Filter.FullName).

```yaml
Type: String[]
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FullScreen
Shows the dashboard on the full screen (alternate screen) instead of inline.

```yaml
Type: SwitchParameter
Parameter Sets: Simple, Advanced
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -NoLiveView
Runs Pester unchanged with its own output, followed by the summary table (no adapter).

```yaml
Type: SwitchParameter
Parameter Sets: Simple, Advanced
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Output
Pester's output verbosity (Output.Verbosity). None runs quietly: no dashboard and no summary.

```yaml
Type: String
Parameter Sets: Simple
Aliases: None
Possible values: Diagnostic, Detailed, Normal, Minimal, None

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Returns Pester's result object.

```yaml
Type: SwitchParameter
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Path
Test files or folders (Run.Path); default the current folder.

```yaml
Type: String[]
Parameter Sets: Simple
Aliases: None
Possible values:

Required: False
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -TagFilter
Run only tests with these tags (Filter.Tag).

```yaml
Type: String[]
Parameter Sets: Simple
Aliases: Tag
Possible values:

Required: False
Position: named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Title
The dashboard title (default Pester).

```yaml
Type: String
Parameter Sets: Simple, Advanced
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

- `System.Management.Automation.PSObject`

## RELATED LINKS

- None
