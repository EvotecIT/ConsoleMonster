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
            $null = Get-ConsoleDemo $Slug -Path $path
            $body = @(
                "`$slug = '$($Slug.Replace("'", "''"))'"
                "`$keys = '$($Keys.Replace("'", "''"))'"
                "`$path = '$($path.Replace("'", "''"))'"
                @'
Set-Location ([IO.Path]::GetDirectoryName($path))
$demo = Get-ConsoleDemo $slug
$capture = [ConsoleMonster.Interactive.ScreenCapture]::new($demo.Terminal.Width, $demo.Terminal.Height)
$inputSource = [ConsoleMonster.Input.ScriptedInput]::new($keys)
$inputSource.Paced = $true
$state = & $path -AppOptions @{
    InputSource = $inputSource
    Capture = $capture
    PassThru = $true
    NoMouse = $true
}
[pscustomobject] @{
    State = $state
    Frame = $capture.LastFrame.ToPlainText()
    FrameCount = $capture.Frames.Count
    Remaining = $inputSource.Remaining
    InputAcquired = [ConsoleMonster.Input.ConsoleInput]::IsAcquired
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
            $demo.Runtime.PowerShell | Should -BeExactly '7.4+'
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
        @(Get-ConsoleDemo ASSESSMENT).Count | Should -Be 1
        (Get-ConsoleDemo ASSESSMENT).Slug | Should -BeExactly 'assessment'
        (@(Get-ConsoleDemo 'welcome-*').Slug -join ',') | Should -BeExactly 'welcome-compact,welcome-rich'
        @(Get-ConsoleDemo 'no-such-demo-*').Count | Should -Be 0
        { Get-ConsoleDemo no-such-demo } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        { Show-ConsoleDemo no-such-demo -StaticSnapshot } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
    }

    It 'exports the canonical script verbatim and returns its file information' {
        $path = Join-Path $TestDrive 'assessment.ps1'
        $file = Get-ConsoleDemo assessment -Path $path
        $file | Should -BeOfType [IO.FileInfo]
        $file.FullName | Should -Be $path
        $canonical = Join-Path $PSScriptRoot '../../Website/examples/gallery/assessment/assessment.ps1'
        [IO.File]::ReadAllText($path) | Should -BeExactly ([IO.File]::ReadAllText($canonical))
        $tokens = $null
        $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($path, [ref] $tokens, [ref] $parseErrors)
        @($parseErrors).Count | Should -Be 0
        $ast.ParamBlock.Parameters.Name.VariablePath.UserPath | Should -Contain 'StaticSnapshot'
        $ast.ParamBlock.Parameters.Name.VariablePath.UserPath | Should -Contain 'Step'
    }

    It 'preserves an existing file unless Force is explicit and honors WhatIf' {
        $path = Join-Path $TestDrive 'existing.ps1'
        [IO.File]::WriteAllText($path, 'keep my edits')
        { Get-ConsoleDemo fleet -Path $path } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
        [IO.File]::ReadAllText($path) | Should -BeExactly 'keep my edits'
        Get-ConsoleDemo fleet -Path $path -Force -WhatIf
        [IO.File]::ReadAllText($path) | Should -BeExactly 'keep my edits'
        $null = Get-ConsoleDemo fleet -Path $path -Force
        [IO.File]::ReadAllText($path) | Should -Not -BeExactly 'keep my edits'
        $unused = Join-Path $TestDrive 'whatif.ps1'
        Get-ConsoleDemo fleet -Path $unused -WhatIf
        Test-Path -LiteralPath $unused | Should -BeFalse
    }

    It 'rejects unknown exports, wildcard exports and missing destination directories without creating files' {
        $path = Join-Path $TestDrive 'unknown.ps1'
        { Get-ConsoleDemo no-such-demo -Path $path } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        { Get-ConsoleDemo 'welcome-*' -Path $path } | Should -Throw -ErrorId 'ConsoleDemoNotFound*'
        Test-Path -LiteralPath $path | Should -BeFalse
        { Get-ConsoleDemo fleet -Path (Join-Path $TestDrive 'missing/demo.ps1') } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
        { Get-ConsoleDemo fleet -Path 'Variable:GalleryDemoExport' } | Should -Throw -ErrorId 'ConsoleDemoExportFailed*'
    }

    It 'exports <Slug> byte for byte and runs its embedded static snapshot' -ForEach @(
        Get-ChildItem (Join-Path $PSScriptRoot '../../Website/examples/gallery/*/metadata.json') | ForEach-Object { @{ Slug = $_.Directory.Name } }
    ) {
        $path = Join-Path $TestDrive "$Slug.ps1"
        $null = Get-ConsoleDemo $Slug -Path $path -Force
        $canonical = Join-Path $PSScriptRoot "../../Website/examples/gallery/$Slug/$Slug.ps1"
        [Convert]::ToBase64String([IO.File]::ReadAllBytes($path)) | Should -BeExactly ([Convert]::ToBase64String([IO.File]::ReadAllBytes($canonical)))
        $text = Invoke-GalleryChild -Body "Show-ConsoleDemo '$Slug' -StaticSnapshot -Step 0"
        $text.Trim() | Should -Not -BeNullOrEmpty
    }

    It 'forwards Step and produces the same deterministic snapshot as an exported standalone script' {
        $path = Join-Path $TestDrive 'standalone.ps1'
        $null = Get-ConsoleDemo pipeline -Path $path
        $embedded = Invoke-GalleryChild -Body 'Show-ConsoleDemo pipeline -StaticSnapshot -Step 1'
        $exported = Invoke-GalleryChild -Body "Set-Location '$($TestDrive.Replace("'", "''"))'; & '$($path.Replace("'", "''"))' -StaticSnapshot -Step 1"
        $embedded | Should -Match 'Restore\s+complete'
        $embedded | Should -Match 'Compile\s+running'
        $embedded | Should -Not -Match 'Restore\s+running'
        $embedded | Should -BeExactly $exported
        $embedded | Should -BeExactly (Invoke-GalleryChild -Body 'Show-ConsoleDemo pipeline -StaticSnapshot -Step 1')
    }

    It 'filters the exported <Slug> app through real scripted input and returns its final query' -ForEach @(
        @{ Slug = 'fleet'; Query = 'missing'; Expected = 'No matching hosts'; Absent = 'web-01' }
        @{ Slug = 'live-filter'; Query = 'sql'; Expected = 'sql-02'; Absent = 'web-01' }
    ) {
        $result = Invoke-GalleryAppChild -Slug $Slug -Keys ('"{0}" Escape' -f $Query)
        $result.State.Query | Should -BeExactly $Query
        $result.Frame | Should -Match ([regex]::Escape($Expected))
        $result.Frame | Should -Not -Match ([regex]::Escape($Absent))
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }

    It 'selects a queue preview in the exported toolbox and returns the chosen task' {
        $result = Invoke-GalleryAppChild -Slug toolbox -Keys 'Down Enter Escape'
        $result.State.Task | Should -BeExactly 'Inspect queue'
        $result.Frame | Should -Match '12 jobs waiting'
        $result.Frame | Should -Not -Match 'CPU 24%'
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }

    It 'chooses reports and advances the exported setup to its final review' {
        $result = Invoke-GalleryAppChild -Slug setup -Keys 'Tab Down Enter Tab Tab Enter Enter Escape'
        $result.State.Folder | Should -BeExactly './toolbox-agent'
        $result.State.Component | Should -BeExactly 'Core + reports'
        $result.State.Next | Should -Be 2
        $result.State.Back | Should -Be 0
        $result.Frame | Should -Match '3 / 3'
        $result.Frame | Should -Match 'Ready to install'
        $result.Frame | Should -Match 'Component: Core \+ reports'
        $result.Frame | Should -Not -Match 'Choose a writable folder'
        $result.FrameCount | Should -BeGreaterThan 1
        $result.Remaining | Should -Be 0
        $result.InputAcquired | Should -BeFalse
    }
}
