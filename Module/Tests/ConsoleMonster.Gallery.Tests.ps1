Describe 'ConsoleMonster gallery runner' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:GalleryModulePath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $script:GalleryRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:GalleryCulture = [Threading.Thread]::CurrentThread.CurrentCulture
        $script:GalleryUICulture = [Threading.Thread]::CurrentThread.CurrentUICulture
        $script:GalleryPreviousConfiguration = $env:CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION
        $env:CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION = 'Release'
        # Name-based imports in the unchanged standalone scripts need a discoverable module.
        # Its development loader also needs the sibling project link to find this checkout's binaries.
        $script:GalleryModuleLinkRoot = Join-Path $TestDrive 'modules'
        $null = New-Item -ItemType Directory -Path $script:GalleryModuleLinkRoot
        $script:GalleryModuleLinks = @()
        $repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
        $script:GalleryCapturePath = Join-Path $repository 'ConsoleMonster.PowerShell/Gallery/DemoCapture.ps1'
        $linkType = if ($env:OS -eq 'Windows_NT') { 'Junction' } else { 'SymbolicLink' }
        foreach ($link in @(
            @{ Name = 'ConsoleMonster'; Target = (Join-Path $repository 'Module') }
            @{ Name = 'ConsoleMonster.PowerShell'; Target = (Join-Path $repository 'ConsoleMonster.PowerShell') }
        )) {
            $path = Join-Path $script:GalleryModuleLinkRoot $link.Name
            $null = New-Item -ItemType $linkType -Path $path -Target $link.Target -ErrorAction Stop
            $script:GalleryModuleLinks += $path
        }
        [Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
        [Threading.Thread]::CurrentThread.CurrentUICulture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
        Import-Module $script:GalleryModulePath -Force -ErrorAction Stop

        function Invoke-GalleryChild {
            param([Parameter(Mandatory)][string] $Body)

            $setup = @(
                '$ErrorActionPreference = ''Stop'''
                'try {'
                '[Threading.Thread]::CurrentThread.CurrentCulture = [Globalization.CultureInfo]::GetCultureInfo(''en-US'')'
                '[Threading.Thread]::CurrentThread.CurrentUICulture = [Globalization.CultureInfo]::GetCultureInfo(''en-US'')'
                '$env:NO_COLOR = ''1'''
                '$env:CONSOLEMONSTER_MODE = ''Plain'''
                '$env:CONSOLEMONSTER_INPUT = $null'
                '$env:CONSOLEMONSTER_SNAPSHOT = $null'
                '$env:CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION = ''Release'''
                "`$env:PSModulePath = '$($script:GalleryModuleLinkRoot.Replace("'", "''"))' + [IO.Path]::PathSeparator + `$env:PSModulePath"
                'Import-Module ConsoleMonster -Force -ErrorAction Stop'
                $Body
                '} catch { [Console]::Error.WriteLine($_.Exception.ToString()); exit 1 }'
            )
            $output = @(Invoke-TestChild -Runtime $script:GalleryRuntime -Script ($setup -join "`n"))
            if ($LASTEXITCODE -ne 0) {
                throw "Gallery child failed ($LASTEXITCODE): $($output -join "`n")"
            }
            $output -join "`n"
        }

        function Invoke-GalleryAppChild {
            param(
                [Parameter(Mandatory)][string] $Slug,
                [Parameter(Mandatory)][string] $Keys
            )

            $path = Join-Path $TestDrive "interactive-$Slug.ps1"
            $null = Export-ConsoleDemo $Slug -Path $path -PassThru
            $body = @(
                "`$slug = '$($Slug.Replace("'", "''"))'"
                "`$keys = '$($Keys.Replace("'", "''"))'"
                "`$path = '$($path.Replace("'", "''"))'"
                @'
Set-Location ([IO.Path]::GetDirectoryName($path))
$demo = Get-ConsoleDemo $slug
$capture = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.ScreenCapture'))::new($demo.Terminal.Width, $demo.Terminal.Height)
$inputSource = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ScriptedInput'))::new($keys)
$inputSource.Paced = $true
$previous = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default
$options = $previous.Clone()
$options.Input = $inputSource
$options.Capture = $capture
$options.Mode = 'Rich'
$options.NonInteractive = $false
try {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default = $options
    & $path
} finally {
    ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default = $previous
}
[pscustomobject] @{
    Frame = $capture.LastFrame.ToPlainText()
    FrameCount = $capture.Frames.Count
    Remaining = $inputSource.Remaining
    InputAcquired = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Input.ConsoleInput'))::IsAcquired
} | ConvertTo-Json -Depth 4 -Compress
'@
            )
            Invoke-GalleryChild -Body ($body -join "`n") | ConvertFrom-Json
        }
    }

    AfterAll {
        [Threading.Thread]::CurrentThread.CurrentCulture = $script:GalleryCulture
        [Threading.Thread]::CurrentThread.CurrentUICulture = $script:GalleryUICulture
        $env:CONSOLEMONSTER_DEVELOPMENT_CONFIGURATION = $script:GalleryPreviousConfiguration
        # Delete only the owned links, never recursively traverse their source targets.
        foreach ($path in $script:GalleryModuleLinks) {
            [IO.Directory]::Delete($path)
        }
    }

    It 'imports both compiled commands and lists the complete gallery metadata' {
        (Get-Command Get-ConsoleDemo -Module ConsoleMonster).CommandType | Should -Be 'Cmdlet'
        (Get-Command Show-ConsoleDemo -Module ConsoleMonster).CommandType | Should -Be 'Cmdlet'
        $demos = @(Get-ConsoleDemo)
        $expected = @(Get-ChildItem (Join-Path $PSScriptRoot '../../Website/examples/gallery/*/metadata.json') | ForEach-Object { $_.Directory.Name } | Sort-Object)
        ($demos.Slug -join ',') | Should -BeExactly ($expected -join ',')
        foreach ($demo in $demos) {
            $demo.Title | Should -Not -BeNullOrEmpty
            $demo.Pitch | Should -Not -BeNullOrEmpty
            $demo.Category | Should -Not -BeNullOrEmpty
            $demo.Tags | Should -BeOfType [string]
            $demo.Terminal.Width | Should -BeGreaterThan 0
            $demo.Terminal.Height | Should -BeGreaterThan 0
            $demo.Runtime.PowerShell | Should -BeExactly $(if ($demo.Slug -eq 'tls-verdict') { '5.1+' } else { '7.4+' })
            $demo.Runtime.DotNet | Should -BeExactly '8.0+'
            $demo.PSObject.Properties.Name | Should -Contain 'addons'
            @($demo.MakeItYours).Count | Should -Be 3
            @('static', 'live') | Should -Contain $demo.Mode
            @('starter', 'showcase') | Should -Contain $demo.Tier
            $demo.UseCase | Should -Match '^Use it when .+\.$'
            $demo.Steps | Should -BeGreaterThan 0
        }
    }

    It 'filters by positional slug and wildcard without running a demo' {
        @(Get-ConsoleDemo READINESS-REPORT).Count | Should -Be 1
        (Get-ConsoleDemo READINESS-REPORT).Slug | Should -BeExactly 'readiness-report'
        (@(Get-ConsoleDemo 'welcome-*').Slug -join ',') | Should -BeExactly 'welcome-compact,welcome-rich'
        @(Get-ConsoleDemo 'no-such-demo-*').Count | Should -Be 0
        { Get-ConsoleDemo no-such-demo } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        { Show-ConsoleDemo no-such-demo -StaticSnapshot } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
    }

    It 'exports the canonical script verbatim and returns its file information' {
        $path = Join-Path $TestDrive 'readiness-report.ps1'
        $file = Export-ConsoleDemo readiness-report -Path $path -PassThru
        $file | Should -BeOfType [IO.FileInfo]
        $file.FullName | Should -Be $path
        $canonical = Join-Path $PSScriptRoot '../../Website/examples/gallery/readiness-report/readiness-report.ps1'
        [IO.File]::ReadAllText($path) | Should -BeExactly ([IO.File]::ReadAllText($canonical))
        $tokens = $null
        $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($path, [ref] $tokens, [ref] $parseErrors)
        @($parseErrors).Count | Should -Be 0
        $ast.ParamBlock | Should -BeNullOrEmpty
    }

    It 'preserves an existing file unless Force is explicit and honors WhatIf' {
        $path = Join-Path $TestDrive 'existing.ps1'
        [IO.File]::WriteAllText($path, 'keep my edits')
        { Export-ConsoleDemo fleet-filter -Path $path -PassThru } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
        [IO.File]::ReadAllText($path) | Should -BeExactly 'keep my edits'
        Export-ConsoleDemo fleet-filter -Path $path -Force -WhatIf -PassThru
        [IO.File]::ReadAllText($path) | Should -BeExactly 'keep my edits'
        $null = Export-ConsoleDemo fleet-filter -Path $path -Force -PassThru
        [IO.File]::ReadAllText($path) | Should -Not -BeExactly 'keep my edits'
        $unused = Join-Path $TestDrive 'whatif.ps1'
        Export-ConsoleDemo fleet-filter -Path $unused -WhatIf -PassThru
        Test-Path -LiteralPath $unused | Should -BeFalse
    }

    It 'rejects unknown exports, wildcard exports and missing destination directories without creating files' {
        $path = Join-Path $TestDrive 'unknown.ps1'
        { Export-ConsoleDemo no-such-demo -Path $path -PassThru } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        { Export-ConsoleDemo 'welcome-*' -Path $path -PassThru } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        Test-Path -LiteralPath $path | Should -BeFalse
        { Export-ConsoleDemo fleet-filter -Path (Join-Path $TestDrive 'missing/demo.ps1') -PassThru } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
        { Export-ConsoleDemo fleet-filter -Path 'Variable:GalleryDemoExport' -PassThru } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
    }

    It 'exports <Slug> byte for byte with its BOM and runs a runner-selected snapshot' -ForEach @(
        Get-ChildItem (Join-Path $PSScriptRoot '../../Website/examples/gallery/*/metadata.json') | ForEach-Object { @{ Slug = $_.Directory.Name } }
    ) {
        $path = Join-Path $TestDrive "$Slug.ps1"
        $null = Export-ConsoleDemo $Slug -Path $path -Force -PassThru
        $canonical = Join-Path $PSScriptRoot "../../Website/examples/gallery/$Slug/$Slug.ps1"
        $bytes = [IO.File]::ReadAllBytes($path)
        [Convert]::ToBase64String($bytes) | Should -BeExactly ([Convert]::ToBase64String([IO.File]::ReadAllBytes($canonical)))
        ($bytes[0..2] -join ',') | Should -BeExactly '239,187,191'
        $text = Invoke-GalleryChild -Body "Show-ConsoleDemo '$Slug' -StaticSnapshot -Step 0"
        $text.Trim() | Should -Not -BeNullOrEmpty
    }

    It 'drives the selected state in the runner for both embedded and exported ordinary scripts' {
        $path = Join-Path $TestDrive 'standalone.ps1'
        $null = Export-ConsoleDemo build-steps -Path $path -PassThru
        $embedded = Invoke-GalleryChild -Body 'Show-ConsoleDemo build-steps -StaticSnapshot -Step 1'
        $body = @(
            "Set-Location '$($TestDrive.Replace("'", "''"))'"
            "`$frame = & '$($script:GalleryCapturePath.Replace("'", "''"))' -Demo { & '$($path.Replace("'", "''"))' } -Metadata (Get-ConsoleDemo build-steps) -Step 1"
            @'
$output = [ConsoleMonster.Terminal]::CreateOutput([ConsoleMonster.Terminal]::GetCurrentProfile(), [ConsoleMonster.Terminal]::Target.Writer)
try {
    $output.WriteBlock($frame)
} finally {
    $output.Dispose()
}
'@
        )
        $exported = Invoke-GalleryChild -Body ($body -join "`n")
        $embedded | Should -Match 'Restore\s+complete'
        $embedded | Should -Match 'Compile\s+running'
        $embedded | Should -Not -Match 'Restore\s+running'
        $embedded | Should -BeExactly $exported
        $embedded | Should -BeExactly (Invoke-GalleryChild -Body 'Show-ConsoleDemo build-steps -StaticSnapshot -Step 1')
    }

    It 'applies the declared <ClockMode> clock to direct capture and restores the caller clock' -ForEach @(
        @{ ClockMode = 'Frozen'; ExpectedSeconds = 0 }
        @{ ClockMode = 'Inherited'; ExpectedSeconds = 7 }
    ) {
        $body = @(
            "`$driver = '$($script:GalleryCapturePath.Replace("'", "''"))'"
            "`$mode = '$ClockMode'"
            @'
$interactive = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true)
$clock = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Live.SimulatedClock', $true))::new()
$clock.Advance([double] 7)
$original = $interactive::Default
$previous = $original.Clone()
$previous.ActionClock = [Delegate]::CreateDelegate([Func[TimeSpan]], $clock, 'Read')
$interactive::Default = $previous
try {
    $metadata = Get-ConsoleDemo fleet
    if ($mode -eq 'Frozen') {
        $metadata.capture | Add-Member -NotePropertyName actionClock -NotePropertyValue 'Frozen' -Force
    }
    $frame = & $driver -Metadata $metadata -Step 0 -Demo {
        $seconds = ([ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.Interactive.InteractiveOptions', $true))::Default.ActionClock.Invoke().TotalSeconds
        [ConsoleMonster.Terminal]::WriteMarkupLine("Clock=$seconds")
    }
    [pscustomobject] @{
        Text = $frame.ToPlainText()
        Restored = [object]::ReferenceEquals($interactive::Default, $previous)
        Seconds = $interactive::Default.ActionClock.Invoke().TotalSeconds
    } | ConvertTo-Json -Compress
} finally {
    $interactive::Default = $original
}
'@
        )
        $result = Invoke-GalleryChild -Body ($body -join "`n") | ConvertFrom-Json
        $result.Text.Trim() | Should -BeExactly "Clock=$ExpectedSeconds"
        $result.Restored | Should -BeTrue
        $result.Seconds | Should -Be 7
    }

    It 'keeps the loaded fleet defaults selected in the native editor' {
        $text = Invoke-GalleryChild -Body 'Show-ConsoleDemo fleet -StaticSnapshot -Step 1'
        $text | Should -Match '5 of 5 selected'
    }

    It 'captures toolbox targets only after their defaults have loaded' {
        $text = Invoke-GalleryChild -Body 'Show-ConsoleDemo toolbox -StaticSnapshot -Step 1'
        $text | Should -Match '4 of 9 selected'
    }

    It 'filters the exported <Slug> app through real scripted input and shows the matching result' -ForEach @(
        @{ Slug = 'fleet-filter'; Query = 'missing'; Expected = 'No matching hosts'; Absent = 'web-01'; Count = 0 }
        @{ Slug = 'live-filter'; Query = 'sql'; Expected = 'sql-02'; Absent = 'web-01'; Count = 2 }
    ) {
        $result = Invoke-GalleryAppChild -Slug $Slug -Keys ('"{0}" Escape' -f $Query)
        $result.Frame | Should -Match ([regex]::Escape($Query))
        $result.Frame | Should -Match "$Count of 5 hosts"
        $result.Frame | Should -Match ([regex]::Escape($Expected))
        $result.Frame | Should -Not -Match ([regex]::Escape($Absent))
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }

    It 'selects and shows a queue preview in the exported task-menu' {
        $result = Invoke-GalleryAppChild -Slug task-menu -Keys 'Down Enter Escape'
        $result.Frame | Should -Match 'Inspect queue'
        $result.Frame | Should -Match '12 jobs waiting'
        $result.Frame | Should -Not -Match 'CPU 24%'
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }

    It 'chooses reports and advances the exported setup-basic to its final review' {
        $result = Invoke-GalleryAppChild -Slug setup-basic -Keys 'Tab Down Enter Tab Tab Enter Enter Escape'
        $result.Frame | Should -Match '3 / 3'
        $result.Frame | Should -Match 'Ready to install'
        $result.Frame | Should -Match 'Folder: \./toolbox-agent'
        $result.Frame | Should -Match 'Component: Core \+ reports'
        $result.Frame | Should -Not -Match 'Choose a writable folder'
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }
}
