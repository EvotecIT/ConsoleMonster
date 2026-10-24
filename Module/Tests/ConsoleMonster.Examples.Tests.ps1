param([string[]] $ExampleName)

BeforeDiscovery {
    $script:RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))

    # Every script example: the website gallery and demos, and the module's own examples.
    $script:ExampleScripts = @(
        # Pester test files are fixtures for the Pester demo, not examples of their own.
        Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Website\examples') -Filter *.ps1 -Recurse | Where-Object Name -NotLike '*.Tests.ps1'
        Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Module\Examples') -Filter *.ps1
    ) | Sort-Object FullName | ForEach-Object {
        @{ Path = $_.FullName; Name = $_.FullName.Substring($script:RepositoryRoot.Length + 1).Replace('\', '/') }
    }

    # Headless runs: every example once, plus extra runs of the same script in another layout, each a case of its own
    # named after the script and the run (its settings are in ExampleCases under that name).
    $script:ExampleRuns = @($script:ExampleScripts) + @(
        $script:ExampleScripts | Where-Object Name -EQ 'Module/Examples/Example-FleetHealth.ps1' | ForEach-Object { @{ Path = $_.Path; Name = $_.Name + ' (Inline)' } }
        $script:ExampleScripts | Where-Object Name -EQ 'Website/examples/interactive/gridview-fallback.ps1' | ForEach-Object { @{ Path = $_.Path; Name = $_.Name + ' (60 columns)' } }
    )
    if ($ExampleName) { $script:ExampleRuns = @($script:ExampleRuns | Where-Object Name -In $ExampleName) }

    # Documents whose PowerShell snippets must use the cmdlet form.
    $script:DocumentFiles = @(
        Get-Item -LiteralPath (Join-Path $script:RepositoryRoot 'README.md')
        Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Docs') -Filter *.md
        Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Module\Docs') -Filter *.md
        Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Website\content') -Filter *.md -Recurse
    ) | Sort-Object FullName | ForEach-Object {
        @{ Path = $_.FullName; Name = $_.FullName.Substring($script:RepositoryRoot.Length + 1).Replace('\', '/') }
    }

    $script:DataFiles = Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Website\data') -Filter *.json | Sort-Object FullName | ForEach-Object {
        @{ Path = $_.FullName; Name = $_.FullName.Substring($script:RepositoryRoot.Length + 1).Replace('\', '/') }
    }
}

BeforeAll {
    $script:RepositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
    $script:ModulePath = Join-Path $script:RepositoryRoot 'Module\ConsoleMonster.psd1'
    $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path


    # Primary PowerShell examples are written in the cmdlet form. These patterns are the .NET form: type names, constructors,
    # discarded method results and the widgets' add/write/set methods.
    $script:ForbiddenPatterns = [ordered] @{
        'a [ConsoleMonster.*] type name'  = '\[ConsoleMonster\.'
        'a ::new( constructor'            = '::new\('
        'a result piped to Out-Null'      = '\|\s*Out-Null\b'
        'a widget method call'            = '\$[\w:]+(?:\.\w+)*\.(?:Add|AddSeries|AddRange|AddValue|AddColumn|AddRow|Write|Set|SetProgress|SetCounter|Resize)\('
    }

    # The marker for a clearly labelled method-form alternative shown after the cmdlet form.
    $script:ObjectFormMarker = '# Using the object directly'

    function Get-NativeStyleViolation {
        param([string] $Code)
        $lines = $Code -split "`r?`n"
        $inObjectRegion = $false
        foreach ($line in $lines) {
            if ($line -match '^\s*#region\s+Using the object directly') { $inObjectRegion = $true; continue }
            if ($inObjectRegion -and $line -match '^\s*#endregion') { $inObjectRegion = $false; continue }
            if ($inObjectRegion) { continue }
            $code = ($line -replace '(^|\s)#.*$', '')
            foreach ($rule in $script:ForbiddenPatterns.GetEnumerator()) {
                if ($code -match $rule.Value) {
                    "$($rule.Key): $($line.Trim())"
                }
            }
        }
    }

    function Get-MarkdownPowerShellBlock {
        param([string] $Text)
        $pattern = '(?ms)^[ \t]*```(?:powershell|pwsh|ps1|ps)[ \t]*\r?\n(?<code>.*?)^[ \t]*```'
        foreach ($match in [regex]::Matches($Text, $pattern)) {
            $match.Groups['code'].Value
        }
    }

    function Get-JsonPowerShellString {
        param($Node, [string] $Name)
        if ($null -eq $Node) { return }
        if ($Node -is [string]) {
            if ($Name -match '^(powershell|script|ps)') { $Node }
            return
        }
        if ($Node -is [System.Collections.IEnumerable] -and $Node -isnot [System.Management.Automation.PSCustomObject]) {
            foreach ($item in $Node) { Get-JsonPowerShellString -Node $item -Name $Name }
            return
        }
        foreach ($property in $Node.PSObject.Properties) {
            Get-JsonPowerShellString -Node $property.Value -Name $property.Name
        }
    }

    # Starts a child PowerShell of this runtime with redirected input and output, so live views fall back to plain text and
    # prompts read the scripted keys the case gives them.
    function Invoke-ExampleChild {
        param([string] $Script, [int] $TimeoutSeconds)
        $text = "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new(`$false)`n" + $Script
        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($text))
        $info = [System.Diagnostics.ProcessStartInfo]::new($script:Runtime, "-NoLogo -NoProfile -EncodedCommand $encoded")
        # Each example gets its own temporary folder, removed afterwards: some write there (the record demo saves a recording).
        $temp = Join-Path ([IO.Path]::GetTempPath()) ('cm-example-' + [guid]::NewGuid().ToString('N').Substring(0, 12))
        $null = New-Item -ItemType Directory -Path $temp
        $info.Environment['TEMP'] = $temp
        $info.Environment['TMP'] = $temp
        $info.Environment['TMPDIR'] = $temp
        $info.UseShellExecute = $false
        $info.CreateNoWindow = $true
        $info.RedirectStandardInput = $true
        $info.RedirectStandardOutput = $true
        $info.RedirectStandardError = $true
        $info.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
        $info.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
        $process = [System.Diagnostics.Process]::Start($info)
        try {
            $process.StandardInput.Close()
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
                $process.Kill()
                return "EXAMPLE-TIMEOUT after $TimeoutSeconds seconds"
            }
            $process.WaitForExit()
            (($stdout.Result + "`n" + $stderr.Result) -replace "`r", '') -split "`n"
        } finally {
            $process.Dispose()
            Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # How each example runs headless: arguments that skip prompts or shorten waits, scripted keys for menus, a time limit.
    $script:ExampleCases = @{
        # Relative recording exports belong to the child-owned temp folder, not the checked-out example directory.
        'Website/examples/recording/record-command.ps1' = @{ TemporaryWorkingDirectory = $true }
        'Website/examples/recording/record-session.ps1' = @{
            RequiresCore = $true
            Arguments = "-OutputDirectory '$($TestDrive.Replace("'", "''"))/recording-demo'"
            Expect = @('Build succeeded. 24 tests passed.', 'Build report', 'API_TOKEN=')
        }
        'Website/examples/app/live-filter.ps1' = @{ Arguments = '-AppOptions @{ StaticSnapshot = $true }'; Expect = @('6 of 6 hosts', 'SQL01', 'SQL02') }
        # Keep native help's external-pager choice independent of the launcher's PAGER setting in headless runs.
        'Website/examples/help-formatting/help.ps1' = @{ NoNativePager = $true }
        'Module/Examples/Example-InlineProgress.ps1' = @{ Arguments = '-Delay 5' }
        'Module/Examples/Example-InlinePrompts.ps1'  = @{ Arguments = "-Environment Staging -Component web -Release '2026.10' -Force" }
        'Module/Examples/Example-LoginOverview.ps1'  = @{ Arguments = '-Sample' }
        'Module/Examples/Example-RichText.ps1'       = @{ Arguments = '-LogFolder ([IO.Path]::GetTempPath())' }
        'Module/Examples/Example-ScriptMenu.ps1'     = @{ Arguments = '-Layout Inline'; Input = 'b Enter Escape End Enter' }
        # The whole fleet story as an app on a fixed 140 x 40 screen captured headless: apply the Nightly preset from the
        # presets list and go back to the sidebar (Esc), choose the domain and look at its servers once they load (the keys
        # keep their pauses), add Cert chain, review the command, run on the simulated clock, rerun the failed checks from
        # the results, look at the history and quit. The phrases must have been on screen.
        'Module/Examples/Example-FleetHealth.ps1'    = @{
            Arguments = '-Layout Shell -Speed 0'
            Input     = 'wait:300 Enter wait:300 Down wait:200 Down wait:200 Up wait:300 Enter wait:300 Escape wait:300 Down wait:300 Enter wait:300 Enter wait:300 Enter wait:2500 Enter wait:300 Down Down Down wait:300 Enter wait:300 PageDown wait:300 Enter wait:300 Down Down Down Down Down Down Down Down Down wait:300 Space wait:300 Enter wait:300 PageDown wait:500 PageDown wait:800 Enter wait:8000 Down wait:300 Enter wait:500 Shift+Tab wait:300 Down wait:800 End wait:300 Enter'
            RealTime  = $true
            Screen    = '140x40'
            Expect    = @(
                "Preset 'Nightly"
                'Invoke-FleetCheck -Domain corp.contoso.com'
                'Check map'
                ('Report saved: ' + (Join-Path (Join-Path Reports Fleet) 'fleet-20261020-0930.html'))
                '121 checks on 11 servers in corp.contoso.com. 4 need action now; 7 warnings to review.'
                'Queued 4 failed checks for a rerun'
                # The history row of the new run (the run's own screens say "7 warnings").
                ' 7 warn '
            )
        }
        # The same app in the Inline layout, in the scrollback: apply the Nightly preset from the presets list, choose the
        # domain, page through to the review, run, rerun the failed checks from the next steps, open the history and quit.
        # Without a Screen the phrases are looked up in what the run printed.
        'Module/Examples/Example-FleetHealth.ps1 (Inline)' = @{
            Arguments = '-Layout Inline -Speed 0'
            Input     = 'wait:300 p wait:300 Down wait:200 Enter wait:300 n wait:300 Enter wait:300 Enter wait:2000 PageDown wait:300 PageDown wait:300 PageDown wait:500 Enter wait:5000 Down wait:300 Enter wait:500 h wait:500 End wait:300 Enter'
            RealTime  = $true
            Expect    = @(
                "all sites' applied"
                'Invoke-FleetCheck -Domain corp.contoso.com'
                '110 checks on 11 servers in corp.contoso.com. 4 need action now; 6 warnings to review.'
                ('Report saved: ' + (Join-Path (Join-Path Reports Fleet) 'fleet-20261020-0930.html'))
                'Queued 4 failed checks for a rerun'
                '4 runs'
            )
        }
        'Module/Examples/Example-Stress.ps1'         = @{ Arguments = '-Seconds 2' }
        # Demos that show failures on purpose: the run must finish, and the failures are its errors.
        # The everyday and FormatCommands demos run Windows commands (svchost, Get-Service), as their recordings do; a
        # command that does not exist stops the script, so they run on Windows only.
        'Website/examples/demos/formatting.ps1'      = @{ RequiresWindows = $true }
        'Website/examples/demos/parallel.ps1'        = @{ ExpectErrors = $true }
        'Website/examples/demos/format-commands.ps1' = @{ RequiresWindows = $true }
        # ForEach-Object -Parallel needs PowerShell 7 (the demo says so).
        'Website/examples/demos/threads-naive.ps1'   = @{ RequiresCore = $true }
        'Website/examples/demos/pester.ps1'          = @{ ExpectErrors = $true }
        'Website/examples/demos/errors.ps1'          = @{ ExpectErrors = $true }
        'Website/examples/recording/demo.ps1'         = @{ ExpectErrors = $true }
        'Website/examples/errors/compact.ps1'         = @{ ExpectErrors = $true }
        # This example deliberately keeps a working native desktop grid. Its headless run exercises the unavailable
        # command namespace instead, with the same scripted selection as the site frames, so no native window opens.
        'Website/examples/interactive/gridview-fallback.ps1' = @{
            UnavailableGridView = $true
            Input = 'Space Down Space F10'
            Paced = $true
            Screen = '120x20'
            Expect = @('Choose nodes', 'api-east', 'worker-west', 'ctrl+enter', 'esc')
            Selection = @('api-east', 'worker-west')
        }
    }
    $script:ExampleCases['Website/examples/interactive/gridview-fallback.ps1 (60 columns)'] = @{} + $script:ExampleCases['Website/examples/interactive/gridview-fallback.ps1']
    $script:ExampleCases['Website/examples/interactive/gridview-fallback.ps1 (60 columns)'].Screen = '60x20'
    # The site's prompt stories answer with the keys the site records them with (data/interactive.json, written by the site
    # tool from the same story list), on a captured screen of the story's size.
    $interactive = Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'Website\data\interactive.json') -Raw | ConvertFrom-Json
    foreach ($story in $interactive.stories) {
        $name = "Website/examples/interactive/$($story.id).ps1"
        if (Test-Path -LiteralPath (Join-Path $script:RepositoryRoot $name)) {
            $script:ExampleCases[$name] = @{ Input = $story.keys; RealTime = $true; Paced = $true; Screen = "$($story.player.w)x$($story.player.h)"; Expect = @($story.title.Substring(0, 0)) }
        }
    }
}

Describe 'Examples run headless' -Tag 'Examples' {
    BeforeAll {
        # Examples import the module by name (Import-Module ConsoleMonster), as a reader's copy would. A link named
        # ConsoleMonster to this repository's Module folder, first on PSModulePath in each child, makes that name load the
        # repository build instead of an installed version. ConsoleMonster.PowerShell is linked beside it because the
        # module's development loader looks for the cmdlet binaries at $PSScriptRoot\..\ConsoleMonster.PowerShell\bin.
        $script:ModuleLinkRoot = Join-Path ([IO.Path]::GetTempPath()) ('cm-example-modules-' + $PID + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
        $script:ModuleLinks = @(
            @{ Path = Join-Path $script:ModuleLinkRoot 'ConsoleMonster'; Target = Join-Path $script:RepositoryRoot 'Module' }
            @{ Path = Join-Path $script:ModuleLinkRoot 'ConsoleMonster.PowerShell'; Target = Join-Path $script:RepositoryRoot 'ConsoleMonster.PowerShell' }
        )
        $null = New-Item -ItemType Directory -Force -Path $script:ModuleLinkRoot
        $linkType = if ($PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows) { 'Junction' } else { 'SymbolicLink' }
        foreach ($link in $script:ModuleLinks) {
            $null = New-Item -ItemType $linkType -Path $link.Path -Value $link.Target
        }
    }

    AfterAll {
        # Directory.Delete without recursion removes the link itself and never the repository folder it points to.
        foreach ($link in $script:ModuleLinks) {
            if (Test-Path -LiteralPath $link.Path) { [IO.Directory]::Delete($link.Path, $false) }
        }
        if (Test-Path -LiteralPath $script:ModuleLinkRoot) { [IO.Directory]::Delete($script:ModuleLinkRoot, $false) }
    }
    It '<Name> runs without errors' -ForEach $script:ExampleRuns {
        $case = $script:ExampleCases[$Name]
        if ($null -eq $case) { $case = @{} }
        if ($case['RequiresCore'] -and $PSVersionTable.PSEdition -eq 'Desktop') {
            Set-ItResult -Skipped -Because 'the example uses PowerShell 7 syntax and says so'
            return
        }
        if ($case['RequiresWindows'] -and -not ($PSVersionTable.PSEdition -eq 'Desktop' -or $IsWindows)) {
            Set-ItResult -Skipped -Because 'the example runs Windows commands, as its recording does'
            return
        }
        # Scripted keys run instantly unless the case asks for real time, which honours their wait: pauses.
        $realTime = if ($case['RealTime']) { '$true' } else { '$false' }
        # Paced keys wait for the prompt to take the previous one, as the site's recordings do.
        $paced = if ($case['Paced']) { '$true' } else { '$false' }
        $keys = if ($case['Input']) { "`$scriptedKeys = [ConsoleMonster.Input.ScriptedInput]::new('$($case['Input'])'); `$scriptedKeys.RealTime = $realTime; `$scriptedKeys.Paced = $paced; [ConsoleMonster.Interactive.InteractiveOptions]::Default.Input = `$scriptedKeys" } else { '' }
        $arguments = if ($case['Arguments']) { $case['Arguments'] } else { '' }
        $pager = if ($case['NoNativePager']) { "function global:Out-ConsoleExamplePager { param(`$PagerArguments) process { `$_ } }; `$env:PAGER = 'Out-ConsoleExamplePager'" } else { '' }
        $invoke = "& '$Path' $arguments | Out-String | Out-Null"
        $location = if ($case['TemporaryWorkingDirectory']) {
            'Set-Location -LiteralPath ([IO.Path]::GetTempPath())'
        } else {
            "Set-Location -LiteralPath '$(Split-Path -Parent $Path)'"
        }
        if ($case['Selection']) {
            $names = ($case['Selection'] -join ',').Replace("'", "''")
            $invoke = "`$selected = @(& '$Path' $arguments); if ((`$selected.Name -join ',') -ne '$names') { throw 'The full example returned an unexpected selection.' }"
        }
        # A case with a Screen draws the rich layouts on a fixed screen captured in memory, whose frames the Expect phrases
        # are looked up in afterwards.
        $screen = ''
        $expect = ''
        if ($case['Screen']) {
            $width, $height = $case['Screen'] -split 'x'
            $screen = "`$capture = [ConsoleMonster.Interactive.ScreenCapture]::new($width, $height, `$false); `$options = [ConsoleMonster.Interactive.InteractiveOptions]::Default; `$options.Mode = 'Rich'; `$options.Capture = `$capture; `$options.Profile = [ConsoleMonster.Capabilities.TerminalProfile]::Fixed($width, $height, 'TrueColor', 'Full')"
            $phrases = @($case['Expect'] | ForEach-Object { "'" + ($_ -replace "'", "''") + "'" }) -join ', '
            $expect = "`$frames = @(`$capture.Frames | ForEach-Object { `$_.ToPlainText() }); foreach (`$phrase in @($phrases)) { if (`$frames | Where-Object { `$_.Contains(`$phrase) }) { 'EXAMPLE-SAW: ' + `$phrase } else { 'EXAMPLE-MISSING: ' + `$phrase } }"
        }
        $script = @"
foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_MODE', 'CONSOLEMONSTER_NONINTERACTIVE') { [Environment]::SetEnvironmentVariable(`$n, `$null) }
`$ErrorActionPreference = '$(if ($case['ExpectErrors']) { 'Continue' } else { 'Stop' })'
`$env:PSModulePath = '$($script:ModuleLinkRoot)' + [IO.Path]::PathSeparator + `$env:PSModulePath
Import-Module ConsoleMonster -Force
if (@(Get-Module ConsoleMonster).Count -ne 1 -or -not (Get-Command Write-ConsoleWidget).DLL.StartsWith('$($script:ModuleLinkRoot)', [StringComparison]::OrdinalIgnoreCase)) { throw 'ConsoleMonster did not load from this repository (build ConsoleMonster.PowerShell first)' }
$keys
$screen
$pager
$location
try {
    $invoke
    'EXAMPLE-DONE'
} catch {
    'EXAMPLE-FAILED: ' + `$_
}
$expect
'EXAMPLE-ERRORS=' + `$Error.Count
foreach (`$e in `$Error) { 'EXAMPLE-ERROR: ' + `$e }
"@
        $timeout = if ($case['TimeoutSeconds']) { $case['TimeoutSeconds'] } else { 120 }
        if ($case['UnavailableGridView']) {
            $helper = Join-Path $script:RepositoryRoot 'Module/Tests/Support/Invoke-GridViewSession.ps1'
            $manifest = Join-Path $script:ModuleLinkRoot 'ConsoleMonster/ConsoleMonster.psd1'
            # Resolve the public name and the isolated-session manifest through the same supported module identity.
            $script = $script.Replace('Import-Module ConsoleMonster -Force', '')
            $script = "`$env:PSModulePath = '$($script:ModuleLinkRoot)' + [IO.Path]::PathSeparator + `$env:PSModulePath`nImport-Module ConsoleMonster`n. '$($helper.Replace("'", "''"))'`nInvoke-GridViewSession -ModulePath '$($manifest.Replace("'", "''"))' -Script '$($script.Replace("'", "''"))'"
        }
        $lines = @(Invoke-ExampleChild -Script $script -TimeoutSeconds $timeout)
        # Terminal control sequences (cursor show and hide, line clears) can share a line with the markers.
        $escape = [string][char]27
        $report = ($lines | ForEach-Object { ($_ -replace ($escape + '\][^\a]*?(\a|' + $escape + '\\)'), '') -replace ($escape + '\[[0-9;?]*[ -/]*[@-~]'), '' } | Where-Object { $_ -match 'EXAMPLE-' }) -join "`n"
        $report | Should -Match 'EXAMPLE-DONE' -Because $report
        if (-not $case['ExpectErrors']) {
            $report | Should -Match 'EXAMPLE-ERRORS=0' -Because $report
        }
        # On a captured screen the child reports each phrase it saw; otherwise the phrase must be in what it printed.
        $printed = ($lines | ForEach-Object { ($_ -replace ($escape + '\][^\a]*?(\a|' + $escape + '\\)'), '') -replace ($escape + '\[[0-9;?]*[ -/]*[@-~]'), '' }) -join "`n"
        foreach ($phrase in @($case['Expect'])) {
            if (-not $phrase) {
                continue
            }
            if ($case['Screen']) {
                $report | Should -Match ('EXAMPLE-SAW: ' + [regex]::Escape($phrase)) -Because $report
            } else {
                $printed.Contains($phrase) | Should -BeTrue -Because "the run should have printed '$phrase'. $report"
            }
        }
    }
}

Describe 'Examples use the PowerShell cmdlet form' -Tag 'Examples' {
    It 'ships non-ASCII PowerShell scripts as UTF-8 with a BOM for Windows PowerShell 5.1' {
        $scripts = @(
            Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Module') -Filter *.ps1 -Recurse |
                Where-Object FullName -NotMatch '[/\\]Tests[/\\]'
            Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Website/examples') -Filter *.ps1 -Recurse
            Get-ChildItem -LiteralPath (Join-Path $script:RepositoryRoot 'Website/static/examples') -Filter *.ps1 -Recurse
        )
        $invalid = foreach ($file in $scripts) {
            $bytes = [IO.File]::ReadAllBytes($file.FullName)
            $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
            if (-not $hasBom -and @($bytes | Where-Object { $_ -gt 127 }).Count) {
                $file.FullName.Substring($script:RepositoryRoot.Length + 1)
            }
        }
        $invalid | Should -BeNullOrEmpty -Because ('Windows PowerShell 5.1 reads BOM-less scripts as the system ANSI encoding: ' + ($invalid -join ', '))
    }

    It '<Name> has no .NET construction or widget method calls' -ForEach $script:ExampleScripts {
        $violations = @(Get-NativeStyleViolation -Code (Get-Content -LiteralPath $Path -Raw))
        $violations | Should -BeNullOrEmpty -Because ($violations -join '; ')
    }

    It '<Name> has only cmdlet-form PowerShell snippets' -ForEach $script:DocumentFiles {
        $violations = foreach ($block in Get-MarkdownPowerShellBlock -Text (Get-Content -LiteralPath $Path -Raw)) {
            if ($block.TrimStart().StartsWith($script:ObjectFormMarker)) { continue }
            Get-NativeStyleViolation -Code $block
        }
        @($violations) | Should -BeNullOrEmpty -Because (@($violations) -join '; ')
    }

    It '<Name> has only cmdlet-form PowerShell snippets' -ForEach $script:DataFiles {
        $data = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        $violations = foreach ($snippet in Get-JsonPowerShellString -Node $data -Name '') {
            if ($snippet.TrimStart().StartsWith($script:ObjectFormMarker)) { continue }
            Get-NativeStyleViolation -Code $snippet
        }
        @($violations) | Should -BeNullOrEmpty -Because (@($violations) -join '; ')
    }

    It 'flags the .NET form and accepts the labelled alternative' {
        @(Get-NativeStyleViolation -Code "`$tree = [ConsoleMonster.Widgets.Tree]::new('x')").Count | Should -Be 2
        @(Get-NativeStyleViolation -Code "`$mx.Add('a', `$null, 'b') | Out-Null").Count | Should -Be 2
        @(Get-NativeStyleViolation -Code "`$log.Write('text', 'info')").Count | Should -Be 1
        @(Get-NativeStyleViolation -Code "New-ConsoleTree 'x' { New-ConsoleTreeNode 'MX' -Status Pass }").Count | Should -Be 0
        @(Get-NativeStyleViolation -Code "#region Using the object directly`n`$log.Write('text')`n#endregion").Count | Should -Be 0
    }
}

Describe 'Every exported command has real help' -Tag 'Help' {
    BeforeAll {
        $manifest = Import-PowerShellDataFile -LiteralPath $script:ModulePath
        $script:Exported = @($manifest.CmdletsToExport)
        [xml] $script:Maml = Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'Module\en-US\ConsoleMonster-help.xml') -Raw
        $script:Namespaces = [System.Xml.XmlNamespaceManager]::new($script:Maml.NameTable)
        $script:Namespaces.AddNamespace('command', 'http://schemas.microsoft.com/maml/dev/command/2004/10')
        $script:Namespaces.AddNamespace('maml', 'http://schemas.microsoft.com/maml/2004/10')
        $script:Namespaces.AddNamespace('dev', 'http://schemas.microsoft.com/maml/dev/2004/10')
        $script:Commands = @{}
        foreach ($node in $script:Maml.SelectNodes('//command:command', $script:Namespaces)) {
            $script:Commands[$node.SelectSingleNode('command:details/command:name', $script:Namespaces).InnerText.Trim()] = $node
        }
        # Examples PowerForge writes when a command has none of its own: a bare call with empty or sample values.
        $script:PlaceholderExample = '^\S+(\s+-\w+(\s+(\{\s*\}|@\{\}|''Value''|\d+|\$true|\$false))?)*\s*$'
    }

    It 'documents every exported cmdlet in the external help and the command pages' {
        $missing = @($script:Exported | Where-Object { -not $script:Commands.ContainsKey($_) -or -not (Test-Path -LiteralPath (Join-Path $script:RepositoryRoot "Module\Docs\$_.md")) })
        $missing | Should -BeNullOrEmpty -Because ($missing -join ', ')
    }

    It 'gives every exported cmdlet a description and at least one real example' {
        $problems = foreach ($name in $script:Exported) {
            $node = $script:Commands[$name]
            if ($null -eq $node) { continue }
            $synopsis = $node.SelectSingleNode('command:details/maml:description', $script:Namespaces).InnerText.Trim()
            $description = $node.SelectSingleNode('maml:description', $script:Namespaces).InnerText.Trim()
            if ($synopsis.Length -lt 15 -or $synopsis -eq $name) { "${name}: placeholder synopsis '$synopsis'" }
            if ($description.Length -lt 60) { "${name}: thin description ($($description.Length) characters)" }
            $real = @($node.SelectNodes('command:examples/command:example', $script:Namespaces) | Where-Object {
                    $code = $_.SelectSingleNode('dev:code', $script:Namespaces).InnerText.Trim()
                    $remarks = $_.SelectSingleNode('dev:remarks', $script:Namespaces).InnerText.Trim()
                    $code.Length -gt 0 -and $remarks.Length -gt 0 -and $code -notmatch $script:PlaceholderExample
                })
            if ($real.Count -eq 0) { "${name}: no real example" }
        }
        @($problems) | Should -BeNullOrEmpty -Because (@($problems) -join '; ')
    }
}
