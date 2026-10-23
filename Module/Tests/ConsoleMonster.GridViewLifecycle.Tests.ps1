Describe 'GridView first-use Windows output mode' -Skip:($env:OS -ne 'Windows_NT') {
    BeforeAll {
        $script:GridHostFramework = if ($PSEdition -eq 'Desktop') { 'net472' } else { 'net8.0' }
        $script:GridHostExtension = if ($PSEdition -eq 'Desktop') { 'exe' } else { 'dll' }
        $project = Join-Path $PSScriptRoot '../../ConsoleMonster.TestHost/ConsoleMonster.TestHost.csproj'
        $build = @(& dotnet build $project -c Debug -f $script:GridHostFramework -warnaserror -nodeReuse:false --nologo 2>&1)
        if ($LASTEXITCODE -ne 0) { throw ($build -join "`n") }
        $script:GridHostAssembly = [IO.Path]::GetFullPath((Join-Path (Split-Path $project) "bin/Debug/$script:GridHostFramework/ConsoleMonster.TestHost.$script:GridHostExtension"))
    }

    It 'restores stdout after <Entry> <Scenario> in a fresh console before first capability lookup' -ForEach @(
        @{ Entry = 'Fallback'; Scenario = 'Accepted' }
        @{ Entry = 'Fallback'; Scenario = 'Escape' }
        @{ Entry = 'Direct'; Scenario = 'Accepted' }
        @{ Entry = 'Direct'; Scenario = 'Escape' }
        @{ Entry = 'Direct'; Scenario = 'Unavailable' }
    ) {
        $receipt = Join-Path $TestDrive ($Entry + '-' + $Scenario + '.txt')
        $helper = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Support/Assert-GridViewOutputMode.ps1'))
        $module = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../ConsoleMonster.psd1'))
        $hostExecutable = (Get-Process -Id $PID).Path
        $arguments = @('host', $hostExecutable, '-NoLogo', '-NoProfile', '-File', $helper,
            '-ModulePath', $module, '-AssemblyPath', $script:GridHostAssembly, '-Receipt', $receipt, '-Entry', $Entry, '-Scenario', $Scenario)
        if ($PSEdition -eq 'Desktop') { & $script:GridHostAssembly @arguments }
        else { & dotnet $script:GridHostAssembly @arguments }
        $LASTEXITCODE | Should -Be 0 -Because $(if (Test-Path $receipt) { Get-Content $receipt -Raw } else { 'The owned grid child did not produce a receipt.' })
        Get-Content $receipt -Raw | Should -Be ('GRID_MODE_OK ' + $Entry + ' ' + $Scenario + ' 3->3')
    }
}
