Describe 'Rich text cmdlets' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        Import-Module $script:ModulePath -Force

        # Runs a script in a child process of the same runtime with redirected output (the plain host route), with every
        # capability input cleared and en-US culture, so the output is deterministic plain text.
        function Invoke-TextChild {
            param([Parameter(Mandatory)][string] $Script)
            $lines = [System.Collections.Generic.List[string]]::new()
            $lines.Add("foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'COLORTERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }")
            $lines.Add("[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('en-US')")
            $lines.Add("Import-Module '$($script:ModulePath)' -Force")
            $lines.Add($Script)
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }

        # Runs a script in a background job (the coloured-records host route) and returns its output as strings.
        function Invoke-TextJob {
            param([Parameter(Mandatory)][scriptblock] $ScriptBlock)
            $job = Start-Job -ArgumentList $script:ModulePath, $ScriptBlock.ToString() -ScriptBlock {
                param($ModulePath, $Body)
                foreach ($name in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_MODE', 'TERM') { [Environment]::SetEnvironmentVariable($name, $null) }
                Import-Module $ModulePath -Force
                & ([scriptblock]::Create($Body))
            }
            $null = Wait-Job $job -Timeout 180
            $result = @(Receive-Job $job | ForEach-Object { "$_" })
            Remove-Job $job -Force
            $result
        }
    }

    Context 'Write-ConsoleText with Write-Color parameters' {
        It 'writes the pieces on one line with literal foreground and background colors' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText -Text 'Red ', 'Green ', 'Yellow' -Color Red, Green, Yellow
Write-ConsoleText -Text 'color ', 'pieces' -Color Yellow -BackgroundColor Blue
Write-ConsoleText -Text 'foreground' -Color Cyan -BackgroundColor DarkGray
Write-ConsoleText 'indent' -Indent 8
'@
            $text | Should -Match '(?m)^Red Green Yellow$'
            $text | Should -Match '(?m)^color pieces$'
            $text | Should -Match '(?m)^foreground$'
            $text | Should -Match '(?m)^ {8}indent$'
        }

        It 'applies Indent and keeps NoNewLine output on the same line' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText 'spaced' -Indent 3
Write-ConsoleText 'no ' -NoNewLine
Write-ConsoleText 'newline'
'@
            $lines = @($text -split "`n")
            $lines | Should -Contain '   spaced'
            $lines | Should -Contain 'no newline'
        }

        It 'shows the time with ShowTimestamp and TimestampFormat' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText 'timed' -ShowTimestamp -TimestampFormat 'yyyy'
Write-ConsoleText 'alias' -ShowTimestamp -TimestampFormat 'MM'
Write-ConsoleText 'alias2' -ShowTimestamp -TimestampFormat 'dd'
'@
            $text | Should -Match ('(?m)^\[' + (Get-Date).ToString('yyyy') + '\] timed$')
            $text | Should -Match ('(?m)^\[' + (Get-Date).ToString('MM') + '\] alias$')
            $text | Should -Match ('(?m)^\[' + (Get-Date).ToString('dd') + '\] alias2$')
        }

        It 'appends literal UTF-8 text to LogFile while preserving console output' {
            $log = Join-Path $TestDrive 'basic.log'
            $text = Invoke-TextChild -Script @"
Write-ConsoleText 'zażółć' -LogFile '$log'
Write-ConsoleText 'second line' -LogFile '$log'
"@
            $text | Should -Match 'zażółć'
            $text | Should -Match 'second line'
            $content = @(Get-Content -LiteralPath $log -Encoding UTF8)
            $content.Count | Should -Be 2
            $content[0] | Should -Match '^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] zażółć$'
            $content[1] | Should -Match '^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] second line$'
        }

        It 'aligns complete lines by display width including prefixes emoji and CJK' {
            $text = Invoke-TextChild -Script @'
$env:CONSOLEMONSTER_GLYPHS = 'Full'
Write-ConsoleText 'abcd' -Alignment Center -Width 20
Write-ConsoleText ([string] [char] 0x4E2D + [char] 0x6587 + [char]::ConvertFromUtf32(0x1F98A)) -Alignment Center -Width 20
Write-ConsoleText 'x' -Alignment Right -Indent 2 -Width 10
Write-ConsoleText 'abcd' -Alignment Center -Indent 2 -Width 10
Write-ConsoleText 'one two three four five six seven' -Alignment Right -Indent 2 -ShowTimestamp -TimestampFormat "'T'" -Wrap -Width 20
'@
            $lines = $text -split "`n"
            $lines[0] | Should -BeExactly ((' ' * 8) + 'abcd')
            # Two CJK characters (2 cells each) and an emoji without a fallback (2 cells) are 6 cells wide: 7 spaces before.
            $lines[1] | Should -BeExactly ((' ' * 7) + [string] [char] 0x4E2D + [char] 0x6587 + [char]::ConvertFromUtf32(0x1F98A))
            $lines[2] | Should -BeExactly ((' ' * 9) + 'x')
            $lines[3] | Should -BeExactly '    abcd'
            $lines[4] | Should -BeExactly '   [T] one two three'
            $lines[5] | Should -BeExactly '       four five six'
            $lines[6] | Should -BeExactly ((' ' * 15) + 'seven')
        }









        It 'maps ConsoleColor names exactly as Write-Host shows them' {
            (New-ConsoleText 'x' -Color Red).Segments[0].Style.Color.ToMarkup() | Should -Be 'red'
            (New-ConsoleText 'x' -Color DarkRed).Segments[0].Style.Color.ToMarkup() | Should -Be 'maroon'
            (New-ConsoleText 'x' -Color Green).Segments[0].Style.Color.ToMarkup() | Should -Be 'lime'
            (New-ConsoleText 'x' -Color Gray).Segments[0].Style.Color.ToMarkup() | Should -Be 'silver'
            (New-ConsoleText 'x' -Color DarkGray).Segments[0].Style.Color.ToMarkup() | Should -Be 'grey'
            (New-ConsoleText 'x' -Color '#ff8800').Segments[0].Style.Color.ToMarkup() | Should -Be '#ff8800'
            (New-ConsoleText 'x' -Role Pass).Segments[0].Style.Role.ToString() | Should -Be 'Pass'
            { New-ConsoleText 'x' -Color 'NotAColour' } | Should -Throw '*is not a colour*'
        }

        It 'gives extra pieces the first colour, as Write-Color does' {
            $text = New-ConsoleText 'a', 'b', 'c' -Color Yellow, Green
            ($text.Segments | ForEach-Object { $_.Style.Color.ToMarkup() }) -join ',' | Should -Be 'yellow,lime,yellow'
        }

        It 'carries the piece colours to Receive-Job as Write-Host colours' {
            $records = Invoke-TextJob -ScriptBlock {
                Write-ConsoleText 'red ', 'darkgreen ', 'role' -Color Red, DarkGreen, Fail 6>&1 | ForEach-Object { $_.MessageData.Message + '|' + $_.MessageData.ForegroundColor }
            }
            ($records -join "`n") | Should -Match 'red \|Red'
            ($records -join "`n") | Should -Match 'darkgreen \|DarkGreen'
            ($records -join "`n") | Should -Match 'role\|Red'
        }
    }

    Context 'Write-ConsoleText beyond Write-Color' {
        It 'aligns -SplitLines under the first line after the time stamp' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText '[info] ', "Execution Path exists`n2. Line`n3. Line" -Color Yellow, White -ShowTimestamp -TimestampFormat 'HH:mm' -SplitLines "`n"
'@
            $lines = $text -split "`n"
            $lines[0] | Should -Match '^\[\d\d:\d\d\] \[info\] Execution Path exists$'
            $lines[1] | Should -BeExactly ((' ' * 8) + '2. Line')
            $lines[2] | Should -BeExactly ((' ' * 8) + '3. Line')
        }



        It 'wraps with -Wrap under the prefix' {
            $text = Invoke-TextChild -Script "Write-ConsoleText 'one two three four five six seven' -Indent 4 -Wrap -Width 20"
            $lines = $text -split "`n"
            $lines[0] | Should -BeExactly '    one two three'
            $lines[1] | Should -BeExactly '    four five six'
            $lines[2] | Should -BeExactly '    seven'
        }

        It 'mixes literal pieces with -Markup, keeping data literal' {
            $text = Invoke-TextChild -Script "Write-ConsoleText 'data [red]x[/] ' -Markup '[bold]markup[/] [pass]ok[/]'"
            $text | Should -Match '(?m)^data \[red\]x\[/\] markup ok$'
        }



        It 'never interprets data: brackets and escape sequences stay literal and are stripped of controls' {
            $text = Invoke-TextChild -Script @'
$esc = [char] 27
Write-ConsoleText ('[red]alert[/] ' + $esc + ']0;PWNED' + [char] 7 + ' end')
[pscustomobject] @{ Name = '[bold]x[/]' + $esc + '[2J' } | Show-ConsoleTable -Width 40
'@
            $text | Should -Match '\[red\]alert\[/\]'
            $text | Should -Match '\[bold\]x\[/\]'
            $text.IndexOf([char] 27) | Should -Be -1
            $text.IndexOf([char] 7) | Should -Be -1
        }
    }

    Context 'New-ConsoleText in widgets' {
        It 'works in Show-ConsoleTable cells through calculated properties, with a rich header' {
            $text = Invoke-TextChild -Script @'
@(
    [pscustomobject] @{ Name = 'SPF'; Status = 'pass' }
    [pscustomobject] @{ Name = 'DKIM'; Status = 'fail' }
) | Show-ConsoleTable -Width 50 -Title (New-ConsoleText 'Checks' -Color Cyan) -Property @{ Name = 'Name'; Header = (New-ConsoleText 'Na', 'me' -Color Cyan, Magenta) }, @{ Name = 'Result'; Expression = { if ($_.Status -eq 'pass') { New-ConsoleText 'PASS' -Role Pass } else { New-ConsoleText 'FAIL' -Role Fail -Decoration Bold } } }
'@
            $text | Should -Match 'Checks'
            $text | Should -Match 'Name\s+.\s+Result'
            $text | Should -Match 'SPF\s+.\s+PASS'
            $text | Should -Match 'DKIM\s+.\s+FAIL'
        }

        It 'keeps the cell colours on the coloured host route' {
            $records = Invoke-TextJob -ScriptBlock {
                # The table is the last command of its own pipeline, so it is drawn through the job's host (piped on, it would be text).
                & { [pscustomobject] @{ Name = 'SPF' } | Show-ConsoleTable -Property Name, @{ Name = 'Result'; Expression = { New-ConsoleText 'PASS' -Color Green } } } 6>&1 | ForEach-Object { $_.MessageData.Message.Trim() + '|' + $_.MessageData.ForegroundColor }
            }
            ($records -join "`n") | Should -Match 'PASS\|Green'
        }

        It 'opts a string column into markup with Markup = $true only' {
            $text = Invoke-TextChild -Script "[pscustomobject] @{ Literal = '[bold]a[/]'; Rich = '[bold]b[/]' } | Show-ConsoleTable -Width 40 -Property Literal, @{ Name = 'Rich'; Markup = `$true }"
            $text | Should -Match '\[bold\]a\[/\]'
            $text | Should -Match '(?m)\bb\s'
            $text | Should -Not -Match '\[bold\]b'
        }

        It 'accepts rich text for panel titles, lanes and log lines' {
            $text = Invoke-TextChild -Script @'
$d = New-ConsoleDashboard -Title (New-ConsoleText 'Rich ', 'dashboard' -Color Cyan, Yellow) -Content { New-ConsoleLanes -WidgetId lanes -Content { (New-ConsoleText 'SRV', '01' -Color Cyan, Yellow) }; New-ConsoleLogRegion }
Invoke-ConsoleDashboard $d {
    Update-Console -Key SRV01 -Status Running -Detail (New-ConsoleText 'copying ', 'files' -Color Gray, Green) -WidgetId 'lanes'
    Write-ConsoleLogEntry -Message (New-ConsoleText 'rich ', 'log' -Color Yellow, Red)
    Update-Console -Key SRV01 -Result Pass -WidgetId 'lanes'
    Update-Console -Key SRV01 -Complete -WidgetId 'lanes'
}
New-ConsolePanel -Title (New-ConsoleText 'Panel ', 'title' -Color Cyan, Green) -Content { New-ConsoleText 'body' } | Show-Console -Width 40
'@
            $text | Should -Match 'rich log'
            $text | Should -Match 'SRV01'
            $text | Should -Match 'Panel title'
            $text | Should -Not -Match '\[(aqua|yellow|lime)\]'
        }
    }

    Context 'Show-ConsoleStatusLine outside a console' {
        It 'writes one line per change and the final line, without the spinner' {
            $text = Invoke-TextChild -Script @'
Show-ConsoleStatusLine 'step one' -Spinner
Show-ConsoleStatusLine 'step one' -Spinner
Show-ConsoleStatusLine 'step two' -Spinner
Show-ConsoleStatusLine 'finished' -Status Pass -Complete
'@
            $lines = @($text -split "`n" | Where-Object { $_ })
            @($lines | Where-Object { $_ -eq 'step one' }).Count | Should -Be 1
            $lines | Should -Contain 'step two'
            $lines[-1] | Should -Match 'finished$'
        }
    }

    Context 'One status vocabulary' {
        It 'accepts PowerShell''s status words for -Status and lists the vocabulary for an unknown one' {
            $text = Invoke-TextChild -Script @'
Show-ConsoleStatusLine 'deployed' -Status Success -Complete
Show-ConsoleStatusLine 'slow' -Status Warning -Complete
Show-ConsoleStatusLine 'broken' -Status Failure -Complete
Show-ConsoleStatusLine 'noted' -Status Information -Complete
try { Show-ConsoleStatusLine 'x' -Status Bogus } catch { 'ERROR=' + $_.Exception.Message }
'@
            # Each line carries its status marker (the glyphs depend on the console's tier).
            $lines = @($text -split "`n")
            $lines -match '^\S+ deployed$' | Should -Not -BeNullOrEmpty
            $lines -match '^\S+ slow$' | Should -Not -BeNullOrEmpty
            $lines -match '^\S+ broken$' | Should -Not -BeNullOrEmpty
            $pass = (@($lines | Where-Object { $_ -match 'deployed' })[0] -split ' ')[0]
            $fail = (@($lines | Where-Object { $_ -match 'broken' })[0] -split ' ')[0]
            $pass | Should -Not -Be $fail
            $text | Should -Match "ERROR=.*'Bogus' is not a status\. Use one of: .*Success.*Warning"
        }

        It 'maps PowerShell''s words to log levels for Write-ConsoleLogEntry and completes them' {
            $transform = ((Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.Binding.LogLevelTransformationAttribute'))::new()
            $transform.Transform($null, 'Information') | Should -Be 'info'
            $transform.Transform($null, 'Warning') | Should -Be 'warn'
            $transform.Transform($null, 'Error') | Should -Be 'err'
            $transform.Transform($null, 'Verbose') | Should -Be 'verbose'
            { $transform.Transform($null, 'loud') } | Should -Throw
            $completions = (TabExpansion2 -inputScript 'Write-ConsoleLogEntry x -Level Wa' -cursorColumn 'Write-ConsoleLogEntry x -Level Wa'.Length).CompletionMatches.CompletionText
            $completions | Should -Contain 'warn'
            $completions | Should -Contain 'Warning'
        }
    }

    Context 'Spectre markup compatibility and screen helpers' {
        It 'renders palette names and escaped literal tags' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText -Markup '[cadetblue]cadet[/] [grey50]grey[/] [deepskyblue3]sky[/]'
Write-ConsoleText -Markup '[[nosuchcolour]]kept text[[/]] and [[x]] stays'
Write-ConsoleText -Markup ('[pass]' + ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Text.Markup'))::Escape('[literal]') + '[/]')
'@
            $lines = @($text -split "`n")
            $lines | Should -Contain 'cadet grey sky'
            $lines | Should -Contain '[nosuchcolour]kept text[/] and [x] stays'
            $lines | Should -Contain '[literal]'
        }

        It 'leaves the line open with Write-ConsoleText -Markup -NoNewLine' {
            $text = Invoke-TextChild -Script @'
Write-ConsoleText -Markup '[cadetblue]Checking[/] ' -NoNewLine
Write-ConsoleText -Markup '[pass]done[/]'
'@
            @($text -split "`n") | Should -Contain 'Checking done'
        }


    }
}
