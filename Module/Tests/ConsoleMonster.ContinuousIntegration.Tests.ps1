Describe 'CI commands through PowerShell' {
    BeforeAll {
        . (Join-Path $PSScriptRoot 'Support/Invoke-TestChild.ps1')
        $script:CiModule = (Resolve-Path (Join-Path $PSScriptRoot '../ConsoleMonster.psd1')).Path
        $script:CiRuntime = [Diagnostics.Process]::GetCurrentProcess().Path
        $script:CiFailure = Join-Path $TestDrive 'Failure.ps1'
        Set-Content -LiteralPath $script:CiFailure -Value "throw 'Position failure'" -Encoding UTF8
        function Invoke-CiChild {
            param([string] $Script)
            $setup = @"
foreach (`$name in 'CONSOLEMONSTER_CI','GITHUB_ACTIONS','TF_BUILD','GITLAB_CI','CI','JENKINS_URL','TEAMCITY_VERSION') { [Environment]::SetEnvironmentVariable(`$name, `$null) }
Import-Module '$($script:CiModule.Replace("'", "''"))' -Force
`$failureFile = '$($script:CiFailure.Replace("'", "''"))'
"@
            @(Invoke-TestChild -Runtime $script:CiRuntime -Script ($setup + "`n" + $Script))
        }
    }

    It 'selects only a provider-specific true variable and honors none and the C# scope' {
        $text = Invoke-CiChild @'
$ci = [ConsoleMonster.Terminal].Assembly.GetType('ConsoleMonster.ContinuousIntegration')
foreach ($name in 'GITHUB_ACTIONS','TF_BUILD','GITLAB_CI') {
    [Environment]::SetEnvironmentVariable($name, 'true')
    "PROVIDER=$($ci::Provider)"
    [Environment]::SetEnvironmentVariable($name, 'false')
    "DISABLED=$($ci::Provider)"
}
$env:CI = 'true'
$env:JENKINS_URL = 'https://jenkins.example'
$env:TEAMCITY_VERSION = '2026.2'
"OTHER=$($ci::Provider)"
$env:GITHUB_ACTIONS = 'true'
$env:CONSOLEMONSTER_CI = 'none'
"OFF=$($ci::Provider)"
$scope = $ci::UseProvider('Azure')
try { "SCOPED=$($ci::Provider)" } finally { $scope.Dispose() }
"RESTORED=$($ci::Provider)"
'@
        @($text | Where-Object { $_ -like 'PROVIDER=*' }) -join ',' | Should -BeExactly 'PROVIDER=GitHub,PROVIDER=Azure,PROVIDER=GitLab'
        @($text | Where-Object { $_ -eq 'DISABLED=None' }).Count | Should -Be 3
        $text | Should -Contain 'OTHER=None'
        $text | Should -Contain 'OFF=None'
        $text | Should -Contain 'SCOPED=Azure'
        $text | Should -Contain 'RESTORED=None'
    }

    It 'closes nested groups on exceptions and preserves script output' {
        $text = Invoke-CiChild @'
$env:CONSOLEMONSTER_COLOR = 'truecolor'
$env:CONSOLEMONSTER_CI = 'github'
try {
    Use-ConsoleGroup 'Build' {
        Use-ConsoleGroup 'Test' { Write-Host 'test output'; throw 'failed' }
    }
} catch { 'caught' }
$value = Use-ConsoleGroup 'Result' { 42 }
"VALUE=$value"
Use-ConsoleGroup 'Switch' { $env:CONSOLEMONSTER_CI = 'none'; Write-Host 'provider changed' }
'@
        $commands = @($text | Where-Object { $_ -match '^::|^test output$|^caught$|^VALUE=' })
        ($commands -join "`n") | Should -BeExactly "::group::Build`n::group::Test`ntest output`n::endgroup::`n::endgroup::`ncaught`n::group::Result`n::endgroup::`nVALUE=42`n::group::Switch`n::endgroup::"
    }

    It 'returns semantic Markdown outside CI and sends no provider commands' {
        $text = Invoke-CiChild @'
Use-ConsoleGroup 'Build' { Write-ConsoleAnnotation -Level Notice -Message 'finished' -File ./build.ps1 -Line 4 }
$markdown = @([pscustomobject]@{ Check = 'Build'; Result = 'Passed' }, [pscustomobject]@{ Check = 'Test'; Result = 'Passed' }) | Add-ConsoleJobSummary
"MARKDOWN=$markdown"
'@
        ($text -join "`n") | Should -Match '\| Check \| Result \|'
        ($text -join "`n") | Should -Match '\| Build \|.*Passed \|'
        ($text -join "`n") | Should -Match 'finished.*build\.ps1:4'
        ($text -join "`n") | Should -Not -Match '::group::|::notice|##vso|section_start:'
    }

    It 'resolves annotation locations in the runspace and prints a single relative terminal label' {
        $text = Invoke-CiChild @'
Set-Location (Split-Path $failureFile)
$env:GITHUB_WORKSPACE = (Get-Location).ProviderPath
$env:CONSOLEMONSTER_CI = 'github'
Write-ConsoleAnnotation -Level Error -Message 'Value cannot be null' -File ./src/App.cs -Line 42
Write-ConsoleAnnotation -Level Warning -Message 'Outside' -File ../outside.ps1 -Line 7
New-PSDrive -Name CiSource -PSProvider FileSystem -Root (Get-Location).ProviderPath | Out-Null
Write-ConsoleAnnotation -Level Notice -Message 'Drive' -File CiSource:/src/App.cs -Line 42
$env:CONSOLEMONSTER_CI = 'none'
Write-ConsoleAnnotation -Level Error -Message 'Value cannot be null' -File (Join-Path (Get-Location) 'src/App.cs') -Line 42
'@
        ($text -join "`n") | Should -BeExactly "::error file=src/App.cs,line=42::Value cannot be null`n::warning file=../outside.ps1,line=7::Outside`n::notice file=src/App.cs,line=42::Drive`nError: Value cannot be null (src/App.cs:42)"
    }

    It 'groups sequential run bodies and Start-ConsoleRun completion output in CI' {
        $text = Invoke-CiChild @'
$env:CONSOLEMONSTER_CI = 'github'
'Build' | Invoke-ConsoleRun { Write-Host "working $_" }
$run = Start-ConsoleRun -InputObject 'Deploy' -NoLive -ProgressEvery 0 -ProgressIntervalSeconds 0
try { Update-Console -Run $run -Index 0 -Status Pass -Detail 'deployed' }
finally { Complete-ConsoleRun $run }
'@
        ($text -join "`n") | Should -Match '(?s)::group::Build\n.*working Build.*::endgroup::'
        ($text -join "`n") | Should -Match '(?s)::group::Deploy\n.*deployed.*::endgroup::'
        @($text | Where-Object { $_ -eq '::endgroup::' }).Count | Should -Be 2
    }

    It 'annotates a terminating script error when the Errors feature is enabled' {
        $text = Invoke-CiChild @'
$env:CONSOLEMONSTER_CI = 'github'
Enable-ConsoleFormatting -Include Errors -Exclude Output,FormatCommands,Streams,ReadLine
& $failureFile
'@
        @($text | Where-Object { $_ -match '^::error file=.*Failure.ps1,line=1,col=1::Position failure$' }).Count | Should -Be 1
        $disabled = Invoke-CiChild @'
$env:CONSOLEMONSTER_CI = 'github'
Enable-ConsoleFormatting -Include Errors -Exclude Output,FormatCommands,Streams,ReadLine
Disable-ConsoleFormatting -Feature Errors
& $failureFile
'@
        @($disabled | Where-Object { $_ -match '^::error' }).Count | Should -Be 0
    }

    It 'keeps parallel <Provider> groups, host messages and result records intact' -ForEach @(
        @{ Provider = 'github'; Mode = ''; Groups = 4 }
        @{ Provider = 'none'; Mode = '-NoLive'; Groups = 0 }
    ) {
        $text = Invoke-CiChild (@'
$env:CONSOLEMONSTER_CI = '__PROVIDER__'
$results = @(1..4 | Invoke-ConsoleRun -Parallel __MODE__ -ThrottleLimit 4 {
    Write-Host "working $_"
    Write-Warning "warning $_"
    Write-Progress -Activity "progress $_" -PercentComplete 50
    "result $_"
} -WarningAction SilentlyContinue -WarningVariable warnings)
"RESULTS=$($results -join ',')"
"WARNINGS=$(@($warnings).Count)"
'@.Replace('__PROVIDER__', $Provider).Replace('__MODE__', $Mode))
        $text | Should -Contain 'RESULTS=result 1,result 2,result 3,result 4'
        $text | Should -Contain 'WARNINGS=4'
        @($text | Where-Object { $_ -match '^working [1-4]$' }).Count | Should -Be 4
        @($text | Where-Object { $_ -eq '::endgroup::' }).Count | Should -Be $Groups
        ($text -join "`n") | Should -Not -Match 'Writer is closed|Process terminated'
    }

    It 'bounds errors shown by one default-output pipeline and counts the remainder once' {
        $text = Invoke-CiChild @'
$env:CONSOLEMONSTER_CI = 'github'
Enable-ConsoleFormatting -Include Errors -Exclude Output,FormatCommands,Streams,ReadLine
$sink = [Management.Automation.CmdletInfo]::new('Out-ConsoleDefault', (Get-Command Show-Console).ImplementingType.Assembly.GetType('ConsoleMonster.PowerShell.CmdletOutConsoleDefault'))
1..13 | ForEach-Object { try { & $failureFile } catch { $_ } } | & $sink -Shim -TopLevel
'@
        @($text | Where-Object { $_ -match '^::error file=' }).Count | Should -Be 10
        @($text | Where-Object { $_ -eq '3 additional errors omitted from CI annotations.' }).Count | Should -Be 1
    }
}
