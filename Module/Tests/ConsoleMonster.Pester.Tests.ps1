Describe 'Invoke-ConsolePester' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support\Invoke-TestChild.ps1')
        $script:ModulePath = Join-Path $PSScriptRoot '..\ConsoleMonster.psd1'
        $script:Runtime = [System.Diagnostics.Process]::GetCurrentProcess().Path
        $script:PesterPath = Join-Path (Get-Module Pester).ModuleBase 'Pester.psd1'
        $script:Fixtures = Join-Path $TestDrive 'fixtures'
        New-Item -ItemType Directory -Path $script:Fixtures -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $script:Fixtures 'Sample.Tests.ps1') -Encoding UTF8 -Value @'
Describe 'Math' {
    It 'adds' { 1 + 1 | Should -Be 2 }
    It 'fails number' { 3 | Should -Be 4 }
    It 'skips' -Skip { 1 | Should -Be 1 }
    Context 'nested' {
        It 'writes' { Write-Host 'from the test'; $true | Should -BeTrue }
    }
}
'@

        # Runs a script in a child process with redirected output (a stream terminal), the module and the same Pester
        # version as this run imported, and detection inputs cleared.
        function Invoke-PesterChild {
            param([Parameter(Mandatory)][string] $Script, [switch] $WithoutPester)
            $lines = @(
                "foreach (`$n in 'NO_COLOR', 'CONSOLEMONSTER_COLOR', 'CONSOLEMONSTER_GLYPHS', 'CONSOLEMONSTER_MODE', 'CI', 'GITHUB_ACTIONS', 'TF_BUILD', 'TERM', 'WT_SESSION', 'COLUMNS', 'LINES') { [Environment]::SetEnvironmentVariable(`$n, `$null) }"
                if ($WithoutPester) {
                    "`$env:PSModulePath = Join-Path `$PSHOME 'Modules'"
                } else {
                    # PS 5.1 and PS 7 search different user-module folders. Use the running Pester's path so a child
                    # cannot silently miss this version and let the adapter discover a different installation.
                    "Import-Module '$($script:PesterPath.Replace("'", "''"))' -Force -ErrorAction Stop"
                }
                "Import-Module '$($script:ModulePath)' -Force"
                "`$fixtures = '$($script:Fixtures)'"
                $Script
            )
            $output = Invoke-TestChild -Runtime $script:Runtime -Script ($lines -join "`n")
            ($output | ForEach-Object { "$_" }) -join "`n"
        }
    }

    It 'returns the Pester result object with -PassThru, with the same counts as Invoke-Pester' {
        $text = Invoke-PesterChild @'
$plain = Invoke-Pester -Path $fixtures -PassThru -Output None
$r = Invoke-ConsolePester -Path $fixtures -PassThru
"TYPE=$($r.PSObject.Properties.Name -contains 'Containers')"
"COUNTS=$($r.Result);$($r.PassedCount);$($r.FailedCount);$($r.SkippedCount)"
"PLAIN=$($plain.Result);$($plain.PassedCount);$($plain.FailedCount);$($plain.SkippedCount)"
'@
        $text | Should -Match 'TYPE=True'
        $text | Should -Match 'COUNTS=Failed;2;1;1'
        $text | Should -Match 'PLAIN=Failed;2;1;1'
    }

    It 'writes one line per failure and a summary line in redirected output' {
        $text = Invoke-PesterChild 'Invoke-ConsolePester -Path $fixtures'
        $text | Should -Match 'Math . fails number: expected 4, actual 3 \(Sample\.Tests\.ps1:3\)'
        $text | Should -Match 'Tests failed: 1 failed . 2 passed . 1 skipped . 4 tests'
        $text | Should -Match 'from the test'
        $text | Should -Not -Match 'Running tests from'
    }

    It 'returns nothing without -PassThru and restores the Pester screen plugin afterwards' {
        $text = Invoke-PesterChild @'
$pester = Get-Module Pester
$before = (& $pester { ${function:Get-WriteScreenPlugin} }).ToString()
$out = @(Invoke-ConsolePester -Path $fixtures)
"OUT=$($out.Count)"
"RESTORED=$((& $pester { ${function:Get-WriteScreenPlugin} }).ToString() -eq $before)"
'@
        $text | Should -Match 'OUT=0'
        $text | Should -Match 'RESTORED=True'
    }

    It 'falls back to a plain Pester run when the internals it relies on change' {
        $text = Invoke-PesterChild @'
$pester = Get-Module Pester
$original = & $pester { ${function:Get-WriteScreenPlugin} }
& $pester { param($o) $script:CmOriginal = $o; function script:Get-WriteScreenPlugin { & $script:CmOriginal @args } } $original
$r = Invoke-ConsolePester -Path $fixtures -PassThru -WarningVariable warnings -WarningAction SilentlyContinue
"WARN=$($warnings -join ' ')"
"COUNTS=$($r.Result);$($r.PassedCount);$($r.FailedCount);$($r.SkippedCount)"
'@
        $text | Should -Match 'WARN=Invoke-ConsolePester runs Pester .* with its own output: Pester internals changed: Get-WriteScreenPlugin has no Verbosity parameter'
        $text | Should -Match 'COUNTS=Failed;2;1;1'
        $text | Should -Match '(?s)Tests Passed: 2,.*Failed: 1'
    }

    It 'runs quietly with -Output None and with -NoLive keeps Pester''s own output' {
        $text = Invoke-PesterChild @'
$quiet = Invoke-ConsolePester -Path $fixtures -Output None -PassThru
"QUIET=$($quiet.FailedCount)"
'---'
$plain = Invoke-ConsolePester -Path $fixtures -NoLive -PassThru
"PLAIN=$($plain.FailedCount)"
'@
        $quietPart, $plainPart = $text -split '---'
        $quietPart | Should -Match 'QUIET=1'
        $quietPart | Should -Not -Match 'Tests failed'
        $plainPart | Should -Match '(?s)Tests Passed: 2,.*Failed: 1'
        $plainPart | Should -Match 'Tests failed: 1 failed'
        $plainPart | Should -Match 'PLAIN=1'
    }

    It 'accepts a configuration and saves the final dashboard with -ExportHtml' {
        $text = Invoke-PesterChild @'
$c = New-PesterConfiguration
$c.Run.Path = $fixtures
$c.Run.PassThru = $true
$c.Filter.FullName = '*adds*'
$path = Join-Path $fixtures 'run.html'
$r = Invoke-ConsolePester -Configuration $c -ExportHtml $path
"COUNTS=$($r.Result);$($r.PassedCount);$($r.FailedCount)"
"HTML=$((Get-Content -LiteralPath $path -Raw) -match 'Sample\.Tests\.ps1')"
'@
        $text | Should -Match 'COUNTS=Passed;1;0'
        $text | Should -Match 'HTML=True'
    }

    It 'stops with a clear error when Pester 5 or later is not installed' {
        $text = Invoke-PesterChild -WithoutPester @'
try { Invoke-ConsolePester -Path $fixtures; 'NO ERROR' } catch { "ERROR=$($_.FullyQualifiedErrorId)|$($_.Exception.Message)" }
'@
        $text | Should -Match 'ERROR=ConsoleMonsterPesterNotFound'
        $text | Should -Match 'needs Pester 5 or later'
    }
}
